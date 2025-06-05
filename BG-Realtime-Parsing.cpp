//Read this if ur gay
#include <iostream>
#include <vector>
#include <thread>
#include <mutex>
#include <fstream>
#include <pcap.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <chrono>
#include <unordered_map>
#include <unordered_set>
#include <map>
#include <atomic>
#include <csignal>
#include <ctime>
#include <sstream>
#include <iomanip>
#include <set>
#include <cstring>
#include <sodium.h>

#pragma comment(lib, "ws2_32.lib")

#ifdef _WIN32
#include <conio.h>
#else
#include <termios.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/ioctl.h>
#endif

constexpr size_t KEY_SIZE = crypto_secretbox_KEYBYTES;
constexpr size_t NONCE_SIZE = crypto_secretbox_NONCEBYTES;
constexpr size_t MAC_SIZE = crypto_secretbox_MACBYTES;

// --- Standardized (shared) encryption key ---
const unsigned char sodium_key[KEY_SIZE] = {
    0xDE,0xAD,0xBE,0xEF,0xFE,0xED,0xBA,0xBE,
    0xCA,0xFE,0xFA,0xCE,0xBA,0xAD,0xF0,0x0D,
    0x12,0x34,0x56,0x78,0x9A,0xBC,0xDE,0xF0,
    0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88
};

// --- Data Structures ---

struct TrafficKey {
    std::string device_name;
    std::string ip;
    std::string protocol;

    bool operator==(const TrafficKey& other) const {
        return device_name == other.device_name &&
            ip == other.ip &&
            protocol == other.protocol;
    }
};

namespace std {
    template <>
    struct hash<TrafficKey> {
        size_t operator()(const TrafficKey& k) const {
            size_t h1 = hash<std::string>()(k.device_name);
            size_t h2 = hash<std::string>()(k.ip);
            size_t h3 = hash<std::string>()(k.protocol);
            return (h1 ^ (h2 << 1)) ^ (h3 << 2);
        }
    };
}

struct TrafficStats {
    int total_count = 0;
    uint64_t total_bytes = 0;
    std::unordered_set<std::string> src_ports;
    std::unordered_set<std::string> dest_ports;
    std::chrono::system_clock::time_point first_seen;
    std::chrono::system_clock::time_point last_seen;
    std::string domain_or_mdns;
    std::string eth_type;
    std::time_t month_start;
    std::set<int> hours_seen; // set of hours (0-23) when communication occurred
};

// --- Globals ---

std::mutex stats_mutex;
std::mutex devname_mutex;
std::map<pcap_t*, std::string> handle_to_devname;
std::unordered_map<TrafficKey, TrafficStats> traffic_stats;

std::atomic<bool> keep_running(true);
std::atomic<bool> capture_enabled(true);

// --- Signal Handling ---

#ifdef _WIN32
#include <windows.h>
BOOL WINAPI console_handler(DWORD signal) {
    if (signal == CTRL_C_EVENT) {
        keep_running = false;
        return TRUE;
    }
    return FALSE;
}
#else
#include <signal.h>
void signal_handler(int) {
    keep_running = false;
}
#endif

// --- Utility Functions ---

std::string join_ports(const std::unordered_set<std::string>& ports) {
    if (ports.empty()) return "n/a";
    std::string result;
    for (const auto& port : ports) {
        if (!result.empty()) result += ";";
        result += port;
    }
    return result;
}

std::string join_hours(const std::set<int>& hours) {
    if (hours.empty()) return "n/a";
    std::ostringstream oss;
    bool first = true;
    for (int h : hours) {
        if (!first) oss << ";";
        oss << std::setw(2) << std::setfill('0') << h;
        first = false;
    }
    return oss.str();
}

std::time_t get_month_start() {
    auto now = std::chrono::system_clock::now();
    std::time_t t = std::chrono::system_clock::to_time_t(now);
    tm tm_buf;
#ifdef _WIN32
    localtime_s(&tm_buf, &t);
#else
    localtime_r(&t, &tm_buf);
#endif
    tm_buf.tm_mday = 1;
    tm_buf.tm_hour = 0;
    tm_buf.tm_min = 0;
    tm_buf.tm_sec = 0;
    return mktime(&tm_buf);
}

// --- Ethernet Header Parsing ---
std::string parse_ethernet_header(const u_char* pck, int caplen, int& eth_type) {
    if (caplen < 14) return "Unknown";
    eth_type = (pck[12] << 8) | pck[13];
    if (eth_type == 0x8100 && caplen >= 18) { // VLAN tag present
        eth_type = (pck[16] << 8) | pck[17];
        return "802.1Q VLAN";
    }
    if (eth_type == 0x0800 || eth_type == 0x86DD)
        return "Ethernet II/DIX";
    return "Unknown";
}

// --- DNS Parsing Helpers ---
bool parse_dns_name(const u_char* dns_data, int dns_len, int& offset, std::string& out_name) {
    out_name.clear();
    bool jumped = false;
    int jump_offset = -1;

    while (offset < dns_len) {
        uint8_t len = dns_data[offset];
        if (len == 0) {
            offset++;
            break;
        }
        if ((len & 0xC0) == 0xC0) {
            if (!jumped) {
                jump_offset = offset + 2;
                jumped = true;
            }
            int pointer = ((len & 0x3F) << 8) | dns_data[offset + 1];
            if (pointer >= dns_len) return false;
            offset = pointer;
            continue;
        }
        offset++;
        if (offset + len > dns_len) return false;
        if (!out_name.empty()) out_name += ".";
        out_name.append(reinterpret_cast<const char*>(dns_data + offset), len);
        offset += len;
    }
    if (jumped) offset = jump_offset;
    return true;
}

std::string parse_dns_query(const u_char* payload, int payload_len) {
    if (payload_len < 12) return "";
    int offset = 12;
    std::string domain;
    if (!parse_dns_name(payload, payload_len, offset, domain)) return "";
    return domain;
}

// --- mDNS Extraction Helper ---
std::string extract_mdns_name(const u_char* payload, int payload_len) {
    if (payload_len < 12) return "";

    int qdcount = (payload[4] << 8) | payload[5];
    int ancount = (payload[6] << 8) | payload[7];
    int offset = 12;
    std::string name;

    // Extract first question name
    if (qdcount > 0) {
        if (!parse_dns_name(payload, payload_len, offset, name)) return "";
        return name;
    }

    // Skip to first answer (if any)
    for (int i = 0; i < qdcount; ++i) {
        if (!parse_dns_name(payload, payload_len, offset, name)) return "";
        if (offset + 4 > payload_len) return "";
        offset += 4; // type (2) + class (2)
    }

    // Extract first answer name
    if (ancount > 0) {
        if (!parse_dns_name(payload, payload_len, offset, name)) return "";
        return name;
    }

    return "";
}

// --- TLS SNI Parsing Helpers ---
std::string parse_tls_sni(const u_char* payload, int payload_len) {
    if (payload_len < 5) return "";
    if (payload[0] != 0x16) return "";
    int record_length = (payload[3] << 8) | payload[4];
    if (record_length + 5 > payload_len) return "";

    if (payload[5] != 0x01) return "";
    if (payload_len < 5 + 4) return "";

    int handshake_length = (payload[6] << 16) | (payload[7] << 8) | payload[8];
    if (handshake_length + 9 > payload_len) return "";

    int pos = 9;

    if (pos + 2 + 32 + 1 > payload_len) return "";
    pos += 2 + 32;
    uint8_t session_id_len = payload[pos];
    pos += 1 + session_id_len;
    if (pos + 2 > payload_len) return "";

    uint16_t cipher_suites_len = (payload[pos] << 8) | payload[pos + 1];
    pos += 2 + cipher_suites_len;
    if (pos + 1 > payload_len) return "";

    uint8_t comp_methods_len = payload[pos];
    pos += 1 + comp_methods_len;
    if (pos + 2 > payload_len) return "";

    uint16_t extensions_len = (payload[pos] << 8) | payload[pos + 1];
    pos += 2;
    int extensions_end = pos + extensions_len;
    if (extensions_end > payload_len) return "";

    while (pos + 4 <= extensions_end) {
        uint16_t ext_type = (payload[pos] << 8) | payload[pos + 1];
        uint16_t ext_len = (payload[pos + 2] << 8) | payload[pos + 3];
        pos += 4;
        if (pos + ext_len > extensions_end) return "";

        if (ext_type == 0x0000) {
            int sni_pos = pos;
            if (sni_pos + 2 > extensions_end) return "";
            uint16_t sni_list_len = (payload[sni_pos] << 8) | payload[sni_pos + 1];
            sni_pos += 2;
            if (sni_pos + sni_list_len > extensions_end) return "";

            while (sni_pos + 3 <= extensions_end) {
                uint8_t name_type = payload[sni_pos];
                uint16_t name_len = (payload[sni_pos + 1] << 8) | payload[sni_pos + 2];
                sni_pos += 3;
                if (sni_pos + name_len > extensions_end) return "";
                if (name_type == 0) {
                    std::string sni_host(reinterpret_cast<const char*>(payload + sni_pos), name_len);
                    return sni_host;
                }
                sni_pos += name_len;
            }
        }
        pos += ext_len;
    }
    return "";
}

// --- HTTP Host Parsing Helper ---
std::string parse_http_host(const u_char* payload, int payload_len) {
    if (payload_len < 16) return "";
    std::string data(reinterpret_cast<const char*>(payload), payload_len);
    size_t host_pos = data.find("\r\nHost: ");
    if (host_pos == std::string::npos) {
        if (data.find("Host: ") == 0) host_pos = 0;
        else host_pos = data.find("Host: ");
        if (host_pos == std::string::npos) return "";
    }
    else {
        host_pos += 2; // skip \r\n
    }
    host_pos = data.find("Host: ", host_pos);
    if (host_pos == std::string::npos) return "";
    host_pos += 6;
    size_t end = data.find("\r\n", host_pos);
    if (end == std::string::npos) end = data.length();
    return data.substr(host_pos, end - host_pos);
}

// --- Encryption/Decryption Functions ---
std::string encrypt_data(const std::string& data, const unsigned char* key) {
    unsigned char nonce[NONCE_SIZE];
    randombytes_buf(nonce, sizeof nonce);

    std::string encrypted_data;
    encrypted_data.resize(data.size() + MAC_SIZE + NONCE_SIZE);

    // Copy nonce to the beginning of the encrypted data
    std::copy(nonce, nonce + NONCE_SIZE, encrypted_data.begin());

    // Encrypt the data
    if (crypto_secretbox_easy(
        reinterpret_cast<unsigned char*>(&encrypted_data[NONCE_SIZE]),
        reinterpret_cast<const unsigned char*>(data.data()),
        data.size(), nonce, key) != 0) {
        throw std::runtime_error("Encryption failed");
    }

    return encrypted_data;
}

std::string decrypt_data(const std::string& encrypted_data, const unsigned char* key) {
    if (encrypted_data.size() < NONCE_SIZE + MAC_SIZE) {
        throw std::runtime_error("Invalid encrypted data");
    }

    unsigned char nonce[NONCE_SIZE];
    std::copy(encrypted_data.begin(), encrypted_data.begin() + NONCE_SIZE, nonce);

    std::string decrypted_data;
    decrypted_data.resize(encrypted_data.size() - NONCE_SIZE - MAC_SIZE);

    // Decrypt the data
    if (crypto_secretbox_open_easy(
        reinterpret_cast<unsigned char*>(&decrypted_data[0]),
        reinterpret_cast<const unsigned char*>(&encrypted_data[NONCE_SIZE]),
        encrypted_data.size() - NONCE_SIZE, nonce, key) != 0) {
        throw std::runtime_error("Decryption failed");
    }

    return decrypted_data;
}

void write_encrypted_csv_line(std::ofstream& csv_file, const std::string& csv_line) {
    std::string encrypted = encrypt_data(csv_line, sodium_key);
    uint32_t len = static_cast<uint32_t>(encrypted.size());
    csv_file.write(reinterpret_cast<const char*>(&len), sizeof(len));
    csv_file.write(encrypted.data(), encrypted.size());
}

// --- Packet Handler ---
void packet_handler(u_char* user, const struct pcap_pkthdr* header, const u_char* pck) {
    if (!capture_enabled) return;

    pcap_t* handle = (pcap_t*)user;
    std::string dev_name;
    {
        std::lock_guard<std::mutex> lock(devname_mutex);
        auto it = handle_to_devname.find(handle);
        dev_name = (it != handle_to_devname.end()) ? it->second : "n/a";
    }

    int eth_type;
    std::string eth_header = parse_ethernet_header(pck, header->caplen, eth_type);
    int header_len = (eth_header == "Ethernet II/DIX") ? 14 : (eth_header == "802.1Q VLAN" ? 18 : 0);
    if (header_len == 0 || header->caplen < static_cast<size_t>(header_len + 20)) return;

    int ip_version = (pck[header_len] >> 4);
    std::string src_ip, protocol_str;
    int src_port = -1, dest_port = -1;

    if (ip_version == 4) {
        int ip_header_len = (pck[header_len] & 0x0F) * 4;
        if (header->caplen < static_cast<size_t>(header_len + ip_header_len + 4)) return;
        u_char protocol = pck[header_len + 9];
        protocol_str = (protocol == 6) ? "TCP" : (protocol == 17) ? "UDP" : "OTHER";
        char src_ip_buf[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, pck + header_len + 12, src_ip_buf, INET_ADDRSTRLEN);
        src_ip = src_ip_buf;
        if (protocol == 6 || protocol == 17) {
            const u_char* transport_header = pck + header_len + ip_header_len;
            src_port = ntohs(*reinterpret_cast<const uint16_t*>(transport_header));
            dest_port = ntohs(*reinterpret_cast<const uint16_t*>(transport_header + 2));
        }
    }
    else if (ip_version == 6) {
        const int ipv6_header_len = 40;
        if (header->caplen < static_cast<size_t>(header_len + ipv6_header_len + 4)) return;
        u_char next_header = pck[header_len + 6];
        protocol_str = (next_header == 6) ? "TCP" : (next_header == 17) ? "UDP" : "OTHER";
        char src_ip_buf[INET6_ADDRSTRLEN];
        inet_ntop(AF_INET6, pck + header_len + 8, src_ip_buf, INET6_ADDRSTRLEN);
        src_ip = src_ip_buf;
        if (next_header == 6 || next_header == 17) {
            const u_char* transport_header = pck + header_len + ipv6_header_len;
            src_port = ntohs(*reinterpret_cast<const uint16_t*>(transport_header));
            dest_port = ntohs(*reinterpret_cast<const uint16_t*>(transport_header + 2));
        }
    }
    else return;

    // --- Domain/mDNS Extraction ---
    std::string domain_or_mdns;
    bool is_mdns = false;

    // mDNS
    if (protocol_str == "UDP" && (src_port == 5353 || dest_port == 5353)) {
        int udp_header_offset = header_len + ((ip_version == 4) ? ((pck[header_len] & 0x0F) * 4) : 40);
        int udp_payload_len = header->caplen - udp_header_offset - 8;
        if (udp_payload_len > 0) {
            const u_char* mdns_payload = pck + udp_header_offset + 8;
            domain_or_mdns = extract_mdns_name(mdns_payload, udp_payload_len);
            is_mdns = !domain_or_mdns.empty();
        }
    }
    // DNS
    if (!is_mdns && protocol_str == "UDP" && (src_port == 53 || dest_port == 53)) {
        int udp_header_offset = header_len + ((ip_version == 4) ? ((pck[header_len] & 0x0F) * 4) : 40);
        int udp_payload_len = header->caplen - udp_header_offset - 8;
        if (udp_payload_len > 0) {
            const u_char* dns_payload = pck + udp_header_offset + 8;
            domain_or_mdns = parse_dns_query(dns_payload, udp_payload_len);
        }
    }
    // TLS SNI
    if (domain_or_mdns.empty() && protocol_str == "TCP" && (src_port == 443 || dest_port == 443)) {
        int ip_header_len = (ip_version == 4) ? ((pck[header_len] & 0x0F) * 4) : 40;
        int tcp_header_offset = header_len + ip_header_len;
        if (header->caplen > tcp_header_offset + 20) {
            int tcp_header_len = ((pck[tcp_header_offset + 12] >> 4) & 0x0F) * 4;
            int tls_payload_offset = tcp_header_offset + tcp_header_len;
            int tls_payload_len = header->caplen - tls_payload_offset;
            if (tls_payload_len > 0) {
                domain_or_mdns = parse_tls_sni(pck + tls_payload_offset, tls_payload_len);
            }
        }
    }
    // HTTP Host
    if (domain_or_mdns.empty() && protocol_str == "TCP" && (src_port == 80 || dest_port == 80)) {
        int ip_header_len = (ip_version == 4) ? ((pck[header_len] & 0x0F) * 4) : 40;
        int tcp_header_offset = header_len + ip_header_len;
        if (header->caplen > tcp_header_offset + 20) {
            int tcp_header_len = ((pck[tcp_header_offset + 12] >> 4) & 0x0F) * 4;
            int http_payload_offset = tcp_header_offset + tcp_header_len;
            int http_payload_len = header->caplen - http_payload_offset;
            if (http_payload_len > 0) {
                domain_or_mdns = parse_http_host(pck + http_payload_offset, http_payload_len);
            }
        }
    }

    // --- Update Stats ---
    TrafficKey key{ dev_name, src_ip, protocol_str };
    auto now = std::chrono::system_clock::now();
    std::time_t now_time = std::chrono::system_clock::to_time_t(now);
    tm tm_now;
#ifdef _WIN32
    localtime_s(&tm_now, &now_time);
#else
    localtime_r(&now_time, &tm_now);
#endif
    int hour = tm_now.tm_hour;

    {
        std::lock_guard<std::mutex> lock(stats_mutex);
        auto& stats = traffic_stats[key];
        if (stats.total_count == 0) {
            stats.first_seen = now;
            stats.month_start = get_month_start();
        }
        stats.last_seen = now;
        stats.total_count++;
        stats.total_bytes += header->len;
        if (src_port != -1) stats.src_ports.insert(std::to_string(src_port));
        if (dest_port != -1) stats.dest_ports.insert(std::to_string(dest_port));
        stats.domain_or_mdns = domain_or_mdns;
        stats.eth_type = eth_header;
        stats.hours_seen.insert(hour);
    }
}

// --- Device Selection and Capture Threading ---
void capture_thread_func(pcap_t* handle) {
    pcap_loop(handle, 0, packet_handler, reinterpret_cast<u_char*>(handle));
}

// --- Periodic Encrypted CSV Writer ---
void periodic_encrypted_writer(const std::string& filename, int interval_seconds) {
    while (keep_running) {
        std::this_thread::sleep_for(std::chrono::seconds(interval_seconds));
        std::ofstream csv_file(filename, std::ios::binary);
        if (!csv_file) continue;
        write_encrypted_csv_line(csv_file, "timestamp,Device,IP,Domain,Src Ports,Dest Ports,Protocol,EthType,MonthlyAvgFreq/s,MonthlyTotalFreq,MonthlyTotalBytes,mdns_dns_name");
        {
            std::lock_guard<std::mutex> lock(stats_mutex);
            for (auto it = traffic_stats.begin(); it != traffic_stats.end(); ++it) {
                const TrafficKey& key = it->first;
                const TrafficStats& stats = it->second;
                std::string hours_str = join_hours(stats.hours_seen);
                double seconds_in_month = std::difftime(std::time(nullptr), stats.month_start);
                double monthly_avg = (seconds_in_month > 0) ? (double)stats.total_count / seconds_in_month : 0.0;
                std::ostringstream oss;
                oss << hours_str << ","
                    << key.device_name << ","
                    << key.ip << ","
                    << (stats.domain_or_mdns.empty() ? "n/a" : stats.domain_or_mdns) << ","
                    << join_ports(stats.src_ports) << ","
                    << join_ports(stats.dest_ports) << ","
                    << key.protocol << ","
                    << stats.eth_type << ","
                    << monthly_avg << ","
                    << stats.total_count << ","
                    << stats.total_bytes << ","
                    << (stats.domain_or_mdns.empty() ? "n/a" : stats.domain_or_mdns);
                write_encrypted_csv_line(csv_file, oss.str());
            }
        }
        csv_file.close();
    }
}

// --- Main ---
int main() {
    if (sodium_init() < 0) {
        std::cerr << "libsodium initialization failed\n";
        return 1;
    }

#ifdef _WIN32
    SetConsoleCtrlHandler(console_handler, TRUE);
#else
    std::signal(SIGINT, signal_handler);
#endif

    // --- Device selection: open all interfaces automatically ---
    char errbuf[PCAP_ERRBUF_SIZE];
    pcap_if_t* alldevs;
    if (pcap_findalldevs(&alldevs, errbuf) == -1) {
        std::cerr << "Error finding devices: " << errbuf << std::endl;
        return 1;
    }
    std::vector<pcap_if_t*> devs;
    for (pcap_if_t* d = alldevs; d; d = d->next) {
        devs.push_back(d);
    }
    int ndevs = static_cast<int>(devs.size());
    if (ndevs == 0) {
        std::cerr << "No devices found.\n";
        pcap_freealldevs(alldevs);
        return 1;
    }

    // --- Open all devices and start capture threads ---
    std::vector<pcap_t*> handles;
    std::vector<std::thread> threads;
    for (int i = 0; i < ndevs; ++i) {
        pcap_if_t* dev = devs[i];
        pcap_t* handle = pcap_open_live(dev->name, 65536, 1, 1000, errbuf);
        if (!handle) {
            std::cerr << "Failed to open " << dev->name << ": " << errbuf << std::endl;
            continue;
        }
        {
            std::lock_guard<std::mutex> lock(devname_mutex);
            // Use description if available, otherwise fallback to internal name
            handle_to_devname[handle] = dev->description ? dev->description : dev->name;
        }
        handles.push_back(handle);
        threads.emplace_back(capture_thread_func, handle);
    }
    pcap_freealldevs(alldevs);
    if (handles.empty()) {
        std::cerr << "No devices could be opened for capture.\n";
        return 1;
    }

    // --- Start periodic encrypted writer thread ---
    std::thread writer_thread(periodic_encrypted_writer, "packet_capture_encrypted.csv", 10); // every 10 seconds

    // --- User prompt loop for decrypted CSV ---
    std::cout << "Packet capture running on all interfaces.\n";
    std::cout << "Encrypted stats are written to 'packet_capture_encrypted.csv' every 10 seconds.\n";
    std::cout << "Type 'd' + Enter at any time to write decrypted stats to 'packet_capture_decrypted.csv'.\n";
    std::cout << "Press Ctrl+C to exit.\n";
    std::string input;
    while (keep_running) {
        std::getline(std::cin, input);
        if (input == "d") {
            std::string filename = "packet_capture_decrypted.csv";
            std::ofstream csv_file(filename);
            if (!csv_file) {
                std::cerr << "Failed to open file for writing.\n";
                continue;
            }
            csv_file << "timestamp,Device,IP,Domain,Src Ports,Dest Ports,Protocol,EthType,MonthlyAvgFreq/s,MonthlyTotalFreq,MonthlyTotalBytes,mdns_dns_name\n";
            std::lock_guard<std::mutex> lock(stats_mutex);
            for (auto it = traffic_stats.begin(); it != traffic_stats.end(); ++it) {
                const TrafficKey& key = it->first;
                const TrafficStats& stats = it->second;
                std::string hours_str = join_hours(stats.hours_seen);
                double seconds_in_month = std::difftime(std::time(nullptr), stats.month_start);
                double monthly_avg = (seconds_in_month > 0) ? (double)stats.total_count / seconds_in_month : 0.0;
                csv_file << hours_str << ","
                    << key.device_name << ","
                    << key.ip << ","
                    << (stats.domain_or_mdns.empty() ? "n/a" : stats.domain_or_mdns) << ","
                    << join_ports(stats.src_ports) << ","
                    << join_ports(stats.dest_ports) << ","
                    << key.protocol << ","
                    << stats.eth_type << ","
                    << monthly_avg << ","
                    << stats.total_count << ","
                    << stats.total_bytes << ","
                    << (stats.domain_or_mdns.empty() ? "n/a" : stats.domain_or_mdns) << "\n";
            }
            csv_file.close();
            std::cout << "Decrypted CSV written to '" << filename << "'\n";
        }
    }

    // --- Cleanup ---
    for (auto handle : handles) {
        pcap_breakloop(handle);
    }
    for (auto& t : threads) {
        if (t.joinable()) t.join();
    }
    for (auto handle : handles) {
        pcap_close(handle);
    }
    if (writer_thread.joinable()) writer_thread.join();

    return 0;
}
