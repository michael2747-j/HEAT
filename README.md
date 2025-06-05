# 🧠 H.E.A.T. Core Parsing Modules

Native C++ components for real-time network traffic analysis across Windows and Android platforms.  
This branch contains the protocol parsers and traffic analysis logic that powers both the desktop and mobile versions of the H.E.A.T. system.

---

## 🌐 Purpose

These modules perform low-level packet analysis to support:

- Ethernet and VLAN frame decoding
- DNS, DHCP, VPN, OSPF, and BGP traffic parsing
- Real-time extraction of network metadata
- Automated generation of VyOS-compatible firewall and routing configs
- Triggering alerts for suspicious or misconfigured services

These C++ modules are integrated into the Windows app via CLI and into the Android app via the Android NDK + JNI bridge.

---

## 📁 Key Modules and Files

| File/Folder                        | Description                                                                 |
|------------------------------------|-----------------------------------------------------------------------------|
| DNS (UDP/UDP)/                   | Parses DNS queries (UDP port 53), detects public resolvers and domain behavior |
| Ethernet_VLAN Parser/           | Captures and parses raw Ethernet frames, identifies VLAN-tagged traffic     |
| VPN parsing/                     | Flags VPN protocols (OpenVPN, GRE, IPsec) based on port and payload analysis |
| WindowsNetworkHistoryLogger.cpp | Extracts DNS/DHCP records from Windows system logs (for offline analysis)    |
| ospf_esp/                        | Lightweight OSPF + ESP detection test module (useful for tunneling detection) |
| ospfesp.cpp                      | Consolidated parser for OSPF routing and IPsec (ESP) encapsulation behavior  |

---

## 🔌 Integration Targets

| Platform      | Binding Method            | Purpose                                 |
|---------------|----------------------------|------------------------------------------|
| Android       | JNI + NDK                  | Mobile traffic capture and config build  |
| Windows       | CLI or P/Invoke bridge     | GUI-integrated analysis in WinUI app     |

---

## ⚙️ How It Works

1. Traffic is captured from a live interface or PCAP stream.
2. Each protocol-specific module scans for matching packet headers.
3. Parsed data is structured into a standard format.
4. This data feeds the VyOS configuration engine to generate:
   - Interface definitions
   - DHCP/DNS rules
   - VPN/NAT policies
   - BGP/OSPF routing blocks
