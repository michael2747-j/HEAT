# GEN_REALTIME_CSV.cs Comprehensive README

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Architecture](#architecture)
    - [Data Models](#data-models)
    - [Network Capture](#network-capture)
    - [CSV Writer](#csv-writer)
4. [Usage](#usage)
    - [Prerequisites](#prerequisites)
    - [Running Real-Time Analysis](#running-real-time-analysis)
    - [Stopping Analysis](#stopping-analysis)
    - [CSV Output Structure](#csv-output-structure)
5. [Extending and Customizing](#extending-and-customizing)
6. [Troubleshooting](#troubleshooting)
7. [Credits](#credits)

---

## Overview

**GEN_REALTIME_CSV.cs** is a real-time network analysis and CSV export tool for Windows, designed for advanced network interface monitoring, data aggregation, and reporting. It leverages packet capture libraries to analyze traffic, aggregate protocol and endpoint information, and periodically writes structured summaries to a CSV file.

- **Authors:** Anthony Samen / Cody Cartwright
- **Created:** June 5, 2025
- **Last Modified:** June 5, 2025

---

## Features

- **Live Network Capture:** Monitors selected network interfaces in real time.
- **Protocol & Service Detection:** Identifies common protocols (TCP, UDP, DHCP, DNS, VPN, NAT, etc.).
- **VLAN Awareness:** Recognizes and tracks VLAN-tagged traffic.
- **Data Aggregation:** Summarizes observed endpoints, services, and protocols.
- **CSV Export:** Writes comprehensive, multi-table CSV reports for further analysis.
- **User Interaction:** UI prompts for interface selection and status notifications.
- **Extensible:** Modular design for easy feature addition.

---

## Architecture

### Data Models

All observed network data is aggregated into static classes under `HEAT.DataModels`. Each configuration type (LAN, WAN, VLAN, NAT, DHCP, DNS, VPN, AD) has its own class with relevant properties, typically including sets of observed IPs, protocols, ports, and other metadata.

#### Example Model: `LanInterfaceConfig`

```csharp
public class LanInterfaceConfig
{
    public string VLANId = "";
    public string LanDevice = "";
    public HashSet CommonNames = new();
    public HashSet SourceIps = new();
    // ... (other sets for aggregation)
}
```

All models are stored in static dictionaries for fast lookup and aggregation keyed by unique identifiers.

### Network Capture

The `NetworkCapture` static class orchestrates:

- **Device Selection:** UI dialog for users to select interfaces (or accepts a pre-selected list).
- **Packet Capture:** Uses [SharpPcap](https://github.com/chmorgan/sharppcap) and [PacketDotNet](https://github.com/chmorgan/packetnet) for low-level packet parsing.
- **Packet Parsing:** Extracts VLAN IDs, IPs, MACs, protocol types, ports, DNS queries, DHCP details, and more.
- **Data Aggregation:** Updates relevant data models in a thread-safe manner.
- **NAT Detection:** Attempts to determine the public NAT IP via `nslookup` or external services.
- **Status Feedback:** Optionally reports status via a callback (for UI/AI integration).
- **Graceful Stop:** Cancels capture and flushes data on request.

### CSV Writer

The `CsvWriter` static class periodically (every 5 seconds) writes out all aggregated data to a CSV file (`REALTIME_LOG.csv` on the Desktop):

- **Multi-Table Output:** Each section (LAN, WAN, NAT, DHCP, DNS, VPN, AD, Routing Protocols) is output as a separate table with headers.
- **Data Merging:** The "Total Network" table merges information from all sources for a comprehensive view.
- **Safe Writing:** Handles file lock exceptions gracefully.

---

## Usage

### Prerequisites

- **Windows 10/11**
- **.NET 6+**
- **Admin Rights** (required for packet capture)
- **SharpPcap** and **PacketDotNet** libraries
- **Microsoft.UI.Xaml** (WinUI 3)
- **Internet Access** (for NAT IP detection)

### Running Real-Time Analysis

1. **Start Analysis:**
    - Call `NetworkCapture.gen_rt_csv(...)` from your UI or main logic.
    - The user is prompted to select network interfaces unless a list is provided.
    - Status messages can be routed to a UI or logging callback.

2. **During Analysis:**
    - All selected interfaces are monitored in real-time.
    - Aggregated data is written to `REALTIME_LOG.csv` on the Desktop every 5 seconds.

3. **Stop Analysis:**
    - Call `NetworkCapture.StopCaptureAsync()` to gracefully stop all capture and writing.

#### Example (pseudo-code):

```csharp
await NetworkCapture.gen_rt_csv(mainWindow, StatusCallback);
...
await NetworkCapture.StopCaptureAsync();
```

### CSV Output Structure

The CSV file contains multiple tables, each separated by a blank line:

- **Total Network:** Comprehensive merged view.
- **LAN Interface Configurations**
- **WAN Interface Configurations**
- **VLAN Interface Configurations**
- **NAT Configurations**
- **DHCP Servers in Use**
- **DNS Servers in Use**
- **VPN Tunnels in Use**
- **Active Directory Controllers / Non-Public DNS Domains**
- **Routing Protocols Observed**

Each table includes appropriate headers and columns (see code for details).

---

## Extending and Customizing

- **Add New Protocols:** Extend packet parsing in `NetworkCapture` to detect new protocols or services.
- **Custom Output:** Modify `CsvWriter` for different output formats or additional tables.
- **UI Integration:** Use the callback mechanism for real-time UI updates or AI feedback.
- **Performance Tuning:** Adjust the CSV write interval or optimize data aggregation as needed.

---

## Troubleshooting

- **No Interfaces Found:** Ensure SharpPcap is installed and the app is running with admin rights.
- **CSV Not Updating:** Check for file locks or permission issues on the Desktop.
- **NAT IP Shows "Unknown":** Ensure internet access for NAT detection.
- **Packet Parsing Errors:** Some malformed packets may not be parsed; these are logged to debug output.

---

## Credits

- **Authors:** Anthony Samen / Cody Cartwright
- **Libraries Used:** [SharpPcap](https://github.com/chmorgan/sharppcap), [PacketDotNet](https://github.com/chmorgan/packetnet), [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/)
- **Special Thanks:** Community contributors and testers.

---

## Appendix: Key Methods and Their Roles

| Method/Class                       | Purpose                                                                 |
|-------------------------------------|-------------------------------------------------------------------------|
| `DataModels.ClearAllData()`         | Resets all aggregated data models.                                      |
| `NetworkCapture.gen_rt_csv(...)`    | Starts real-time capture and aggregation on selected interfaces.         |
| `NetworkCapture.StopCaptureAsync()` | Stops all captures and flushes data.                                    |
| `CsvWriter.WriteAggregatedDataToCsv`| Periodically writes all aggregated data to CSV.                         |
| `GetNatTranslatedIpAsync()`         | Attempts to discover public NAT IP address.                             |
| `ParseDnsQuery(...)`                | Extracts FQDN from DNS packet payload.                                  |
| `IsWan(IPAddress)`                  | Determines if an IP is considered WAN/public.                           |

---