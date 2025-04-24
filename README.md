# tapadam
Hands-on exploration of DNS servers, packet sniffing (promiscuous mode), and VyOS-based routing. Great for students, sysadmins, and network enthusiasts.
An intelligent tool to automatically generate VyOS router configurations from real-world network traffic taps.

🚀 Built as part of an internship project for ADAMnetworks
🎯 Targeting VyOS 1.5 with support for dynamic multi-WAN/LAN, NATs, VPNs, and more
🧠 AI-inspired UX for real-time config generation
📱 Runs locally—even on a smartphone with USB Ethernet

🧭 Executive Summary
ADAMnetworks requires a simple, intelligent, and portable tool to help enterprises build VyOS configurations using observed network traffic via a network tap. This includes:

WAN/DMZ + Internal LAN Configuration

Support for NAT, Port Forwards, Routing, VPNs, and DHCP

Deep Traffic Inspection to detect:

DNS Servers

Active Directory controllers

Routing protocols (e.g., OSPF, BGP)

VPN tunnels (via port/protocol detection)

BONUS: Optional support for adam:ONE deployment

⚙️ Features
🔍 Passive detection of pre-NAT and post-NAT traffic flows

🔌 Support for 2+ WAN and 2+ LAN interfaces (Layer 2 & 3)

🧠 Real-time, LLM-inspired code output UI

📱 Mobile-friendly POC on Android using USB Ethernet adapter

🧠 Traffic inspection includes:

DNS requests + responses (including SRV for AD discovery)

Routing protocol detection (e.g., BGP, OSPF)

VPN ports

DHCP fingerprinting

🧩 Extensible for future traffic analysis modules

🧪 Development & Testing Platform
SG-3100 + Unifi 8-port switch (pre-configured)

VyOS 1.5 on Lanner box

Android with USB Ethernet adapter (support for promiscuous mode required)

Cross-platform: Linux, MacOS, Windows (via Python)

🧠 Architecture Highlights
Promiscuous Packet Capture (e.g., scapy, libpcap, or pyshark)

Stateful Analysis Engine to detect network services and traffic patterns

VyOS Configuration Engine that emits CLI-config-formatted blocks

Optional AI-style UI that reveals configs dynamically as traffic is observed

📅 Timeline (2025)
April 22: Project Kickoff

April 24–26: Discovery & Feasibility (Review #1)

April 28: Hardware distribution (meetup near YYZ)

June 6: Review #2 (midpoint)

July 18: Review #3 (alpha release)

August TBD: Final presentation

📞 Contacts
Nick Neufeld: support@adamnet.works | Signal: 519-854-8849

Steve Sansford: Signal: 506-655-9860

General Inquiries: info@adamnet.works
