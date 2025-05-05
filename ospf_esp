#include <iostream>
#include <vector>
#include <thread>
#include <mutex>
#include <fstream>
#include <pcap/pcap.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "ws2_32.lib")

std::ofstream log_file;
std::mutex log_mutex;

void log_message(const std::string& msg) {
    std::lock_guard<std::mutex> lock(log_mutex);
    std::cout << msg << std::endl;
    if (log_file.is_open()) {
        log_file << msg << std::endl;
        log_file.flush();
    }
}

void packet_handler(u_char* user, const struct pcap_pkthdr* header, const u_char* packet) {
    pcap_t* handle = (pcap_t*)user;
    int linktype = pcap_datalink(handle);
    int header_len = (linktype == DLT_EN10MB) ? 14 : 0;
    if (header->caplen < header_len + 20) return;

    int ip_version = (packet[header_len] >> 4);
    if (ip_version == 4) {
        int ip_header_len = (packet[header_len] & 0x0F) * 4;
        if (header->caplen < header_len + ip_header_len) return;

        u_char protocol = packet[header_len + 9];
        char Src_Ip[INET_ADDRSTRLEN];
        char Dest_Ip[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, packet + header_len + 12, Src_Ip, INET_ADDRSTRLEN);
        inet_ntop(AF_INET, packet + header_len + 16, Dest_Ip, INET_ADDRSTRLEN);

        std::ostringstream oss;
        if (protocol == 89) {
            oss << "[OSPF] " << Src_Ip << " -> " << Dest_Ip;
        } else if (protocol == 50) {
            oss << "[ESP] " << Src_Ip << " -> " << Dest_Ip;
        } else {
            return;
        }

        log_message(oss.str());
    }
}

void capture_on_device(pcap_if_t* device) {
    char errbuf[PCAP_ERRBUF_SIZE];
    pcap_t* handle = pcap_open_live(device->name, 65536, 1, 1000, errbuf);
    if (!handle) return;

    // Filter for protocol 89 (OSPF) or 50 (ESP)
    struct bpf_program filter;
    const char* filter_exp = "ip proto 89 or ip proto 50";
    if (pcap_compile(handle, &filter, filter_exp, 1, PCAP_NETMASK_UNKNOWN) == -1 ||
        pcap_setfilter(handle, &filter) == -1) {
        pcap_close(handle);
        return;
    }
    pcap_freecode(&filter);

    log_message(std::string("Capturing on interface: ") + (device->description ? device->description : device->name));
    pcap_loop(handle, 0, packet_handler, (u_char*)handle);
    pcap_close(handle);
}

int main() {
    log_file.open("ospf_esp_capture.log", std::ios::out | std::ios::app);
    if (!log_file.is_open()) {
        std::cerr << "Failed to open log file.\n";
        return 1;
    }

    pcap_if_t* alldevs;
    char errbuf[PCAP_ERRBUF_SIZE];
    if (pcap_findalldevs(&alldevs, errbuf) == -1) {
        std::cerr << "Error finding devices: " << errbuf << std::endl;
        return 1;
    }

    std::vector<std::thread> threads;
    for (pcap_if_t* d = alldevs; d != nullptr; d = d->next) {
        threads.emplace_back(capture_on_device, d);
    }

    std::cout << "Monitoring for OSPF and ESP packets on all interfaces..." << std::endl;
    std::cout << "Press Ctrl+C to stop." << std::endl;

    for (auto& t : threads) {
        if (t.joinable()) t.join();
    }

    pcap_freealldevs(alldevs);
    log_file.close();
    return 0;
}
