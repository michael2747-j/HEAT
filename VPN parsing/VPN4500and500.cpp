#include <iostream>          // For standard I/O streams
#include <vector>            // For using std::vector container
#include <thread>            // For multithreading support
#include <mutex>             // For mutexes to ensure thread safety
#include <fstream>           // For file input/output streams
#include <sstream>           // For string stream operations
#include <pcap/pcap.h>       // For packet capture (libpcap/WinPcap)
#include <winsock2.h>        // For Windows sockets API
#include <ws2tcpip.h>        // For IP address conversion functions

#pragma comment(lib, "ws2_32.lib") // Link with Winsock library

// Define IKEv2 header structure, packed to avoid padding
#pragma pack(push, 1)
struct IKEv2Header {
    uint64_t Initiator_SPI;   // Initiator's Security Parameter Index
    uint64_t Responder_SPI;   // Responder's Security Parameter Index
    uint8_t Next_Payload;     // Next payload type
    uint8_t Version;          // IKE version
    uint8_t Exchange_Type;    // Type of IKE exchange
    uint8_t Flags;            // Flags
    uint32_t Message_ID;      // Message ID
    uint32_t Length;          // Total length of the message
};
#pragma pack(pop)             // Restore previous packing alignment

// Helper function to convert IKEv2 exchange type to a string
const char* ikev2_exchange_type_str(uint8_t exchange_type) {
    switch (exchange_type) {
    case 34: return "IKE_SA_INIT";         // Exchange type 34
    case 35: return "IKE_AUTH";            // Exchange type 35
    case 36: return "CREATE_CHILD_SA";     // Exchange type 36
    case 37: return "INFORMATIONAL";       // Exchange type 37
    default: return "UNKNOWN";             // Unknown type
    }
}

// Global log file stream
std::ofstream log_file;
// Global mutex for thread-safe logging
std::mutex log_mutex;

// Thread-safe function to log messages to console and file
void log_message(const std::string& msg) {
    std::lock_guard<std::mutex> lock(log_mutex); // Lock mutex for thread safety
    std::cout << msg << std::endl;               // Print message to console
    if (log_file.is_open()) {                    // If log file is open
        log_file << msg << std::endl;            // Write message to log file
        log_file.flush();                        // Flush output to ensure it's written
    }
}

// Callback function called by pcap for each captured packet
void packet_handler(u_char* user, const struct pcap_pkthdr* header, const u_char* pck) {
    pcap_t* handle = (pcap_t*)user;              // Cast user data to pcap handle
    int linktype = pcap_datalink(handle);        // Get link-layer type
    int header_len = 0;                          // Initialize header length

    if (linktype == DLT_NULL) {                  // If link-layer is NULL (loopback)
        header_len = 4;                          // Set header length to 4 bytes
    }
    else if (linktype == DLT_EN10MB) {           // If Ethernet
        header_len = 14;                         // Set header length to 14 bytes
    }
    else {
        return;                                  // Unsupported link-layer, exit
    }

    if (header->caplen < header_len + 8) return; // Not enough data for IP header, exit

    int ip_version = (pck[header_len] >> 4);     // Extract IP version from header

    if (ip_version == 4) {                       // IPv4 packet
        if (header->caplen < header_len + 20) return; // Not enough data for IPv4 header
        int ip_header_len = (pck[header_len] & 0x0F) * 4; // Calculate IPv4 header length
        if (header->caplen < header_len + ip_header_len + 8) return; // Not enough data for UDP header
        u_char protocol = pck[header_len + 9];   // Extract protocol field
        if (protocol != 17) return;              // Not UDP, exit

        char Src_Ip[INET_ADDRSTRLEN];            // Buffer for source IP string
        char Dest_Ip[INET_ADDRSTRLEN];           // Buffer for destination IP string
        inet_ntop(AF_INET, pck + header_len + 12, Src_Ip, INET_ADDRSTRLEN); // Convert source IP to string
        inet_ntop(AF_INET, pck + header_len + 16, Dest_Ip, INET_ADDRSTRLEN); // Convert dest IP to string

        const u_char* udp_header = pck + header_len + ip_header_len; // Pointer to UDP header
        uint16_t Src_Prt = ntohs(*(uint16_t*)(udp_header));          // Source port
        uint16_t Dest_Prt = ntohs(*(uint16_t*)(udp_header + 2));     // Destination port
        uint16_t udp_length = ntohs(*(uint16_t*)(udp_header + 4));   // UDP length
        int udp_payload_len = udp_length - 8;                        // UDP payload length

        // Check if either source or destination port is a VPN-related port
        bool is_vpn_port = (Src_Prt == 500 || Src_Prt == 4500 || Src_Prt == 51820 ||
            Dest_Prt == 500 || Dest_Prt == 4500 || Dest_Prt == 51820);

        if (!is_vpn_port) return;                // Not a VPN packet, exit

        std::ostringstream oss;                  // Create a string stream for logging
        oss << "[IPv4] " << Src_Ip << ":" << Src_Prt << " -> " << Dest_Ip << ":" << Dest_Prt
            << " | Payload length: " << udp_payload_len << " bytes"; // Log basic packet info

        if (Src_Prt == 51820 || Dest_Prt == 51820) { // If WireGuard port
            oss << " | WireGuard UDP packet";        // Log as WireGuard
        }
        else if (udp_payload_len >= sizeof(IKEv2Header)) { // If enough payload for IKEv2 header
            const IKEv2Header* ike_hdr = (const IKEv2Header*)(udp_header + 8); // Point to IKEv2 header
            uint8_t major_version = (ike_hdr->Version >> 4);                   // Extract IKEv2 major version
            if (major_version == 2) {                                          // If IKEv2
                oss << " | IKEv2 Msg: Exchange=" << ikev2_exchange_type_str(ike_hdr->Exchange_Type)
                    << " MsgID=" << ntohl(ike_hdr->Message_ID);                // Log exchange type and message ID
            }
        }

        // Check for NAT-T (NAT Traversal) encapsulated ESP packet
        if ((Src_Prt == 4500 || Dest_Prt == 4500) && !(Src_Prt == 51820 || Dest_Prt == 51820)) {
            if (udp_payload_len >= 4) {               // Ensure enough payload
                const u_char* Nat_T_Header = udp_header + 8; // Point to NAT-T header
                if (Nat_T_Header[0] == 0 && Nat_T_Header[1] == 0 && Nat_T_Header[2] == 0 && Nat_T_Header[3] == 0) {
                    oss << " | NAT-T Encapsulated ESP packet"; // Log as NAT-T ESP
                }
            }
        }

        log_message(oss.str());                       // Log the constructed message

    }
    else if (ip_version == 6) {                       // IPv6 packet
        const int ipv6_header_len = 40;               // IPv6 header is always 40 bytes
        if (header->caplen < header_len + ipv6_header_len + 8) return; // Not enough data for UDP header
        u_char next_header = pck[header_len + 6];     // Extract next header field
        if (next_header != 17) return;                // Not UDP, exit

        char Src_Ip[INET6_ADDRSTRLEN];                // Buffer for source IPv6 string
        char Dest_Ip[INET6_ADDRSTRLEN];               // Buffer for dest IPv6 string
        inet_ntop(AF_INET6, pck + header_len + 8, Src_Ip, INET6_ADDRSTRLEN);  // Convert source IPv6 to string
        inet_ntop(AF_INET6, pck + header_len + 24, Dest_Ip, INET6_ADDRSTRLEN);// Convert dest IPv6 to string

        const u_char* udp_header = pck + header_len + ipv6_header_len; // Pointer to UDP header
        uint16_t Src_Prt = ntohs(*(uint16_t*)(udp_header));            // Source port
        uint16_t Dest_Prt = ntohs(*(uint16_t*)(udp_header + 2));       // Destination port
        uint16_t udp_length = ntohs(*(uint16_t*)(udp_header + 4));     // UDP length
        int udp_payload_len = udp_length - 8;                          // UDP payload length

        // Check if either source or destination port is a VPN-related port
        bool is_vpn_port = (Src_Prt == 500 || Src_Prt == 4500 || Src_Prt == 51820 ||
            Dest_Prt == 500 || Dest_Prt == 4500 || Dest_Prt == 51820);

        if (!is_vpn_port) return;                // Not a VPN packet, exit

        std::ostringstream oss;                  // Create a string stream for logging
        oss << "[IPv6] " << Src_Ip << ":" << Src_Prt << " -> " << Dest_Ip << ":" << Dest_Prt
            << " | Payload length: " << udp_payload_len << " bytes"; // Log basic packet info

        if (Src_Prt == 51820 || Dest_Prt == 51820) { // If WireGuard port
            oss << " | WireGuard UDP packet";        // Log as WireGuard
        }
        else if (udp_payload_len >= sizeof(IKEv2Header)) { // If enough payload for IKEv2 header
            const IKEv2Header* ike_hdr = (const IKEv2Header*)(udp_header + 8); // Point to IKEv2 header
            uint8_t major_version = (ike_hdr->Version >> 4);                   // Extract IKEv2 major version
            if (major_version == 2) {                                          // If IKEv2
                oss << " | IKEv2 Msg: Exchange=" << ikev2_exchange_type_str(ike_hdr->Exchange_Type)
                    << " MsgID=" << ntohl(ike_hdr->Message_ID);                // Log exchange type and message ID
            }
        }

        // Check for NAT-T (NAT Traversal) encapsulated ESP packet
        if ((Src_Prt == 4500 || Dest_Prt == 4500) && !(Src_Prt == 51820 || Dest_Prt == 51820)) {
            if (udp_payload_len >= 4) {               // Ensure enough payload
                const u_char* Nat_T_Header = udp_header + 8; // Point to NAT-T header
                if (Nat_T_Header[0] == 0 && Nat_T_Header[1] == 0 && Nat_T_Header[2] == 0 && Nat_T_Header[3] == 0) {
                    oss << " | NAT-T Encapsulated ESP packet"; // Log as NAT-T ESP
                }
            }
        }

        log_message(oss.str());                       // Log the constructed message

    }
    else {
        // Unsupported IP version
        return;                                      // Exit if not IPv4 or IPv6
    }
}

// Function to start packet capture on a specific device/interface
void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];                   // Buffer for error messages
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf); // Open device for capture
    if (!handle) {                                   // If failed to open device
        std::lock_guard<std::mutex> lock(log_mutex); // Lock mutex for thread safety
        std::cerr << "Failed to open device " << (device->description ? device->description : device->name)
            << ": " << errbuf << std::endl;          // Print error message
        return;                                      // Exit function
    }

    struct bpf_program filter;                       // BPF filter structure
    const char* filter_exp = "udp port 500 or udp port 4500 or udp port 51820"; // Filter expression

    // Compile the filter expression
    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1) {
        std::lock_guard<std::mutex> lock(log_mutex); // Lock mutex for thread safety
        std::cerr << "Couldn't parse filter '" << filter_exp << "' on "
            << (device->description ? device->description : device->name)
            << ": " << pcap_geterr(handle) << std::endl; // Print error message
        pcap_close(handle);                         // Close pcap handle
        return;                                     // Exit function
    }

    // Set the compiled filter on the capture handle
    if (pcap_setfilter(handle, &filter) == -1) {
        std::lock_guard<std::mutex> lock(log_mutex); // Lock mutex for thread safety
        std::cerr << "Couldn't install filter on "
            << (device->description ? device->description : device->name)
            << ": " << pcap_geterr(handle) << std::endl; // Print error message
        pcap_freecode(&filter);                   // Free filter resources
        pcap_close(handle);                       // Close pcap handle
        return;                                   // Exit function
    }

    pcap_freecode(&filter);                       // Free filter resources

    { // Scope for mutex lock
        std::lock_guard<std::mutex> lock(log_mutex); // Lock mutex for thread safety
        std::cout << "Started capturing on interface: "
            << (device->description ? device->description : device->name) << std::endl; // Print to console
        if (log_file.is_open()) {                 // If log file is open
            log_file << "Started capturing on interface: "
                << (device->description ? device->description : device->name) << std::endl; // Log to file
            log_file.flush();                     // Flush output
        }
    }

    pcap_loop(handle, 0, packet_handler, (u_char*)handle); // Start packet capture loop

    pcap_close(handle);                           // Close the capture handle after loop ends
}

int main() {
    // Open log file for writing/appending
    log_file.open("packet_capture.log", std::ios::out | std::ios::app);
    if (!log_file.is_open()) {                    // If failed to open log file
        std::cerr << "Failed to open log file for writing." << std::endl; // Print error
        return 1;                                 // Exit with error
    }

    pcap_if_t* alldevs;                           // Pointer to list of all devices
    char errbuf[PCAP_ERRBUF_SIZE];                // Buffer for error messages

    // Find all available network devices
    if (pcap_findalldevs(&alldevs, errbuf) == -1 || alldevs == nullptr) {
        std::cerr << "Error finding devices: " << errbuf << std::endl; // Print error
        return 1;                                 // Exit with error
    }

    std::vector<std::thread> threads;             // Vector to hold capture threads

    // Start a capture thread for each device
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        threads.emplace_back(capture_on_device, d); // Create and start thread
    }

    std::cout << "Capturing on all available interfaces..." << std::endl; // Notify user
    std::cout << "Press Ctrl+C to stop." << std::endl;                   // Notify user

    // Wait for all capture threads to finish (runs until interrupted)
    for (auto& t : threads) {
        if (t.joinable())                // If thread can be joined
            t.join();                    // Wait for thread to finish
    }

    pcap_freealldevs(alldevs);           // Free the device list

    log_file.close();                    // Close the log file
    return 0;                            // Exit program successfully
}
