# 🧠 H.E.A.T. Parsing Modules

High-speed, cross-platform C++ modules for real-time network traffic analysis.  
These native modules power the detection engine behind the H.E.A.T. Android and Windows applications, capturing low-level packet data and translating it into actionable configuration elements for VyOS routers.

---

## 🌐 Purpose

The parsing modules serve as the core intelligence layer of the H.E.A.T. system. Their responsibilities include:

- Capturing Ethernet-level traffic (raw packets)
- Parsing protocols like DNS, DHCP, VLAN, BGP, and OSPF
- Extracting relevant data such as domain queries, port usage, VPN tunnels, rogue DHCPs, and more
- Feeding structured output to the VyOS configuration engine in the Windows and Android apps

---

## 📁 File Overview

| File                     | Description                                                     |
|--------------------------|-----------------------------------------------------------------|
| `BG-Realtime-Parsing.cpp`| Real-time capture and classification of background network data |
| ospf_esp.cpp           | Parses OSPF traffic and detects encapsulated ESP packets        |
| ospfesp.cpp            | Alternate/combined OSPF and ESP handling logic                  |

> These modules interface with platform-specific wrappers (NDK for Android, WinPcap for Windows).

---

## 🧱 Integration Targets

| Platform      | Integration       | Purpose                                 |
|---------------|-------------------|------------------------------------------|
| Android       | JNI + NDK         | Kotlin-based UI, traffic analysis engine |
| Windows       | P/Invoke or bridge| WinUI 3 app + config generator           |

---

## ⚙️ How It Works

1. Traffic is captured either from a live interface or through a PCAP file.
2. Each module identifies key protocol patterns (e.g., DHCP offers, VLAN tags, DNS queries).
3. Output is passed to the configuration layer as structured data.
4. This data is used to generate full VyOS-ready configuration files.

---

## 🧪 Testing Locally

You can build and run a parser on a sample .pcap file like so:

```bash
g++ -std=c++17 -o test_parser ospf_esp.cpp
./test_parser test_capture.pcap
> Replace sample.pcap with a valid capture file for test input.

To test end-to-end on Android:

- Connect an OTG-compatible USB Ethernet adapter
- Accept permissions when prompted
- Tap Scan Network
- Monitor live detection of protocols and services

---

## 📂 Folder Layout
