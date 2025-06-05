/*
    File: GEN_CONFIG.cs
    Purpose: Generates and exports VyOS configuration files from parsed network data in STATE_SAVE.csv
    Created: June 2 2025
    Last Modified: June 4 2025
    Authors: Anthony Samen / Ekaterina Vislova
*/

using System; // Imports base types
using System.IO; // Imports file operations
using System.Linq; // Imports LINQ for collections
using System.Collections.Generic; // Imports generic collections
using System.Text; // Imports string building
using System.Threading.Tasks; // Imports async/await
using Microsoft.UI.Xaml; // Imports Window class for WinUI
using Windows.Storage.Pickers; // Imports file picker for saving files
using WinRT.Interop; // Imports window handle interop

namespace HEAT // Declares project namespace
{
    /// <summary>
    /// Handles generation and export of VyOS configuration files based on network data.
    /// </summary>
    public class VyOSConfigGenerator // Declares generator class
    {
        /// <summary>
        /// Generates VyOS configuration file from STATE_SAVE.csv.
        /// </summary>
        public static void GenerateVyOSConfig(string stateSavePath, string outputPath, string username = "myvyosuser", string password = "defaultPassword123", string sshPublicKey = null)
        {
            try
            {
                var csvData = File.ReadAllLines(stateSavePath); // Reads all lines from CSV file
                var networkData = ParseNetworkData(csvData); // Parses CSV into structured data
                string vyosConfig = GenerateConfigFromNetworkData(networkData, username, password, sshPublicKey); // Generates config text
                File.WriteAllText(outputPath, vyosConfig); // Writes config to output file
                Chatter.Log($"VyOS configuration successfully saved to {outputPath}", Chatter.MessageType.Info); // Logs success
            }
            catch (Exception ex)
            {
                Chatter.Log($"Error generating VyOS config: {ex.Message}", Chatter.MessageType.Error); // Logs error
            }
        }

        /// <summary>
        /// Parses network data from CSV lines into NetworkData object.
        /// </summary>
        private static NetworkData ParseNetworkData(string[] csvLines)
        {
            var data = new NetworkData(); // Instantiates container for parsed data
            string currentSection = null; // Tracks current section
            string[] headers = null; // Tracks current headers

            for (int i = 0; i < csvLines.Length; i++) // Iterates CSV lines
            {
                var line = csvLines[i].Trim(); // Trims whitespace
                if (string.IsNullOrEmpty(line)) continue; // Skips empty lines

                if (!line.Contains(",")) // Detects section title
                {
                    currentSection = line.Trim(':'); // Sets section name
                    headers = null; // Resets headers
                    continue;
                }

                if (headers == null) // Detects header row
                {
                    headers = line.Split(',').Select(h => h.Trim()).ToArray(); // Parses headers
                    continue;
                }

                var columns = line.Split(',').Select(c => c.Trim()).ToArray(); // Parses columns
                if (columns.Length < headers.Length)
                    continue;

                if (currentSection == "Total Network") // Only processes "Total Network" section
                {
                    var vlanId = columns[0]; // Gets VLAN ID
                    var ifaceName = columns[1]; // Gets interface/subnet
                    var lanDevice = columns[2]; // Gets LAN device
                    var commonName = columns[3]; // Gets common name
                    var dhcpServerIp = columns[5]; // Gets DHCP server IP
                    var dnsServerIp = columns[10]; // Gets DNS server IP
                    var offeredIpRange = columns[17]; // Gets offered IP range

                    if (!string.IsNullOrEmpty(ifaceName) && !string.IsNullOrEmpty(dhcpServerIp)) // Checks interface and DHCP IP
                    {
                        var iface = new InterfaceData
                        {
                            Name = ifaceName, // Sets interface name
                            IPAddress = dhcpServerIp.Contains("DHCP") ? "dhcp" : $"{dhcpServerIp}/24", // Sets IP address
                            Description = !string.IsNullOrEmpty(commonName) ? commonName : ifaceName, // Sets description
                            HwId = columns[6], // Sets hardware ID
                            VlanId = vlanId // Sets VLAN ID
                        };
                        if (!data.Interfaces.Any(i => i.Name == iface.Name && i.IPAddress == iface.IPAddress)) // Prevents duplicates
                            data.Interfaces.Add(iface); // Adds interface to list
                    }

                    if (!string.IsNullOrEmpty(offeredIpRange) && offeredIpRange.Contains('-')) // Checks DHCP range
                    {
                        var rangeParts = offeredIpRange.Split('-'); // Splits range
                        var dhcp = new DhcpServer
                        {
                            NetworkName = ifaceName.Replace(".", "_").ToUpper(), // Sets network name
                            Subnet = $"{dhcpServerIp}/24", // Sets subnet
                            RouterIp = dhcpServerIp, // Sets router IP
                            DnsServer = string.IsNullOrEmpty(dnsServerIp) ? dhcpServerIp : dnsServerIp, // Sets DNS server
                            RangeStart = rangeParts[0], // Sets range start
                            RangeEnd = rangeParts[1] // Sets range end
                        };
                        if (!data.DhcpServers.Any(d => d.Subnet == dhcp.Subnet)) // Prevents duplicates
                            data.DhcpServers.Add(dhcp); // Adds DHCP server to list
                    }
                }
            }

            return data; // Returns parsed data
        }

        /// <summary>
        /// Generates VyOS configuration text from network data.
        /// </summary>
        private static string GenerateConfigFromNetworkData(NetworkData data, string username, string password, string sshPublicKey)
        {
            var config = new List<string>(); // Stores config lines

            config.Add("container {"); // Starts container section
            config.Add("    name adamone {");
            config.Add("        allow-host-networks");
            config.Add("        capability net-admin");
            config.Add("        capability net-bind-service");
            config.Add("        capability net-raw");
            config.Add("        capability sys-admin");
            config.Add("        environment LAN_INTERFACES {");
            config.Add("            value eth1");
            config.Add("        }");
            config.Add("        environment LAN_IP4_SUBNETS {");
            config.Add("            value 10.0.10.0/24");
            config.Add("        }");
            config.Add("        environment LISTEN_ADDRESSES {");
            config.Add("            value 10.0.10.1,127.0.0.1");
            config.Add("        }");
            config.Add("        environment LOG_LEVEL {");
            config.Add("            value 4");
            config.Add("        }");
            config.Add("        image cr.adamnet.io/adamone:latest");
            config.Add("        memory 0");
            config.Add("        port dns-tcp {");
            config.Add("            destination 53");
            config.Add("            protocol tcp");
            config.Add("            source 53");
            config.Add("        }");
            config.Add("        port dns-udp {");
            config.Add("            destination 53");
            config.Add("            protocol udp");
            config.Add("            source 53");
            config.Add("        }");
            config.Add("        port http {");
            config.Add("            destination 80");
            config.Add("            protocol tcp");
            config.Add("            source 80");
            config.Add("        }");
            config.Add("        port https {");
            config.Add("            destination 443");
            config.Add("            protocol tcp");
            config.Add("            source 443");
            config.Add("        }");
            config.Add("        volume config {");
            config.Add("            destination /opt");
            config.Add("            source /config");
            config.Add("        }");
            config.Add("    }");
            config.Add("}");

            config.Add("firewall {"); // Starts firewall section
            config.Add("    global-options {");
            config.Add("        state-policy {");
            config.Add("            established {");
            config.Add("                action accept");
            config.Add("            }");
            config.Add("            related {");
            config.Add("                action accept");
            config.Add("            }");
            config.Add("            invalid {");
            config.Add("                action drop");
            config.Add("            }");
            config.Add("        }");
            config.Add("    }");
            config.Add("    group {");
            config.Add("        interface-group WAN {");
            config.Add("            interface eth0");
            config.Add("        }");
            config.Add("        interface-group LAN {");
            config.Add("            interface eth1");
            config.Add("        }");
            config.Add("        interface-group LAN-INTERFACES {");
            config.Add("            interface eth1");
            config.Add("        }");
            config.Add("        network-group LAN-NETWORKS {");
            foreach (var dhcp in data.DhcpServers) // Adds each DHCP subnet
            {
                config.Add($"            network {dhcp.Subnet}");
            }
            config.Add("        }");
            config.Add("    }");
            config.Add("    ipv4 {");
            config.Add("        name OUTSIDE-IN {");
            config.Add("            default-action drop");
            config.Add("        }");
            config.Add("        name VyOS_MANAGEMENT {");
            config.Add("            default-action return");
            config.Add("            rule 15 {");
            config.Add("                action accept");
            config.Add("                inbound-interface {");
            config.Add("                    group LAN");
            config.Add("                }");
            config.Add("            }");
            config.Add("            rule 20 {");
            config.Add("                action drop");
            config.Add("                recent {");
            config.Add("                    count 4");
            config.Add("                    time minute");
            config.Add("                }");
            config.Add("                state new");
            config.Add("                inbound-interface {");
            config.Add("                    group WAN");
            config.Add("                }");
            config.Add("            }");
            config.Add("            rule 21 {");
            config.Add("                action accept");
            config.Add("                state new");
            config.Add("                inbound-interface {");
            config.Add("                    group WAN");
            config.Add("                }");
            config.Add("            }");
            config.Add("        }");
            config.Add("        forward filter {");
            config.Add("            rule 100 {");
            config.Add("                action jump");
            config.Add("                jump-target OUTSIDE-IN");
            config.Add("                inbound-interface {");
            config.Add("                    group WAN");
            config.Add("                }");
            config.Add("                destination {");
            config.Add("                    group {");
            config.Add("                        network-group LAN-NETWORKS");
            config.Add("                    }");
            config.Add("                }");
            config.Add("            }");
            config.Add("        }");
            config.Add("        input filter {");
            config.Add("            default-action drop");
            config.Add("            rule 20 {");
            config.Add("                action jump");
            config.Add("                jump-target VyOS_MANAGEMENT");
            config.Add("                destination {");
            config.Add("                    port 20022");
            config.Add("                }");
            config.Add("                protocol tcp");
            config.Add("            }");
            config.Add("            rule 30 {");
            config.Add("                action accept");
            config.Add("                icmp {");
            config.Add("                    type-name echo-request");
            config.Add("                }");
            config.Add("                protocol icmp");
            config.Add("                state new");
            config.Add("            }");
            config.Add("            rule 40 {");
            config.Add("                action accept");
            config.Add("                destination {");
            config.Add("                    port 53");
            config.Add("                }");
            config.Add("                protocol tcp_udp");
            config.Add("                source {");
            config.Add("                    group {");
            config.Add("                        network-group LAN-NETWORKS");
            config.Add("                    }");
            config.Add("                }");
            config.Add("            }");
            config.Add("            rule 50 {");
            config.Add("                action accept");
            config.Add("                source {");
            config.Add("                    address 127.0.0.0/8");
            config.Add("                }");
            config.Add("            }");
            config.Add("        }");
            config.Add("    }");
            config.Add("}");

            config.Add("interfaces {"); // Starts interfaces section
            foreach (var iface in data.Interfaces) // Adds each interface
            {
                config.Add($"    ethernet {iface.Name} {{");
                config.Add($"        address {iface.IPAddress}");
                config.Add($"        description '{iface.Description}'");
                if (!string.IsNullOrEmpty(iface.HwId))
                    config.Add($"        hw-id {iface.HwId}");
                if (!string.IsNullOrEmpty(iface.VlanId))
                    config.Add($"        vif {iface.VlanId} {{ }}");
                config.Add("    }");
            }
            config.Add("    loopback lo {");
            config.Add("    }");
            config.Add("}");

            config.Add("nat {"); // Starts NAT section
            config.Add("    destination {");
            config.Add("        rule 530 {");
            config.Add("            description \"Hijack Classic DNS\"");
            config.Add("            destination {");
            config.Add("                group {");
            config.Add("                    network-group !LAN-NETWORKS");
            config.Add("                }");
            config.Add("                port 53");
            config.Add("            }");
            config.Add("            inbound-interface {");
            config.Add("                group LAN-INTERFACES");
            config.Add("            }");
            config.Add("            protocol tcp_udp");
            config.Add("            translation {");
            config.Add("                address 10.0.10.1");
            config.Add("            }");
            config.Add("        }");
            config.Add("    }");
            config.Add("    source {");
            config.Add("    }");
            config.Add("}");

            config.Add("service {"); // Starts service section
            config.Add("    dhcp-server {");
            foreach (var dhcp in data.DhcpServers) // Adds each DHCP server
            {
                config.Add($"        shared-network-name {dhcp.NetworkName} {{");
                config.Add($"            subnet {dhcp.Subnet} {{");
                config.Add($"                default-router {dhcp.RouterIp}");
                config.Add($"                domain-name home.arpa");
                config.Add($"                lease 86400");
                config.Add($"                name-server {dhcp.DnsServer}");
                config.Add($"                range 0 {{");
                config.Add($"                    start {dhcp.RangeStart}");
                config.Add($"                    stop {dhcp.RangeEnd}");
                config.Add("                }");
                config.Add("            }");
                config.Add("        }");
            }
            config.Add("    }");
            config.Add("    dns {");
            config.Add("        forwarding {");
            config.Add("            cache-size 0");
            foreach (var dhcp in data.DhcpServers) // Adds DNS listen addresses
            {
                config.Add($"            listen-address {dhcp.RouterIp}");
                config.Add($"            allow-from {dhcp.Subnet}");
            }
            config.Add("        }");
            config.Add("    }");
            config.Add("    ntp {");
            config.Add("        allow-client {");
            config.Add("            address 127.0.0.0/8");
            config.Add("            address 169.254.0.0/16");
            config.Add("            address 10.0.0.0/8");
            config.Add("            address 172.16.0.0/12");
            config.Add("            address 192.168.0.0/16");
            config.Add("            address ::1/128");
            config.Add("            address fe80::/10");
            config.Add("            address fc00::/7");
            config.Add("        }");
            config.Add("        server time1.vyos.net {");
            config.Add("        }");
            config.Add("        server time2.vyos.net {");
            config.Add("        }");
            config.Add("        server time3.vyos.net {");
            config.Add("        }");
            config.Add("    }");
            config.Add("    ssh {");
            config.Add("        port 20022");
            if (sshPublicKey != null) // Adds SSH public key if provided
            {
                config.Add("        disable-password-authentication");
            }
            config.Add("    }");
            config.Add("}");

            config.Add("system {"); // Starts system section
            config.Add("    config-management {");
            config.Add("        commit-revisions 100");
            config.Add("    }");
            config.Add("    conntrack {");
            config.Add("        modules {");
            config.Add("            ftp");
            config.Add("            h323");
            config.Add("            nfs");
            config.Add("            pptp");
            config.Add("            sip");
            config.Add("            sqlnet");
            config.Add("            tftp");
            config.Add("        }");
            config.Add("    }");
            config.Add("    console {");
            config.Add("        device ttyS0 {");
            config.Add("            speed 115200");
            config.Add("        }");
            config.Add("    }");
            config.Add("    host-name vyos");
            config.Add("    login {");
            config.Add($"        user {username} {{");
            config.Add("            authentication {");
            config.Add($"                plaintext-password {password}");
            if (sshPublicKey != null) // Adds SSH key if provided
            {
                config.Add($"                public-keys {username}@device {{");
                config.Add("                    type ssh-rsa");
                config.Add($"                    key {sshPublicKey}");
                config.Add("                }");
            }
            config.Add("            }");
            config.Add("        }");
            config.Add("    }");
            config.Add("    name-server 1.1.1.3");
            config.Add("    name-server 1.0.0.3");
            config.Add("    syslog {");
            config.Add("        global {");
            config.Add("            facility all {");
            config.Add("                level info");
            config.Add("            }");
            config.Add("            facility local7 {");
            config.Add("                level debug");
            config.Add("            }");
            config.Add("        }");
            config.Add("    }");
            config.Add("}");

            return string.Join(Environment.NewLine, config); // Joins all lines into single string
        }

        /// <summary>
        /// Shows file save picker, returns selected file path.
        /// </summary>
        public static async Task<string> ShowSaveFileDialogAsync(Window window)
        {
            var picker = new FileSavePicker(); // Instantiates file picker
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary; // Sets default location
            picker.FileTypeChoices.Add("VyOS Config", new List<string>() { ".conf" }); // Sets allowed file type
            picker.SuggestedFileName = "vyos.conf"; // Sets default file name

            var hwnd = WindowNative.GetWindowHandle(window); // Gets window handle
            InitializeWithWindow.Initialize(picker, hwnd); // Initializes picker with window

            var file = await picker.PickSaveFileAsync(); // Shows picker, waits for user
            return file?.Path; // Returns selected file path or null
        }

        /// <summary>
        /// Exports VyOS config to user-selected location using file picker.
        /// </summary>
        public static async Task ExportVyOSConfigAsync(string stateSavePath, Window window, string username = "myvyosuser", string password = "defaultPassword123", string sshPublicKey = null)
        {
            try
            {
                string outputPath = await ShowSaveFileDialogAsync(window); // Gets output path from user
                if (string.IsNullOrEmpty(outputPath))
                    return; // Exits if cancelled

                GenerateVyOSConfig(stateSavePath, outputPath, username, password, sshPublicKey); // Generates and saves config
                Chatter.Log($"VyOS configuration successfully saved to {outputPath}", Chatter.MessageType.Info); // Logs success
            }
            catch (Exception ex)
            {
                Chatter.Log($"Error exporting config: {ex.Message}", Chatter.MessageType.Error); // Logs error
            }
        }
    }

    // --- Model classes ---

    public class NetworkData // Stores network data parsed from CSV
    {
        public List<InterfaceData> Interfaces { get; } = new List<InterfaceData>(); // List of interfaces
        public List<FirewallRule> FirewallRules { get; } = new List<FirewallRule>(); // List of firewall rules
        public List<NatRule> NatRules { get; } = new List<NatRule>(); // List of NAT rules
        public List<DhcpServer> DhcpServers { get; } = new List<DhcpServer>(); // List of DHCP servers
        public List<ObservedPort> ObservedPorts { get; } = new List<ObservedPort>(); // List of observed ports
        public List<string> RoutingProtocols { get; } = new List<string>(); // List of routing protocols
    }

    public class InterfaceData // Stores interface details
    {
        public string Name { get; set; } // Interface name
        public string IPAddress { get; set; } // Interface IP address
        public string Description { get; set; } // Interface description
        public string HwId { get; set; } // Hardware ID
        public string VlanId { get; set; } // VLAN ID
    }

    public class FirewallRule // Stores firewall rule details
    {
        public string Source { get; set; } // Source address
        public string Destination { get; set; } // Destination address
        public string Protocol { get; set; } // Protocol type
        public string Port { get; set; } // Port number
        public string Action { get; set; } // Rule action
    }

    public class NatRule // Stores NAT rule details
    {
        public string OutboundInterface { get; set; } // Outbound interface
        public string SourceNetwork { get; set; } // Source network
    }

    public class DhcpServer // Stores DHCP server details
    {
        public string NetworkName { get; set; } // DHCP network name
        public string Subnet { get; set; } // DHCP subnet
        public string RouterIp { get; set; } // Router IP address
        public string DnsServer { get; set; } // DNS server address
        public string RangeStart { get; set; } // DHCP range start
        public string RangeEnd { get; set; } // DHCP range end
    }

    public class ObservedPort // Stores observed port details
    {
        public string PortNumber { get; set; } // Port number
        public string Protocol { get; set; } // Protocol type
    }

    public static class Chatter // Handles logging to console
    {
        public enum MessageType { Info, Error, Warning } // Log message types

        public static void Log(string message, MessageType type) // Logs message with type
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {type}: {message}"; // Formats log entry
            Console.WriteLine(logEntry); // Writes log to console
        }
    }
}
