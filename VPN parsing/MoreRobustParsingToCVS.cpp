#include <iostream>
#include <vector>
#include <thread>
#include <mutex>
#include <fstream>
#include <sstream>
#include <pcap.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <chrono>
#include <unordered_map>
#include <map>

#pragma comment(lib, "ws2_32.lib")

struct TrafficKey {
    std::string ip;
    uint16_t port;
    std::string protocol; // "TCP", "UDP", etc.

    bool operator==(const TrafficKey& other) const {
        return ip == other.ip && port == other.port && protocol == other.protocol;
    }
};

namespace std {
    template <>
    struct hash<TrafficKey> {
        size_t operator()(const TrafficKey& k) const {
            return ((hash<std::string>()(k.ip)
                ^ (hash<uint16_t>()(k.port) << 1)) >> 1)
                ^ (hash<std::string>()(k.protocol) << 1);
        }
    };
}

struct TrafficStats {
    int total_count = 0;       // Total packets
    uint64_t total_bytes = 0;  // Total bytes
    int recent_count = 0;      // Packets in last 5 seconds
    uint64_t recent_bytes = 0; // Bytes in last 5 seconds
    std::chrono::steady_clock::time_point last_update = std::chrono::steady_clock::now();
};

std::ofstream log_file;     // For normal log messages
std::ofstream csv_file;     // For CSV output

std::mutex stats_mutex;
std::mutex devname_mutex;
std::map<pcap_t*, std::string> handle_to_devname;

std::unordered_map<TrafficKey, TrafficStats> traffic_stats;

std::string resolve_hostname(const std::string& ip) {
    sockaddr_storage sa;
    int sa_len = 0;
    char host[NI_MAXHOST];
    if (ip.find(':') != std::string::npos) { // IPv6
        sockaddr_in6* sa6 = (sockaddr_in6*)&sa;
        sa6->sin6_family = AF_INET6;
        inet_pton(AF_INET6, ip.c_str(), &sa6->sin6_addr);
        sa_len = sizeof(sockaddr_in6);
    }
    else { // IPv4
        sockaddr_in* sa4 = (sockaddr_in*)&sa;
        sa4->sin_family = AF_INET;  // FIXED: use sin_family for IPv4
        inet_pton(AF_INET, ip.c_str(), &sa4->sin_addr);
        sa_len = sizeof(sockaddr_in);
    }
    if (getnameinfo((sockaddr*)&sa, sa_len, host, sizeof(host), nullptr, 0, NI_NAMEREQD) == 0) {
        return std::string(host);
    }
    return "";
}

void log_message(const std::string& msg) {
    std::cout << msg << std::endl;
    if (log_file.is_open()) {
        log_file << msg << std::endl;
        log_file.flush();
    }
}

void log_csv(const std::string& csv_line) {
    if (csv_file.is_open()) {
        csv_file << csv_line << std::endl;
        csv_file.flush();
    }
}

void packet_handler(u_char* user, const struct pcap_pkthdr* header, const u_char* pck) {
    pcap_t* handle = (pcap_t*)user;
    std::string dev_name;
    {
        std::lock_guard<std::mutex> lock(devname_mutex);
        auto it = handle_to_devname.find(handle);
        dev_name = (it != handle_to_devname.end()) ? it->second : "Unknown Device";
    }

    int linktype = pcap_datalink(handle);
    int header_len = (linktype == DLT_EN10MB) ? 14 : (linktype == DLT_NULL ? 4 : 0);
    if (header_len == 0 || header->caplen < static_cast<size_t>(header_len + 20)) return;

    int ip_version = (pck[header_len] >> 4);
    std::string src_ip;
    uint16_t dest_port = 0; // Destination port
    std::string protocol_str;
    uint64_t packet_len = header->len;

    if (ip_version == 4) {
        if (header->caplen < static_cast<size_t>(header_len + 20)) return;
        int ip_header_len = (pck[header_len] & 0x0F) * 4;
        if (header->caplen < static_cast<size_t>(header_len + ip_header_len + 4)) return;
        u_char protocol = pck[header_len + 9];
        protocol_str = (protocol == 6) ? "TCP" : (protocol == 17) ? "UDP" : "OTHER";

        char Src_Ip[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, pck + header_len + 12, Src_Ip, INET_ADDRSTRLEN);
        src_ip = Src_Ip;

        if (protocol == 6 || protocol == 17) { // TCP or UDP
            const u_char* transport_header = pck + header_len + ip_header_len;
            dest_port = ntohs(*(uint16_t*)(transport_header + 2));  // Destination port
        }
        else {
            dest_port = 0;
        }
    }
    else if (ip_version == 6) {
        const int ipv6_header_len = 40;
        if (header->caplen < static_cast<size_t>(header_len + ipv6_header_len + 4)) return;
        u_char next_header = pck[header_len + 6];
        protocol_str = (next_header == 6) ? "TCP" : (next_header == 17) ? "UDP" : "OTHER";

        char Src_Ip[INET6_ADDRSTRLEN];
        inet_ntop(AF_INET6, pck + header_len + 8, Src_Ip, INET6_ADDRSTRLEN);
        src_ip = Src_Ip;

        if (next_header == 6 || next_header == 17) { // TCP or UDP
            const u_char* transport_header = pck + header_len + ipv6_header_len;
            dest_port = ntohs(*(uint16_t*)(transport_header + 2)); // Destination port
        }
        else {
            dest_port = 0;
        }
    }
    else {
        return;
    }

    // Update stats
    {
        std::lock_guard<std::mutex> lock(stats_mutex);
        TrafficKey key{ src_ip, dest_port, protocol_str };
        auto& stats = traffic_stats[key];

        auto now = std::chrono::steady_clock::now();
        auto time_diff = std::chrono::duration_cast<std::chrono::seconds>(now - stats.last_update).count();

        if (time_diff >= 5) {
            // Reset recent counts every 5 seconds
            stats.recent_count = 0;
            stats.recent_bytes = 0;
            stats.last_update = now;
        }

        stats.total_count++;
        stats.total_bytes += packet_len;
        stats.recent_count++;
        stats.recent_bytes += packet_len;
    }
}

void capture_for_ports_on_all_devices(const std::vector<int>& ports) {
    char errbuf[PCAP_ERRBUF_SIZE];
    pcap_if_t* alldevs;
    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) {
        log_message(std::string("Error finding devices: ") + errbuf);
        return;
    }
    std::vector<pcap_t*> handles;

    // Build combined filter expression for all ports (TCP and UDP)
    std::ostringstream filter_exp;
    for (size_t i = 0; i < ports.size(); ++i) {
        if (i > 0) filter_exp << " or ";
        filter_exp << "udp port " << ports[i] << " or tcp port " << ports[i];
    }
    std::string filter_str = filter_exp.str();

    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        pcap_t* handle = pcap_open_live(d->name, 65536, 1, 1000, errbuf);
        if (!handle) continue;

        struct bpf_program filter;
        if (pcap_compile(handle, &filter, filter_str.c_str(), 1, PCAP_NETMASK_UNKNOWN) == -1) {
            pcap_close(handle);
            continue;
        }
        if (pcap_setfilter(handle, &filter) == -1) {
            pcap_freecode(&filter);
            pcap_close(handle);
            continue;
        }
        pcap_freecode(&filter);

        handles.push_back(handle);
        {
            std::lock_guard<std::mutex> lock(devname_mutex);
            handle_to_devname[handle] = d->description ? d->description : d->name;
        }
    }

    if (!handles.empty()) {
        log_message("Started capturing on " + std::to_string(handles.size()) + " interface(s) for ports: " + filter_str);
    }

    std::vector<std::thread> threads;
    for (auto handle : handles) {
        threads.emplace_back([handle]() {
            pcap_loop(handle, 0, packet_handler, (u_char*)handle);
            });
    }

    std::this_thread::sleep_for(std::chrono::seconds(10)); // Capture duration

    for (auto handle : handles) pcap_breakloop(handle);
    for (auto& t : threads) if (t.joinable()) t.join();
    for (auto handle : handles) pcap_close(handle);
    pcap_freealldevs(alldevs);

    if (!handles.empty()) {
        log_message("Finished capturing for all ports.");
    }
}

int main() {
    log_file.open("packet_capture.log", std::ios::out | std::ios::app);
    if (!log_file.is_open()) {
        std::cerr << "Failed to open log file for writing." << std::endl;
        return 1;
    }

    csv_file.open("packet_capture.csv", std::ios::out | std::ios::app);
    if (!csv_file.is_open()) {
        std::cerr << "Failed to open CSV file for writing." << std::endl;
        return 1;
    }

    log_message("=== Starting UDP and TCP scan on all interfaces and ports ===");

    std::vector<int> ports = {
        53, 67, 68, 69, 123, 137, 138, 161, 162, 500, 514, 520,
        4500, 1812, 1813, 2049, 33434, 51820, 3478, 5060, 5353, 8000, 8080
    };

    capture_for_ports_on_all_devices(ports);

    log_message("=== Finished all scans ===");

    // Write CSV header
    log_csv("Source IP,Port,Protocol,Frequency,Total Frequency,Total Bytes");

    // Write summary lines to both log and CSV files
    {
        std::lock_guard<std::mutex> lock(stats_mutex);
        for (const auto& pair : traffic_stats) {
            const auto& key = pair.first;
            const auto& stats = pair.second;
            std::ostringstream oss;
            oss << "\"" << key.ip << "\","
                << key.port << ","
                << key.protocol << ","
                << stats.recent_count << ","  // Frequency (per 5 sec)
                << stats.total_count << ","   // Total Frequency
                << stats.total_bytes;         // Total Bytes

            log_csv(oss.str());    // Write to CSV file
            log_message(oss.str()); // Also print to console and log file
        }
    }

    log_message("=== End of Summary ===\n");

    log_file.close();
    csv_file.close();
    return 0;
}
