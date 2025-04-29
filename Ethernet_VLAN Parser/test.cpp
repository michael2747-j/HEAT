#include <iostream>
#include <cstdint>
#include <cstdio>
#include "ethernet_parser.h"
#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#else
#include <arpa/inet.h>
#endif

namespace heat {

#pragma pack(push, 1)

    struct EthernetFrame {
        uint8_t destMac[6];
        uint8_t srcMac[6];
        uint16_t etherType;
    };

    struct VLANTag {
        uint16_t tpid;
        uint16_t pcp_vlan;
    };

#pragma pack(pop)

    inline bool isVLANFrame(const EthernetFrame* frame) {
        return ntohs(frame->etherType) == 0x8100;
    }

    inline uint16_t extractVLANId(const VLANTag* vlanTag) {
        return ntohs(vlanTag->pcp_vlan) & 0x0FFF;
    }

} // namespace heat

int main() {
    using namespace heat;

    // Fake VLAN-tagged Ethernet frame:
    // EtherType = 0x8100 (VLAN), VLAN ID = 42
    uint8_t testPacket[] = {
        0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, // Dest MAC
        0x11, 0x22, 0x33, 0x44, 0x55, 0x66, // Src MAC
        0x81, 0x00,                         // EtherType: VLAN (0x8100)
        0x00, 0x2A                          // VLAN TCI: VLAN ID 42
    };

    const EthernetFrame* frame = reinterpret_cast<const EthernetFrame*>(testPacket);

    char srcMac[18], destMac[18];
    snprintf(srcMac, sizeof(srcMac), "%02x:%02x:%02x:%02x:%02x:%02x",
        frame->srcMac[0], frame->srcMac[1], frame->srcMac[2],
        frame->srcMac[3], frame->srcMac[4], frame->srcMac[5]);
    snprintf(destMac, sizeof(destMac), "%02x:%02x:%02x:%02x:%02x:%02x",
        frame->destMac[0], frame->destMac[1], frame->destMac[2],
        frame->destMac[3], frame->destMac[4], frame->destMac[5]);

    std::cout << "Source MAC: " << srcMac << "\n";
    std::cout << "Dest MAC:   " << destMac << "\n";
    std::cout << "EtherType:  0x" << std::hex << ntohs(frame->etherType) << std::dec << "\n";

    if (isVLANFrame(frame)) {
        const VLANTag* vlan = reinterpret_cast<const VLANTag*>(
            testPacket + sizeof(EthernetFrame));
        uint16_t vlanId = extractVLANId(vlan);
        std::cout << "VLAN ID:    " << vlanId << "\n";
    }

    return 0;
}
