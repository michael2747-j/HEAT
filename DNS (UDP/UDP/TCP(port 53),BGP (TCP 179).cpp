#include <pcap.h>           // For packet capture
#include <iostream>         // For standard I/O
#include <fstream>          // For file I/O
#include <sstream>          // For string streams
#include <string>           // For std::string
#include <vector>           // For std::vector
#include <thread>           // For std::thread
#include <mutex>            // For std::mutex
#include <winsock2.h>       // For Windows sockets
#include <ws2tcpip.h>       // For inet_ntop

#pragma comment(lib, "wpcap.lib")    // Link with WinPcap library
#pragma comment(lib, "ws2_32.lib")   // Link with Winsock library

#define ETHERNET_HEADER_SIZE 14 // Standard Ethernet header size in bytes

// Global log file stream and mutex for thread-safe logging
std::ofstream logFile;         // Output file stream for logging
std::mutex logMutex;           // Mutex to synchronize log access

// Thread-safe logging function: prints to console and writes to log file
void log(const std::string& msg) {
    std::lock_guard<std::mutex> lock(logMutex);   // Lock mutex for thread safety
    std::cout << msg << std::endl;                // Print to console
    if (logFile.is_open()) {                      // If log file is open
        logFile << msg << std::endl;              // Write to log file
        logFile.flush();                          // Flush to ensure immediate write
    }
}

// IPv4 header structure (simplified)
struct ip_header {
    u_char  Vhl;              // Version and header length
    u_char  Tos;              // Type of service
    u_short Len;              // Total length
    u_short Id;               // Identification
    u_short Off;              // Fragment offset field
    u_char  Ttl;              // Time to live
    u_char  P;                // Protocol
    u_short Sum;              // Header checksum
    struct  in_addr Src_Ip, Dest_Ip; // Source and destination IP addresses
};

#define IP_HL(ip)       (((ip)->Vhl) & 0x0f)    // Macro to get header length
#define IP_V(ip)        (((ip)->Vhl) >> 4)      // Macro to get version

// UDP header structure
struct udp_header {
    u_short Src_Prt;          // Source port
    u_short Dest_Prt;         // Destination port
    u_short Len;              // Length
    u_short Checksum;         // Checksum
};

// TCP header structure
struct tcp_header {
    u_short Src_Prt;          // Source port
    u_short Dest_Prt;         // Destination port
    u_int Seq;                // Sequence number
    u_int Ack_Seq;            // Acknowledgement number
    u_char  Doff_Res;         // Data offset and reserved
    u_char  Flags;            // Flags
    u_short Window;           // Window size
    u_short Checksum;         // Checksum
    u_short Urg_Ptr;          // Urgent pointer
};

// Convert an IPv4 address to string
std::string ip_to_string(const struct in_addr& Ip_Addr) {
    char Ip_Str[INET_ADDRSTRLEN];                      // Buffer for IP string
    inet_ntop(AF_INET, &Ip_Addr, Ip_Str, INET_ADDRSTRLEN); // Convert to string
    return std::string(Ip_Str);                        // Return as std::string
}

// Parse and print basic DNS header info
void parse_dns(const u_char* Data, int Size) {
    if (Size < 12) return;                             // DNS header is 12 bytes; check size
    u_short Id = ntohs(*(u_short*)(Data));             // Transaction ID
    u_short Flags = ntohs(*(u_short*)(Data + 2));      // Flags
    u_short Qdcount = ntohs(*(u_short*)(Data + 4));    // Number of questions
    u_short Ancount = ntohs(*(u_short*)(Data + 6));    // Number of answers
    std::ostringstream oss;                            // String stream for output
    oss << "DNS Packet - ID: " << Id << ", Flags: 0x" << std::hex << Flags << std::dec
        << ", Questions: " << Qdcount << ", Answers: " << Ancount;
    log(oss.str());                                    // Log DNS packet info
}

// Packet handler callback for each captured packet
void packet_handler(u_char* Param, const struct pcap_pkthdr* Header, const u_char* Pkt_Data) {
    // Skip Ethernet header to get to IP header
    const u_char* Ip_Pck = Pkt_Data + ETHERNET_HEADER_SIZE; // Point to IP header
    struct ip_header* Ip = (struct ip_header*)Ip_Pck;       // Cast to IP header

    int Ip_Header_Len = IP_HL(Ip) * 4;                      // Calculate IP header length
    if (Ip_Header_Len < 20) return;                         // Invalid header length check
    if (IP_V(Ip) != 4) return;                              // Only process IPv4 packets

    std::string Src_Ip_Str = ip_to_string(Ip->Src_Ip);      // Convert source IP to string
    std::string Dest_Ip_Str = ip_to_string(Ip->Dest_Ip);    // Convert dest IP to string

    // Check protocol: UDP or TCP
    if (Ip->P == IPPROTO_UDP) {                             // If UDP packet
        const udp_header* Udp = (udp_header*)(Ip_Pck + Ip_Header_Len); // Point to UDP header
        u_short Src_Prt = ntohs(Udp->Src_Prt);              // Source port
        u_short Dest_Prt = ntohs(Udp->Dest_Prt);            // Destination port

        // DNS (port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {              // Check for DNS port
            std::ostringstream oss;
            oss << "[DNS - UDP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());                                 // Log DNS UDP packet
            int Dns_Data_Len = ntohs(Udp->Len) - sizeof(udp_header); // DNS data length
            const u_char* Dns_Data = Ip_Pck + Ip_Header_Len + sizeof(udp_header); // Point to DNS data
            parse_dns(Dns_Data, Dns_Data_Len);              // Parse DNS data
        }
    }
    else if (Ip->P == IPPROTO_TCP) {                        // If TCP packet
        const tcp_header* Tcp = (tcp_header*)(Ip_Pck + Ip_Header_Len); // Point to TCP header
        u_short Src_Prt = ntohs(Tcp->Src_Prt);              // Source port
        u_short Dest_Prt = ntohs(Tcp->Dest_Prt);            // Destination port

        // DNS (port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {              // Check for DNS over TCP
            std::ostringstream oss;
            oss << "[DNS - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());                                 // Log DNS TCP packet
            // DNS over TCP parsing can be added here if needed
        }
        // BGP (port 179)
        else if (Src_Prt == 179 || Dest_Prt == 179) {       // Check for BGP port
            std::ostringstream oss;
            oss << "[BGP - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());                                 // Log BGP TCP packet
            // BGP parsing can be added here if needed
        }
    }
}

// Thread function to start capture on a single device
void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];                         // Error buffer
    // Open the network interface for capturing
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf); // Open device
    if (!handle) {                                         // Check if handle is valid
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Failed to open device " << (device->description ? device->description : device->name)
            << ": " << errbuf << std::endl;
        return;
    }

    // Filter: DNS (53), BGP (179)
    const char* filter_exp = "port 53 or port 179";        // Filter expression
    struct bpf_program filter;                             // BPF filter program
    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1) { // Compile filter
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't parse filter " << filter_exp << ": " << pcap_geterr(handle) << std::endl;
        pcap_close(handle);
        return;
    }

    if (pcap_setfilter(handle, &filter) == -1) {           // Set filter
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't install filter: " << pcap_geterr(handle) << std::endl;
        pcap_freecode(&filter);
        pcap_close(handle);
        return;
    }

    pcap_freecode(&filter);                                // Free filter code

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
    pcap_loop(handle, 0, packet_handler, nullptr);         // Begin packet capture loop

    pcap_close(handle);                                    // Close the device handle
}

int main() {
    // Open log file
    logFile.open("capture_output.log", std::ios::out | std::ios::app); // Open log file for appending
    if (!logFile.is_open()) {                               // Check if log file opened
        std::cerr << "Failed to open log file for writing." << std::endl;
        return 1;
    }

    pcap_if_t* alldevs;                                     // Pointer to all devices
    char errbuf[PCAP_ERRBUF_SIZE];                          // Error buffer

    // Find all available network devices
    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) { // Find devices
        log(std::string("Error finding devices: ") + errbuf);
        logFile.close();
        return 1;
    }

    // Start a capture thread for each interface
    std::vector<std::thread> threads;                       // Vector of threads
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) { // Iterate over devices
        threads.emplace_back(capture_on_device, d);         // Launch thread for device
    }

    log("Capturing on all available interfaces...");         // Log capture start
    log("Press Ctrl+C to stop.");                            // Log stop instruction

    // Wait for all threads to finish (which will be when the program is interrupted)
    for (auto& t : threads) {                               // Iterate over threads
        if (t.joinable())                                   // If thread is joinable
            t.join();                                       // Wait for thread to finish
    }

    pcap_freealldevs(alldevs);                              // Free device list
    logFile.close();                                        // Close log file

    return 0;                                               // Exit program
}
