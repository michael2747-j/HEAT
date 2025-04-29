

#pragma once

#include <cstdint>

#ifdef _WIN32
#include <winsock2.h>  // Windows networking
#include <ws2tcpip.h>  // For ntohs() on Windows
#else
#include <arpa/inet.h> // Unix/Linux/macOS
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
