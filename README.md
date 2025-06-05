# 📱 H.E.A.T. Android App – Header-based Ether Adapter Tracking

Mobile VyOS config generation from real-time network traffic.  
A fast, intuitive Android application that scans local networks and builds deployable VyOS configurations.

---

## 🌐 Overview

The H.E.A.T. Android app turns your smartphone into a portable network configuration assistant. Designed for system administrators, field engineers, and educators, the app allows users to:

- Connect to a live network (via Wi-Fi or USB adapter)
- Capture traffic and detect services (DNS, DHCP, VPN, VLAN, etc.)
- Generate tailored, production-ready VyOS configurations in minutes
- Optionally share or push the config to a router over SSH

---

## 🎯 Goal

> _“Scan a network in the field, generate the VyOS config, and deploy it on the spot.”_

This app is part of the broader H.E.A.T. ecosystem, providing mobile-first access to the same intelligent parsing and config-generation capabilities found in the desktop version.

---

## 🧱 Architecture

| Layer               | Description                                          |
|---------------------|------------------------------------------------------|
| Kotlin UI           | Android front-end, built with Jetpack and Material  |
| Native C++ Modules  | Parses network traffic via NDK (shared with desktop)|
| USB/Permissions     | Handles adapter access (OTG Ethernet or USB-C)      |
| Config Builder      | Converts parsed data into VyOS config output        |
| SSH Integration     | Optional: send config to VyOS router via libssh     |

---

## 📦 Key Features

- 🌐 Auto-scans local networks
- 🔍 Detects DNS servers, rogue DHCPs, VLANs, NAT rules, VPN tunnels
- 🧠 Uses shared C++ logic for parsing traffic (via Android NDK)
- ⚙️ Generates full VyOS config files (including interface, DHCP, BGP)
- 📤 Exports config via:
  - Save to file
  - Share via apps
  - Push via SSH (optional)
- 🧪 Real-time progress indicators and detection logs
- 🛡️ Alerts for suspicious traffic or unknown services

---

## 🧪 Testing Scenarios

| Scenario                | Expected Output                                 |
|-------------------------|--------------------------------------------------|
| Small home Wi-Fi        | Basic config with DHCP, DNS, single interface    |
| VLAN-segmented network  | Config includes interfaces { vlan X { ... }}   |
| Office LAN with VPN     | Detect VPN services and generate NAT rules       |
| Malformed DNS queries   | Flag suspicious domain patterns in app alerts    |

---

## 🚀 Getting Started

### Prerequisites
- Android Studio Hedgehog or newer
- NDK installed (via SDK Manager)
- USB OTG-capable Android device
- Optionally: USB Ethernet adapter for live testing

