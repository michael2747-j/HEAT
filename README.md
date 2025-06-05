H.E.A.T. 🔥

Header-based Ether Adapter Tracking
Automated VyOS configuration generation from live network traffic.

⸻

🌐 What Is H.E.A.T.?

H.E.A.T. is a cross-platform system designed to transform any device — desktop or mobile — into a network configuration wizard.

Its core function:
➡️ Scan real network traffic
➡️ Parse DNS, VLAN, BGP, DHCP, VPN, and other services
➡️ Auto-generate secure, production-ready VyOS configurations
➡️ Push the config to a router with one click

This project bridges the complexity gap between real-time traffic capture and practical firewall/router deployment.

⸻

🎯 Project Goal

“Turn smartphones and PCs into plug-and-play network config wizards.”

Using packet capture and intelligent parsing, HEAT analyzes network environments and produces tailored VyOS configurations in under 2 minutes — no manual CLI or scripting required.

⸻

🧱 Core Architecture
 • 🖥 Windows App – Built with WinUI 3 and .NET, ideal for system admins
 • 📱 Android App – Mobile version for quick deployment and scanning
 • ⚙️ C++ Modules – High-speed packet parsers (DNS, VPN, BGP, VLAN)
 • 🛠 VyOS Config Generator – Converts captured data into VyOS-compatible output
 • 📤 Deployment Layer – Push configs to routers via SSH (libssh)

⸻

💡 Key Concepts from David (Lead Engineer)

1. Purpose

Automate VyOS config generation based on live traffic, replicating and improving on traditional tools like pfSense.

2. Speed & Simplicity

Users should generate basic configs (with DNS/DHCP/VLAN/NAT) in under 2 minutes of scanning.

3. Real-Time Packet Capture

Capture and analyze DNS queries, DHCP offers, VLAN tags, NAT usage, AD controllers, etc.

4. Output = Deployable Config

HEAT should output configs ready for direct import into VyOS (tested via VirtualBox or Proxmox).

5. Security Monitoring

Flag suspicious domains or traffic in real time. Show warnings inside the UI.

6. Testing Workflow

Simulate networks in VMs → run HEAT → import config into VyOS → validate connectivity.

7. Cross-Platform Extensibility

Initial focus: Windows + Android
Future: iOS and Mac (via C++ interop + Swift bridge)

⸻

🧪 Sample Use Case

Demo:
“With HEAT, configuring a VyOS router is as simple as plugging in your phone. Let’s scan this network… [tap]. Done. The app detected a VLAN for IoT devices, a DNS server at 8.8.8.8, and a VPN tunnel. Here’s the VyOS config it generated.”
📂 In This Branch
 • /docs/ – Architecture, flowcharts, planning
 • /images/ – UI mockups, network diagrams
 • README.md – This file
 • (No code lives here; see other branches)

⸻

🛠 Tools & Tech
 • C++ – Packet parsing (high speed)
 • libssh – Push config files to routers
 • Jinja2 (Python) – Optional templating engine for advanced config generation
 • Android NDK / Swift C++ interop – For mobile parser integration

⸻

📦 Features (by Platform)

🖥 Windows App
 • GUI for scanning, parsing, and generating VyOS configs
 • Preview/edit before export
 • Integrated test environment

📱 Android App
 • “Scan Network” button triggers capture
 • Real-time progress of detected services
 • “Export to VyOS” button for sharing or deployment

⚙️ C++ Modules
 • Ethernet parser
 • DNS + DHCP analyzer
 • VLAN tagging
 • VPN & BGP signature detection
