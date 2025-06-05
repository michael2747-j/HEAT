# 📱 H.E.A.T. Android Parsing Modules

Modular C++ and Kotlin components for real-time network traffic analysis on mobile and embedded platforms.  
This branch supports the Android version of H.E.A.T., combining JNI-bridged C++ parsing with native Kotlin UI to detect and transform local network activity into VyOS-ready configuration files.

---

## 🌐 Purpose

This parsing layer provides protocol-level visibility to the Android app and enables it to:

- Capture raw Ethernet frames over USB adapters (or rooted interfaces)
- Parse and classify traffic across DNS, DHCP, VLANs, VPNs, and routing protocols
- Automatically extract and structure this data into VyOS firewall and routing rules
- Trigger security alerts based on suspicious traffic (rogue DHCP, unknown domains, etc.)

---

## 📁 Key Modules and Files

| File/Folder                        | Description                                                                 |
|------------------------------------|-----------------------------------------------------------------------------|
| DNS (UDP/UDP)/                   | Contains DNS and UDP traffic parsers (port 53), flags public DNS servers   |
| Ethernet_VLAN Parser/           | Extracts Ethernet frame metadata, VLAN tags, and MAC-based segmentation     |
| VPN parsing/                     | Detects VPN tunnels using IPsec, OpenVPN, GRE based on port/protocol flags |
| WindowsNetworkHistoryLogger.cpp | Parses Windows registry and local logs to retrieve past DNS/DHCP activity   |
| ospf_esp/                        | Lightweight test module for parsing OSPF headers and encapsulated ESP data |
| ospfesp.cpp                      | Consolidated parser handling OSPF LSA updates and IPsec (ESP) detection     |

---

## 🔌 Integration Target

| Component        | Role                                  |
|------------------|----------------------------------------|
| Kotlin (Android) | App UI, configuration export           |
| Android NDK      | Loads and executes C++ modules         |
| JNI Bindings     | Bridges NDK modules to Kotlin logic    |

---

## ⚙️ How It Works

1. The user taps “Scan Network” in the Android app.
2. The app initializes packet capture (via USB or root interface).
3. Captured packets are passed to C++ modules using the NDK.
4. Protocol-specific parsers analyze and extract:
   - DNS server IPs
   - DHCP lease announcements
   - VLAN headers
   - VPN port usage
   - BGP/OSPF routing markers
5. Kotlin layer generates a VyOS configuration file using this structured data.
