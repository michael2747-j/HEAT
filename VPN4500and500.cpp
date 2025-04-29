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
    std::lock_guard<std::mutex> lock(logMutex);  // Lock mutex to prevent concurrent writes
    std::cout << msg << std::endl;
    if (logFile.is_open()) {
        logFile << msg << std::endl;
        logFile.flush();  // Ensure immediate write to disk
    }
}

// IPv4 header structure (simplified)
struct ip_header {
    u_char  Vhl;       // Version (4 bits) and header length (4 bits)
    u_char  Tos;       // Type of service
    u_short Len;       // Total length
    u_short Id;        // Identification
    u_short Off;       // Fragment offset field
    u_char  Ttl;       // Time to live
    u_char  P;         // Protocol
    u_short Sum;       // Header checksum
    struct  in_addr Src_Ip, Dest_Ip;  // Source and destination IP addresses
};

#define IP_HL(ip)       (((ip)->Vhl) & 0x0f)    // Extract IP header length (in 32-bit words)
#define IP_V(ip)        (((ip)->Vhl) >> 4)      // Extract IP version

// UDP header structure
struct udp_header {
    u_short Src_Prt;   // Source port
    u_short Dest_Prt;  // Destination port
    u_short Len;       // Length of UDP packet
    u_short Checksum;  // Checksum
};

// TCP header structure
struct tcp_header {
    u_short Src_Prt;   // Source port
    u_short Dest_Prt;  // Destination port
    u_int Seq;         // Sequence number
    u_int Ack_Seq;     // Acknowledgment number
    u_char  Doff_Res;  // Data offset and reserved bits
    u_char  Flags;     // TCP flags
    u_short Window;    // Window size
    u_short Checksum;  // Checksum
    u_short Urg_Ptr;   // Urgent pointer
};

// IKEv2 header struct for UDP ports 500 and 4500 (packed to avoid padding)
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

// Helper function to convert IKEv2 exchange type to string
const char* ikev2_exchange_type_str(uint8_t exchange_type) {
    switch (exchange_type) {
    case 34: return "IKE_SA_INIT";
    case 35: return "IKE_AUTH";
    case 36: return "CREATE_CHILD_SA";
    case 37: return "INFORMATIONAL";
    default: return "UNKNOWN";
    }
}

// Convert an IPv4 address to a string
std::string ip_to_string(const struct in_addr& Ip_Addr) {
    char Ip_Str[INET_ADDRSTRLEN];
    inet_ntop(AF_INET, &Ip_Addr, Ip_Str, INET_ADDRSTRLEN);
    return std::string(Ip_Str);
}

// Parse and log basic DNS header info from DNS packet data
void parse_dns(const u_char* Data, int Size) {
    if (Size < 12) return; // DNS header is 12 bytes minimum
    u_short Id = ntohs(*(u_short*)(Data));           // Transaction ID
    u_short Flags = ntohs(*(u_short*)(Data + 2));    // Flags
    u_short Qdcount = ntohs(*(u_short*)(Data + 4));  // Number of questions
    u_short Ancount = ntohs(*(u_short*)(Data + 6));  // Number of answers

    std::ostringstream oss;
    oss << "DNS Packet - ID: " << Id << ", Flags: 0x" << std::hex << Flags << std::dec
        << ", Questions: " << Qdcount << ", Answers: " << Ancount;
    log(oss.str());
}

// Packet handler callback invoked by pcap for each captured packet
void packet_handler(u_char* Param, const struct pcap_pkthdr* Header, const u_char* Pkt_Data) {
    // Skip Ethernet header to get IP header pointer
    const u_char* Ip_Pck = Pkt_Data + ETHERNET_HEADER_SIZE;
    struct ip_header* Ip = (struct ip_header*)Ip_Pck;

    int Ip_Header_Len = IP_HL(Ip) * 4;
    if (Ip_Header_Len < 20) return; // Invalid header length, discard packet
    if (IP_V(Ip) != 4) return;      // Only process IPv4 packets

    // Convert source and destination IP addresses to strings
    std::string Src_Ip_Str = ip_to_string(Ip->Src_Ip);
    std::string Dest_Ip_Str = ip_to_string(Ip->Dest_Ip);

    // Process UDP packets
    if (Ip->P == IPPROTO_UDP) {
        const udp_header* Udp = (udp_header*)(Ip_Pck + Ip_Header_Len);
        u_short Src_Prt = ntohs(Udp->Src_Prt);
        u_short Dest_Prt = ntohs(Udp->Dest_Prt);

        // DNS traffic (UDP port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {
            std::ostringstream oss;
            oss << "[DNS - UDP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());

            int Dns_Data_Len = ntohs(Udp->Len) - sizeof(udp_header);
            const u_char* Dns_Data = Ip_Pck + Ip_Header_Len + sizeof(udp_header);
            parse_dns(Dns_Data, Dns_Data_Len);
        }
        // IKE/IPsec traffic (UDP ports 500 and 4500)
        else if (Src_Prt == 500 || Dest_Prt == 500 || Src_Prt == 4500 || Dest_Prt == 4500) {
            std::ostringstream oss;
            oss << "[IKE/IPsec UDP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;

            int udp_length = ntohs(Udp->Len);
            int udp_payload_len = udp_length - sizeof(udp_header);

            // Attempt to parse IKEv2 header if payload length is sufficient
            if (udp_payload_len >= (int)sizeof(IKEv2Header)) {
                const IKEv2Header* ike_hdr = (const IKEv2Header*)(Ip_Pck + Ip_Header_Len + sizeof(udp_header));
                uint8_t major_version = (ike_hdr->Version >> 4);
                if (major_version == 2) {
                    oss << " | IKEv2 Msg: Exchange=" << ikev2_exchange_type_str(ike_hdr->Exchange_Type)
                        << " MsgID=" << ntohl(ike_hdr->Message_ID);
                }
            }
            log(oss.str());
        }
        // WireGuard traffic (UDP port 51820)
        else if (Src_Prt == 51820 || Dest_Prt == 51820) {
            std::ostringstream oss;
            oss << "[WireGuard UDP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
        }
    }
    // Process TCP packets
    else if (Ip->P == IPPROTO_TCP) {
        const tcp_header* Tcp = (tcp_header*)(Ip_Pck + Ip_Header_Len);
        u_short Src_Prt = ntohs(Tcp->Src_Prt);
        u_short Dest_Prt = ntohs(Tcp->Dest_Prt);

        // DNS traffic (TCP port 53)
        if (Src_Prt == 53 || Dest_Prt == 53) {
            std::ostringstream oss;
            oss << "[DNS - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
            // DNS over TCP parsing can be added here if needed
        }
        // BGP traffic (TCP port 179)
        else if (Src_Prt == 179 || Dest_Prt == 179) {
            std::ostringstream oss;
            oss << "[BGP - TCP] " << Src_Ip_Str << ":" << Src_Prt << " -> " << Dest_Ip_Str << ":" << Dest_Prt;
            log(oss.str());
            // BGP parsing can be added here if needed
        }
    }
}

// Thread function to start packet capture on a single network device
void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];

    // Open the device for live capture
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf);
    if (!handle) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Failed to open device " << (device->description ? device->description : device->name)
            << ": " << errbuf << std::endl;
        return;
    }

    // Define filter expression for DNS, BGP, IKE/IPsec, and WireGuard ports
    const char* filter_exp = "port 53 or port 179 or port 500 or port 4500 or port 51820";

    struct bpf_program filter;
    // Compile the filter expression
    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't parse filter " << filter_exp << ": " << pcap_geterr(handle) << std::endl;
        pcap_close(handle);
        return;
    }

    // Set the compiled filter
    if (pcap_setfilter(handle, &filter) == -1) {
        std::lock_guard<std::mutex> lock(logMutex);
        std::cerr << "Couldn't install filter: " << pcap_geterr(handle) << std::endl;
        pcap_freecode(&filter);
        pcap_close(handle);
        return;
    }

    pcap_freecode(&filter);

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

    // Start the capture loop; capture indefinitely until interrupted
    pcap_loop(handle, 0, packet_handler, nullptr);

    // Cleanup
    pcap_close(handle);
}

int main() {
    // Open the log file for appending
    logFile.open("capture_output.log", std::ios::out | std::ios::app);
    if (!logFile.is_open()) {
        std::cerr << "Failed to open log file for writing." << std::endl;
        return 1;
    }

    pcap_if_t* alldevs;
    char errbuf[PCAP_ERRBUF_SIZE];

    // Retrieve the list of network devices available for capture
    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) {
        log(std::string("Error finding devices: ") + errbuf);
        logFile.close();
        return 1;
    }

    std::vector<std::thread> threads;

    // Start a capture thread for each network device
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        threads.emplace_back(capture_on_device, d);
    }

    log("Capturing on all available interfaces...");
    log("Press Ctrl+C to stop.");

    // Wait for all capture threads to finish (runs until interrupted)
    for (auto& t : threads) {
        if (t.joinable())
            t.join();
    }

    // Free the device list and close log file
    pcap_freealldevs(alldevs);
    logFile.close();

    return 0;
}
