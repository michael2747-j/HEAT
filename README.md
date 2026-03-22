# 🔥 H.E.A.T. – Header-based Ether  Adapter Tracking

<table>
  <tr>
    <td><img src="docs/heat.png" alt="H.E.A.T. Logo" width="180"  style="border-radius:12px; margin-right:16px;"/></td>
    <td>
      <h3>H.E.A.T. – Header-based Ether Adapter Tracking</h3>
      <p><strong>Automated VyOS configuration generation directly from live network traffic.</strong><br/>
      Built to transform everyday devices into intelligent, plug-and-play network configuration wizards capable of analyzing, interpreting, and deploying secure router setups in minutes.</p>
    </td>
  </tr>
</table>

---

## 🌐 What Is H.E.A.T.?

H.E.A.T. is a cross-platform system that:
- Scans real network traffic
- Parses DNS, DHCP, VLAN, NAT, BGP, and VPN data
- Generates secure VyOS configuration files in under 5 minutes

This project serves as the master coordination hub for all platform-specific branches.

---

## 🎯 Project Goal

> _“Turn smartphones and PCs into plug-and-play network config wizards.”_

Using real-time traffic analysis, H.E.A.T. automates secure VyOS firewall and routing configurations for fast deployment in educational, enterprise, or home environments.

---

## 🧱 Core Architecture

| Component           | Description                                           |
|--------------------|-------------------------------------------------------|
| 🖥 Windows App      | Built with WinUI 3 + .NET for desktop deployment      |
| 📱 Android App      | Mobile app (Kotlin + NDK) for on-the-go config        |
| ⚙️ C# Modules       | Shared native parsers for DNS, BGP, DHCP, VLAN, VPN   |
| 🛠 Config Generator  | Logic layer for turning parsed data into VyOS configs |

---

## 💡 Key Requirements (from Lead Engineer)

- ✅ Generate a basic VyOS config in under 2 minutes of traffic capture
- ✅ Include DNS, DHCP, VLANs, NAT, and device detection
- ✅ Detect non-public domain suffixes, AD controllers, and rogue DHCPs
- ✅ Include security monitoring with real-time alerts
- ✅ Output must be testable on VyOS via VirtualBox/Proxmox
- ✅ Platform targets: Windows, Android (iOS/macOS optional future)

---

## 🔀 Branch Overview

| Branch Name     | Purpose                                            |
|-----------------|----------------------------------------------------|
| main          | 📘 Docs, coordination, planning                     |
| windows-app   | 🖥 Desktop UI + config generation pipeline           |
| android-app   | 📱 Mobile app for network scanning + config build   |
| c#-modules   | ⚙️ Native C# packet analyzers  |

Each branch includes a README.md with platform-specific setup instructions.

---

## 📦 Key Features

- One-click "Scan Network" button
- Detects and parses:
  - DNS queries and suffixes
  - DHCP leases and rogue servers
  - VLANs, NAT, VPN tunnels
  - BGP neighbors, active services
- Real-time progress and logging
- Auto-generation of VyOS config:
  - interfaces { ... }
  - firewall { ... }
  - service { dhcp-server ... }
- Optional SSH export to target router

---

## 🧪 Testing Workflow

| Scenario              | Expected Output                                  |
|-----------------------|--------------------------------------------------|
| Simple home network   | Basic DNS + DHCP config                          |
| Corporate network     | VLAN tagging, NAT, BGP peers, AD domain parsing  |
| Unknown device scan   | Flag suspicious endpoints or rogue DHCP offers   |

Testing is done on virtual networks (VyOS + Lubuntu) in VirtualBox or Proxmox.

---

## 🚀 Getting Started

```bash
git clone https://github.com/your-org/HEAT.git
cd HEAT
git checkout windows-app   # or android-app / cpp-modules
