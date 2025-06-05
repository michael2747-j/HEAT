## GEN_CONFIG.cs - VyOS Configuration Generator Documentation

### 📁 File Overview
**Purpose**: Generates VyOS router configurations from network data in STATE_SAVE.csv  
**Author**: Anthony Samen & Ekaterina Vislova  
**Version**: 1.1.0 (June 4 2025)  
**Dependencies**: .NET 8.0, WinUI 3, Microsoft.Windows.ImplementationLibrary

---

## 🚀 Getting Started

### Prerequisites
1. STATE_SAVE.csv file containing network configuration
2. Windows 11+ environment
3. .NET 8.0 Runtime
4. WinUI 3 XAML Controls

### Basic Usage
```csharp
// Generate config with default credentials
VyOSConfigGenerator.GenerateVyOSConfig(
    "C:/heat/STATE_SAVE.csv", 
    "C:/vyos/config.conf");

// Generate with custom credentials
VyOSConfigGenerator.GenerateVyOSConfig(
    stateSavePath: "network.csv",
    outputPath: "custom.conf",
    username: "admin",
    password: "SecurePass!2025",
    sshPublicKey: "ssh-rsa AAAAB3NzaC...");
```

---

## 📂 File Structure

### Core Components
1. **VyOSConfigGenerator Class**
   - `GenerateVyOSConfig()`: Main generation method
   - `ParseNetworkData()`: CSV parser
   - `GenerateConfigFromNetworkData()`: Config builder
   - `ExportVyOSConfigAsync()`: UI export handler

2. **Model Classes**
   - `NetworkData`: Master container
   - `InterfaceData`: Port configurations
   - `DhcpServer`: DHCP settings
   - `FirewallRule/NatRule`: Security policies

3. **Chatter Class**
   - Real-time logging system
   - Message types: Info/Error/Warning

---

## 🔧 Configuration Details

### Generated Config Sections
1. **Container Configuration**
   - AdamOne container settings
   - Port forwarding (DNS, HTTP/S)
   - Environment variables

2. **Firewall Rules**
   - Stateful filtering
   - Interface groups (WAN/LAN)
   - Default drop policies

3. **Network Interfaces**
   - VLAN configurations
   - Hardware IDs
   - IP addressing (DHCP/Static)

4. **Services**
   - DHCP server pools
   - DNS forwarding
   - NTP time synchronization
   - SSH management (Port 20022)

---

## ⚙️ Customization Options

### Authentication Parameters
```csharp
VyOSConfigGenerator.GenerateVyOSConfig(
    sshPublicKey: "custom-key-here", // RSA key
    username: "custom-admin",        // Privileged user
    password: "CH4ng3M3!"            // Minimum 8 chars
);
```

### CSV Processing Rules
1. Only processes "Total Network" section
2. Required columns:
   - Column 1: VLAN ID
   - Column 2: Interface/Subnet
   - Column 6: DHCP Server IP
   - Column 17: IP Range

### Output Customization
```csharp
// Modify these arrays to change config sections
var config = new List {
    "container { ... }",
    "firewall { ... }"
    // Add custom sections here
};
```

---

## 🛠 Error Handling

### Common Exceptions
1. **CSV Format Errors**
   - Missing required columns
   - Invalid IP ranges

2. **File System Errors**
   - Permission denied
   - Path too long

3. **Network Validation**
   - Duplicate DHCP subnets
   - Invalid VLAN IDs

### Logging Examples
```plaintext
2025-06-05 14:22:10: Info: VyOS config saved to C:\vyos\output.conf
2025-06-05 14:23:45: Error: Invalid DHCP range in row 42
```

---

## 🔄 Version History
| Date       | Version | Changes                  |
|------------|---------|--------------------------|
| 2025-06-04 | 1.1.0   | Added SSH key auth       |
| 2025-06-03 | 1.0.1   | Fixed VLAN parsing       |
| 2025-06-02 | 1.0.0   | Initial release          |

---

## 📝 Notes
1. DHCP ranges must use CIDR notation
2. Requires VyOS 1.4+ compatibility mode
3. CSV must use UTF-8 encoding
4. Default admin credentials should be changed post-install