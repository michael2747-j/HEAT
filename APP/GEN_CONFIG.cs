using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml; // For Window
using Windows.Storage.Pickers;
using WinRT.Interop; // For InitializeWithWindow

namespace HEAT
{
    /// <summary>
    /// Generates and exports VyOS configuration files based on network data from STATE_SAVE.csv.
    /// </summary>
    public class VyOSConfigGenerator
    {
        /// <summary>
        /// Generates a VyOS configuration file from STATE_SAVE.csv data.
        /// </summary>
        public static void GenerateVyOSConfig(string stateSavePath, string outputPath, string username = "myvyosuser", string password = "defaultPassword123", string sshPublicKey = null)
        {
            try
            {
                // Read STATE_SAVE.csv
                var csvData = File.ReadAllLines(stateSavePath);

                // Parse the CSV data into structured objects
                var networkData = ParseNetworkData(csvData);

                // Generate the VyOS configuration
                string vyosConfig = GenerateConfigFromNetworkData(networkData, username, password, sshPublicKey);

                // Save to vyos.conf
                File.WriteAllText(outputPath, vyosConfig);

                // Log success (console only)
                Chatter.Log($"VyOS configuration successfully saved to {outputPath}", Chatter.MessageType.Info);
            }
            catch (Exception ex)
            {
                Chatter.Log($"Error generating VyOS config: {ex.Message}", Chatter.MessageType.Error);
            }
        }

        /// <summary>
        /// Parses network data from STATE_SAVE.csv into a structured NetworkData object.
        /// </summary>
        private static NetworkData ParseNetworkData(string[] csvLines)
        {
            var data = new NetworkData();
            string currentSection = null;
            string[] headers = null;

            for (int i = 0; i < csvLines.Length; i++)
            {
                var line = csvLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Section title (no commas)
                if (!line.Contains(","))
                {
                    currentSection = line.Trim(':');
                    headers = null;
                    continue;
                }

                // Header row
                if (headers == null)
                {
                    headers = line.Split(',').Select(h => h.Trim()).ToArray();
                    continue;
                }

                // Data row
                var columns = line.Split(',').Select(c => c.Trim()).ToArray();
                if (columns.Length < headers.Length)
                    continue;

                // Only parse "Total Network" section for now
                if (currentSection == "Total Network")
                {
                    // Indexes based on your CSV:
                    // 0: vLAN ID, 1: Interface/Subnet Seen, 2: LAN Device, 3: Common Name, 5: DHCP Server IP Address, 10: DNS Server IP, 17: Offered IP Range
                    var vlanId = columns[0];
                    var ifaceName = columns[1];
                    var lanDevice = columns[2];
                    var commonName = columns[3];
                    var dhcpServerIp = columns[5];
                    var dnsServerIp = columns[10];
                    var offeredIpRange = columns[17];

                    // Interface
                    if (!string.IsNullOrEmpty(ifaceName) && !string.IsNullOrEmpty(dhcpServerIp))
                    {
                        var iface = new InterfaceData
                        {
                            Name = ifaceName,
                            IPAddress = dhcpServerIp.Contains("DHCP") ? "dhcp" : $"{dhcpServerIp}/24",
                            Description = !string.IsNullOrEmpty(commonName) ? commonName : ifaceName,
                            HwId = columns[6],
                            VlanId = vlanId
                        };
                        if (!data.Interfaces.Any(i => i.Name == iface.Name && i.IPAddress == iface.IPAddress))
                            data.Interfaces.Add(iface);
                    }

                    // DHCP Server
                    if (!string.IsNullOrEmpty(offeredIpRange) && offeredIpRange.Contains('-'))
                    {
                        var rangeParts = offeredIpRange.Split('-');
                        var dhcp = new DhcpServer
                        {
                            NetworkName = ifaceName.Replace(".", "_").ToUpper(),
                            Subnet = $"{dhcpServerIp}/24",
                            RouterIp = dhcpServerIp,
                            DnsServer = string.IsNullOrEmpty(dnsServerIp) ? dhcpServerIp : dnsServerIp,
                            RangeStart = rangeParts[0],
                            RangeEnd = rangeParts[1]
                        };
                        if (!data.DhcpServers.Any(d => d.Subnet == dhcp.Subnet))
                            data.DhcpServers.Add(dhcp);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Generates the VyOS configuration text from parsed network data.
        /// </summary>
        private static string GenerateConfigFromNetworkData(NetworkData data, string username, string password, string sshPublicKey)
        {
            var config = new List<string>();

            // Container configuration (static sample)
            config.Add("container {");
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

            // Firewall configuration (static sample)
            config.Add("firewall {");
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
            foreach (var dhcp in data.DhcpServers)
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

            // Interfaces
            config.Add("interfaces {");
            foreach (var iface in data.Interfaces)
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

            // NAT configuration (empty, as not parsed from your CSV)
            config.Add("nat {");
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

            // Services
            config.Add("service {");
            config.Add("    dhcp-server {");
            foreach (var dhcp in data.DhcpServers)
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
            foreach (var dhcp in data.DhcpServers)
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
            if (sshPublicKey != null)
            {
                config.Add("        disable-password-authentication");
            }
            config.Add("    }");
            config.Add("}");

            // System configuration
            config.Add("system {");
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
            if (sshPublicKey != null)
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

            return string.Join(Environment.NewLine, config);
        }

        /// <summary>
        /// Shows a WinUI File Save Picker and returns the selected file path.
        /// </summary>
        public static async Task<string> ShowSaveFileDialogAsync(Window window)
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("VyOS Config", new List<string>() { ".conf" });
            picker.SuggestedFileName = "vyos.conf";

            // Get the HWND of the current window and initialize the picker
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        /// <summary>
        /// Exports the VyOS configuration to a user-selected location using WinUI file picker.
        /// </summary>
        public static async Task ExportVyOSConfigAsync(string stateSavePath, Window window, string username = "myvyosuser", string password = "defaultPassword123", string sshPublicKey = null)
        {
            try
            {
                string outputPath = await ShowSaveFileDialogAsync(window);
                if (string.IsNullOrEmpty(outputPath))
                    return; // User cancelled

                GenerateVyOSConfig(stateSavePath, outputPath, username, password, sshPublicKey);
                Chatter.Log($"VyOS configuration successfully saved to {outputPath}", Chatter.MessageType.Info);
            }
            catch (Exception ex)
            {
                Chatter.Log($"Error exporting config: {ex.Message}", Chatter.MessageType.Error);
            }
        }
    }

    // --- Model classes ---

    public class NetworkData
    {
        public List<InterfaceData> Interfaces { get; } = new List<InterfaceData>();
        public List<FirewallRule> FirewallRules { get; } = new List<FirewallRule>();
        public List<NatRule> NatRules { get; } = new List<NatRule>();
        public List<DhcpServer> DhcpServers { get; } = new List<DhcpServer>();
        public List<ObservedPort> ObservedPorts { get; } = new List<ObservedPort>();
        public List<string> RoutingProtocols { get; } = new List<string>();
    }

    public class InterfaceData
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Description { get; set; }
        public string HwId { get; set; }
        public string VlanId { get; set; }
    }

    public class FirewallRule
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Protocol { get; set; }
        public string Port { get; set; }
        public string Action { get; set; }
    }

    public class NatRule
    {
        public string OutboundInterface { get; set; }
        public string SourceNetwork { get; set; }
    }

    public class DhcpServer
    {
        public string NetworkName { get; set; }
        public string Subnet { get; set; }
        public string RouterIp { get; set; }
        public string DnsServer { get; set; }
        public string RangeStart { get; set; }
        public string RangeEnd { get; set; }
    }

    public class ObservedPort
    {
        public string PortNumber { get; set; }
        public string Protocol { get; set; }
    }

    public static class Chatter
    {
        public enum MessageType { Info, Error, Warning }

        public static void Log(string message, MessageType type)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {type}: {message}";
            // Only log to console; do NOT write to disk
            Console.WriteLine(logEntry); // Placeholder for UI integration
        }
    }
}
