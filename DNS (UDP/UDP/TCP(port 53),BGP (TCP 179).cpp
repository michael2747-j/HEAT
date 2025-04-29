#include <pcap.h>
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "wpcap.lib")
#pragma comment(lib, "ws2_32.lib")

#define ETHERNET_HEADER_SIZE 14 // Standard Ethernet header size in bytes

// Global log file stream and mutex for thread-safe logging
std::ofstream logFile;
std::mutex logMutex;

// Thread-safe logging function: prints to console and writes to log file
void log(const std::string& msg) {
    std::lock_guard<std::mutex> lock(logMutex);
    std::cout << msg << std::endl;
    if (logFile.is_open()) {
        logFile << msg << std::endl;
        logFile.flush();
    }
}

// IPv4 header structure (simplified)
struct ip_header {
    u_char  Vhl;
    u_char  Tos;
    u_short Len;
    u_short Id;
    u_short Off;
    u_char  Ttl;
    u_char  P;
    u_short Sum;
    struct  in_addr Src_Ip, Dest_Ip;
};

#define IP_HL(ip)       (((ip)->Vhl) & 0x0f)    // Header length macro
#define IP_V(ip)        (((ip)->Vhl) >> 4)      // Version macro

// UDP header structure
struct udp_header {
    u_short Src_Prt;
    u_short Dest_Prt;
    u_short Len;
    u_short Checksum;
};

// TCP header structure
struct tcp_header {
    u_short Src_Prt;
    u_short Dest_Prt;
    u_int Seq;
    u_int Ack_Seq;
    u_char  Doff_Res;
    u_char  Flags;
    u_short Window;
    u_short Checksum;
    u_short Urg_Ptr;
};

// Convert an IPv4 address to string
std::string ip_to_string(const struct in_addr& Ip_Addr) {
    char Ip_Str[INET_ADDRSTRLEN];
    inet_ntop(AF_INET, &Ip_Addr, Ip_Str, INET_ADDRSTRLEN);
    return std::string(Ip_Str);
}

// Parse and print basic DNS header info
void parse_dns(const u_char* Data, int Size) {
    if (Size < 12) return; // DNS header is 12 bytes
    u_short Id = ntohs(*(u_short*)(Data));
    u_short Flags = ntohs(*(u_short*)(Data + 2));
    u_short Qdcount = ntohs(*(u_short*)(Data + 4));
    u_short Ancount = ntohs(*(u_short*)(Data + 6));
    std::ostringstream oss;
    oss << "DNS Packet - ID: " << Id << ", Flags: 0x" << std::hex << Flags << std::dec
        << ", Questions: " << Qdcount << ", Answers: " << Ancount;
    log(oss.str());
}

// Packet handler callback for each captured packet
void packet_handler(u_char* Param, const struct pcap_pkthdr* Header, const u_char* Pkt_Data) {
    // Skip Ethernet header to get to IP header
    const u_char* Ip_Pck = Pkt_Data + ETHERNET_HEADER_SIZE;
    struct ip_header* Ip = (struct ip_header*)Ip_Pck;

    int Ip_Header_Len = IP_HL(Ip) * 4;
    if (Ip_Header_Len < 20) return; // Invalid header length
    if (IP_V(Ip) != 4) return;      // Only process IPv4 packets

    std::string Src_Ip_Str = ip_to_string(Ip->Src_Ip);
    std::string Dest_Ip_Str = ip_to_string(Ip->Dest_Ip);

    // Check protocol: UDP or TCP
    if (Ip->P == IPPROTO_UDP) {
        const udp_header* Udp = (udp_header*)(Ip_Pck + Ip_Header_Len);
        u_short Src_Prt = ntohs(Udp->Src_Prt);
        u_short Dest_Prt = ntohs(Udp->Dest_Prt);

        // DNS (port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {
            std::ostringstream oss;
            oss << "[DNS - UDP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
            int Dns_Data_Len = ntohs(Udp->Len) - sizeof(udp_header);
            const u_char* Dns_Data = Ip_Pck + Ip_Header_Len + sizeof(udp_header);
            parse_dns(Dns_Data, Dns_Data_Len);
        }
    }
    else if (Ip->P == IPPROTO_TCP) {
        const tcp_header* Tcp = (tcp_header*)(Ip_Pck + Ip_Header_Len);
        u_short Src_Prt = ntohs(Tcp->Src_Prt);
        u_short Dest_Prt = ntohs(Tcp->Dest_Prt);

        // DNS (port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {
            std::ostringstream oss;
            oss << "[DNS - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
            // DNS over TCP parsing can be added here if needed
        }
        // BGP (port 179)
        else if (Src_Prt == 179 || Dest_Prt == 179) {
            std::ostringstream oss;
            oss << "[BGP - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
            // BGP parsing can be added here if needed
        }
    }
}

// Thread function to start capture on a single device
void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];
    // Open the network interface for capturing
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf);
    if (!handle) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Failed to open device " << (device->description ? device->description : device->name)
            << ": " << errbuf << std::endl;
        return;
    }

    // Filter: DNS (53), BGP (179)
    const char* filter_exp = "port 53 or port 179";
    struct bpf_program filter;
    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't parse filter " << filter_exp << ": " << pcap_geterr(handle) << std::endl;
        pcap_close(handle);
        return;
    }

    if (pcap_setfilter(handle, &filter) == -1) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't install filter: " << pcap_geterr(handle) << std::endl;
        pcap_freecode(&filter);
        pcap_close(handle);
        return;
    }

    pcap_freecode(&filter);

    // Log start of capture on this interface
    {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cout << "Started capturing on interface: "
            << (device->description ? device->description : device->name) << std::endl;
        if (logFile.is_open()) {
            logFile << "Started capturing on interface: "
                << (device->description ? device->description : device->name) << std::endl;
            logFile.flush();
        }
    }

    // Start packet capture loop (runs until interrupted)
    pcap_loop(handle, 0, packet_handler, nullptr);

    pcap_close(handle);
}

int main() {
    // Open log file
    logFile.open("capture_output.log", std::ios::out | std::ios::app);
    if (!logFile.is_open()) {
        std::cerr << "Failed to open log file for writing." << std::endl;
        return 1;
    }

    pcap_if_t* alldevs;
    char errbuf[PCAP_ERRBUF_SIZE];

    // Find all available network devices
    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) {
        log(std::string("Error finding devices: ") + errbuf);
        logFile.close();
        return 1;
    }

    // Start a capture thread for each interface
    std::vector<std::thread> threads;
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        threads.emplace_back(capture_on_device, d);
    }

    log("Capturing on all available interfaces...");
    log("Press Ctrl+C to stop.");

    // Wait for all threads to finish (which will be when the program is interrupted)
    for (auto& t : threads) {
        if (t.joinable())
            t.join();
    }

    pcap_freealldevs(alldevs);
    logFile.close();

    return 0;
}
