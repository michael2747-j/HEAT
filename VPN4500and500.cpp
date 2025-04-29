#include <iostream>
#include <vector>
#include <thread>
#include <mutex>
#include <fstream>
#include <sstream>
#include <pcap/pcap.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "ws2_32.lib")

// IKEv2 header struct
#pragma pack(push, 1)
struct IKEv2Header {
    uint64_t Initiator_SPI;
    uint64_t Responder_SPI;
    uint8_t Next_Payload;
    uint8_t Version;
    uint8_t Exchange_Type;
    uint8_t Flags;
    uint32_t Message_ID;
    uint32_t Length;
};
#pragma pack(pop)

// Helper to convert exchange type to string
const char* ikev2_exchange_type_str(uint8_t exchange_type) {
    switch (exchange_type) {
    case 34: return "IKE_SA_INIT";
    case 35: return "IKE_AUTH";
    case 36: return "CREATE_CHILD_SA";
    case 37: return "INFORMATIONAL";
    default: return "UNKNOWN";
    }
}

// Global log file and mutex
std::ofstream log_file;
std::mutex log_mutex;

// Thread-safe logging function
void log_message(const std::string& msg) {
    std::lock_guard<std::mutex> lock(log_mutex);
    std::cout << msg << std::endl;
    if (log_file.is_open()) {
        log_file << msg << std::endl;
        log_file.flush();
    }
}

// Packet handler callback
void packet_handler(u_char* user, const struct pcap_pkthdr* header, const u_char* pck) {
    pcap_t* handle = (pcap_t*)user;
    int linktype = pcap_datalink(handle);
    int header_len = 0;

    if (linktype == DLT_NULL) {
        header_len = 4;
    }
    else if (linktype == DLT_EN10MB) {
        header_len = 14;
    }
    else {
        return; // Unsupported link-layer
    }

    if (header->caplen < header_len + 8) return;

    int ip_version = (pck[header_len] >> 4);

    if (ip_version == 4) {
        if (header->caplen < header_len + 20) return;
        int ip_header_len = (pck[header_len] & 0x0F) * 4;
        if (header->caplen < header_len + ip_header_len + 8) return;
        u_char protocol = pck[header_len + 9];
        if (protocol != 17) return; // UDP only

        char Src_Ip[INET_ADDRSTRLEN];
        char Dest_Ip[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, pck + header_len + 12, Src_Ip, INET_ADDRSTRLEN);
        inet_ntop(AF_INET, pck + header_len + 16, Dest_Ip, INET_ADDRSTRLEN);

        const u_char* udp_header = pck + header_len + ip_header_len;
        uint16_t Src_Prt = ntohs(*(uint16_t*)(udp_header));
        uint16_t Dest_Prt = ntohs(*(uint16_t*)(udp_header + 2));
        uint16_t udp_length = ntohs(*(uint16_t*)(udp_header + 4));
        int udp_payload_len = udp_length - 8;

        bool is_vpn_port = (Src_Prt == 500 || Src_Prt == 4500 || Src_Prt == 51820 ||
            Dest_Prt == 500 || Dest_Prt == 4500 || Dest_Prt == 51820);

        if (!is_vpn_port) return;

        std::ostringstream oss;
        oss << "[IPv4] " << Src_Ip << ":" << Src_Prt << " -> " << Dest_Ip << ":" << Dest_Prt
            << " | Payload length: " << udp_payload_len << " bytes";

        if (Src_Prt == 51820 || Dest_Prt == 51820) {
            oss << " | WireGuard UDP packet";
        }
        else if (udp_payload_len >= sizeof(IKEv2Header)) {
            const IKEv2Header* ike_hdr = (const IKEv2Header*)(udp_header + 8);
            uint8_t major_version = (ike_hdr->Version >> 4);
            if (major_version == 2) {
                oss << " | IKEv2 Msg: Exchange=" << ikev2_exchange_type_str(ike_hdr->Exchange_Type)
                    << " MsgID=" << ntohl(ike_hdr->Message_ID);
            }
        }

        if ((Src_Prt == 4500 || Dest_Prt == 4500) && !(Src_Prt == 51820 || Dest_Prt == 51820)) {
            if (udp_payload_len >= 4) {
                const u_char* Nat_T_Header = udp_header + 8;
                if (Nat_T_Header[0] == 0 && Nat_T_Header[1] == 0 && Nat_T_Header[2] == 0 && Nat_T_Header[3] == 0) {
                    oss << " | NAT-T Encapsulated ESP packet";
                }
            }
        }

        log_message(oss.str());

    }
    else if (ip_version == 6) {
        const int ipv6_header_len = 40;
        if (header->caplen < header_len + ipv6_header_len + 8) return;
        u_char next_header = pck[header_len + 6];
        if (next_header != 17) return;

        char Src_Ip[INET6_ADDRSTRLEN];
        char Dest_Ip[INET6_ADDRSTRLEN];
        inet_ntop(AF_INET6, pck + header_len + 8, Src_Ip, INET6_ADDRSTRLEN);
        inet_ntop(AF_INET6, pck + header_len + 24, Dest_Ip, INET6_ADDRSTRLEN);

        const u_char* udp_header = pck + header_len + ipv6_header_len;
        uint16_t Src_Prt = ntohs(*(uint16_t*)(udp_header));
        uint16_t Dest_Prt = ntohs(*(uint16_t*)(udp_header + 2));
        uint16_t udp_length = ntohs(*(uint16_t*)(udp_header + 4));
        int udp_payload_len = udp_length - 8;

        bool is_vpn_port = (Src_Prt == 500 || Src_Prt == 4500 || Src_Prt == 51820 ||
            Dest_Prt == 500 || Dest_Prt == 4500 || Dest_Prt == 51820);

        if (!is_vpn_port) return;

        std::ostringstream oss;
        oss << "[IPv6] " << Src_Ip << ":" << Src_Prt << " -> " << Dest_Ip << ":" << Dest_Prt
            << " | Payload length: " << udp_payload_len << " bytes";

        if (Src_Prt == 51820 || Dest_Prt == 51820) {
            oss << " | WireGuard UDP packet";
        }
        else if (udp_payload_len >= sizeof(IKEv2Header)) {
            const IKEv2Header* ike_hdr = (const IKEv2Header*)(udp_header + 8);
            uint8_t major_version = (ike_hdr->Version >> 4);
            if (major_version == 2) {
                oss << " | IKEv2 Msg: Exchange=" << ikev2_exchange_type_str(ike_hdr->Exchange_Type)
                    << " MsgID=" << ntohl(ike_hdr->Message_ID);
            }
        }

        if ((Src_Prt == 4500 || Dest_Prt == 4500) && !(Src_Prt == 51820 || Dest_Prt == 51820)) {
            if (udp_payload_len >= 4) {
                const u_char* Nat_T_Header = udp_header + 8;
                if (Nat_T_Header[0] == 0 && Nat_T_Header[1] == 0 && Nat_T_Header[2] == 0 && Nat_T_Header[3] == 0) {
                    oss << " | NAT-T Encapsulated ESP packet";
                }
            }
        }

        log_message(oss.str());

    }
    else {
        // Unsupported IP version
        return;
    }
}

// Capture thread function for each interface
void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf);
    if (!handle) {
        std::lock_guard<std::mutex> lock(log_mutex);
        std::cerr << "Failed to open device " << (device->description ? device->description : device->name)
            << ": " << errbuf << std::endl;
        return;
    }

    struct bpf_program filter;
    const char* filter_exp = "udp port 500 or udp port 4500 or udp port 51820";

    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1) {
        std::lock_guard<std::mutex> lock(log_mutex);
        std::cerr << "Couldn't parse filter '" << filter_exp << "' on "
            << (device->description ? device->description : device->name)
            << ": " << pcap_geterr(handle) << std::endl;
        pcap_close(handle);
        return;
    }

    if (pcap_setfilter(handle, &filter) == -1) {
        std::lock_guard<std::mutex> lock(log_mutex);
        std::cerr << "Couldn't install filter on "
            << (device->description ? device->description : device->name)
            << ": " << pcap_geterr(handle) << std::endl;
        pcap_freecode(&filter);
        pcap_close(handle);
        return;
    }

    pcap_freecode(&filter);

    {
        std::lock_guard<std::mutex> lock(log_mutex);
        std::cout << "Started capturing on interface: "
            << (device->description ? device->description : device->name) << std::endl;
        if (log_file.is_open()) {
            log_file << "Started capturing on interface: "
                << (device->description ? device->description : device->name) << std::endl;
            log_file.flush();
        }
    }

    pcap_loop(handle, 0, packet_handler, (u_char*)handle);

    pcap_close(handle);
}

int main() {
    // Open log file
    log_file.open("packet_capture.log", std::ios::out | std::ios::app);
    if (!log_file.is_open()) {
        std::cerr << "Failed to open log file for writing." << std::endl;
        return 1;
    }

    pcap_if_t* alldevs;
    char errbuf[PCAP_ERRBUF_SIZE];

    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) {
        std::cerr << "Error finding devices: " << errbuf << std::endl;
        return 1;
    }

    std::vector<std::thread> threads;

    // Start capture on all devices
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        threads.emplace_back(capture_on_device, d);
    }

    std::cout << "Capturing on all available interfaces..." << std::endl;
    std::cout << "Press Ctrl+C to stop." << std::endl;

    // Wait for all capture threads to finish (runs until interrupted)
    for (auto& t : threads) {
        if (t.joinable())
            t.join();
    }

    pcap_freealldevs(alldevs);

    log_file.close();
    return 0;
}
