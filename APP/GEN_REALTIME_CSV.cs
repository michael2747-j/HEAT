// GEN_REALTIME_CSV.cs
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace HEAT
{
    // ===================== Data Models =====================
    public static class DataModels
    {
        public class LanInterfaceConfig
        {
            public string VLANId = "";
            public string LanDevice = "";
            public HashSet<string> CommonNames = new();
            public HashSet<string> SourceIps = new();
            public HashSet<string> DestinationIps = new();
            public HashSet<string> EthernetTypes = new();
            public HashSet<string> NetworkLayers = new();
            public HashSet<string> Protocols = new();
            public HashSet<string> DestinationPorts = new();
        }

        public class WanInterfaceConfig
        {
            public string VLANId = "";
            public string WanAddress = "";
            public HashSet<string> SourceIps = new();
            public HashSet<string> DestinationIps = new();
            public HashSet<string> EthernetTypes = new();
            public HashSet<string> NetworkLayers = new();
            public HashSet<string> Protocols = new();
            public HashSet<string> DestinationPorts = new();
        }

        public class VLANInterfaceConfig
        {
            public string VLANId = "";
            public string LanDevice = "";
            public HashSet<string> SourceIps = new();
            public HashSet<string> DestinationIps = new();
            public HashSet<string> EthernetTypes = new();
            public HashSet<string> NetworkLayers = new();
            public HashSet<string> Protocols = new();
            public HashSet<string> DestinationPorts = new();
        }

        public class NatConfig
        {
            public string VLANId = "";
            public HashSet<string> SourceIps = new();
            public string NatTranslatedIp = "";
            public HashSet<string> DestinationIps = new();
            public HashSet<string> Protocols = new();
            public HashSet<string> DestinationPorts = new();
        }

        public class DhcpServerUse
        {
            public string VLANId = "";
            public HashSet<string> InterfaceSubnets = new();
            public HashSet<string> DhcpServerIps = new();
            public HashSet<string> DhcpServerMacs = new();
            public HashSet<string> OfferedIpRanges = new();
            public HashSet<string> Protocols = new();
        }

        public class DnsServerUse
        {
            public string VLANId = "";
            public HashSet<string> SourceIps = new();
            public HashSet<string> DnsServerIps = new();
            public HashSet<string> Fqdns = new();
            public HashSet<string> Protocols = new();
        }

        public class VpnTunnelUse
        {
            public string VLANId = "";
            public HashSet<string> DestinationIps = new();
            public HashSet<string> DestinationPorts = new();
            public HashSet<string> NetworkLayers = new();
            public HashSet<string> Protocols = new();
        }

        public class AdControllerNonPublicDns
        {
            public string VLANId = "";
            public HashSet<string> SourceIps = new();
            public HashSet<string> DestinationIps = new();
            public HashSet<bool> SrvQueries = new();
            public HashSet<bool> NonPublicDomains = new();
            public HashSet<string> Protocols = new();
        }

        public static readonly HashSet<int> udpPorts = new()
        {
            67, 68, 123, 137, 138, 161, 162, 500, 4500, 1701, 51820, 1194, 1195,
            514, 520, 521, 88
        };

        public static readonly HashSet<int> tcpPorts = new()
        {
            17, 20, 21, 22, 23, 25, 53, 80, 110, 143, 139, 1433, 1434, 389, 636, 443,
            445, 1723, 3389, 3306, 8080, 8443, 179, 88
        };

        public static readonly HashSet<int> ipProtocols = new() { 50, 51, 89 }; // ESP, AH, OSPF

        public static readonly Dictionary<string, LanInterfaceConfig> lanConfigs = new();
        public static readonly Dictionary<string, WanInterfaceConfig> wanConfigs = new();
        public static readonly Dictionary<string, VLANInterfaceConfig> vlanConfigs = new();
        public static readonly Dictionary<string, NatConfig> natConfigs = new();
        public static readonly Dictionary<string, DhcpServerUse> dhcpServers = new();
        public static readonly Dictionary<string, DnsServerUse> dnsServers = new();
        public static readonly Dictionary<string, VpnTunnelUse> vpnTunnels = new();
        public static readonly Dictionary<string, AdControllerNonPublicDns> adControllers = new();

        public static string natTranslatedIp = "";

        public static void ClearAllData()
        {
            lanConfigs.Clear();
            wanConfigs.Clear();
            vlanConfigs.Clear();
            natConfigs.Clear();
            dhcpServers.Clear();
            dnsServers.Clear();
            vpnTunnels.Clear();
            adControllers.Clear();
        }
    }

    // ===================== Network Capture =====================
    public static class NetworkCapture
    {
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(3) };
        public static readonly object dataLock = new();
        private static CancellationTokenSource? _globalCts;
        private static Task? _writeTask;
        private static List<ICaptureDevice> _activeDevices = new();

        public static async Task<(bool success, List<ICaptureDevice> devices)> gen_rt_csv(
            Window parentWindow,
            Func<string, Color, Task>? aiChatterCallback = null,
            List<ICaptureDevice>? preSelectedDevices = null,
            CancellationToken? cancellationToken = null)
        {
            // Cancel any existing capture
            if (_globalCts != null && !_globalCts.IsCancellationRequested)
            {
                _globalCts.Cancel();
                try
                {
                    await Task.WhenAll(_activeDevices.Select(d => Task.Run(() => {
                        try { d.StopCapture(); d.Close(); } catch { }
                    })));
                    _activeDevices.Clear();
                }
                catch { }
            }

            DataModels.ClearAllData();
            _globalCts = new CancellationTokenSource();
            var ct = cancellationToken ?? _globalCts.Token;

            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                await ShowMessageDialog("No network interfaces found.", parentWindow);
                if (aiChatterCallback != null)
                    await aiChatterCallback("No network interfaces found.", Colors.Red);
                return (false, new List<ICaptureDevice>());
            }

            List<ICaptureDevice> selectedDevices;
            if (preSelectedDevices != null && preSelectedDevices.Any())
            {
                selectedDevices = preSelectedDevices;
            }
            else
            {
                var checkboxes = devices.Select(dev => new CheckBox
                {
                    Content = dev.Description,
                    Tag = dev
                }).ToList();

                var panel = new StackPanel();
                foreach (var cb in checkboxes) panel.Children.Add(cb);

                var dialog = new ContentDialog
                {
                    Title = "Select Network Interfaces to Monitor",
                    Content = panel,
                    PrimaryButtonText = "Start",
                    CloseButtonText = "Cancel",
                    XamlRoot = parentWindow.Content.XamlRoot
                };

                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                    return (false, new List<ICaptureDevice>());

                selectedDevices = checkboxes
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => (ICaptureDevice)cb.Tag)
                    .ToList();
            }

            if (selectedDevices.Count == 0)
            {
                await ShowMessageDialog("No interfaces selected.", parentWindow);
                if (aiChatterCallback != null)
                    await aiChatterCallback("No interfaces selected.", Colors.OrangeRed);
                return (false, new List<ICaptureDevice>());
            }

            string csvPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "REALTIME_LOG.csv"
            );

            if (aiChatterCallback != null)
                await aiChatterCallback($"Initializing real-time network analysis on {selectedDevices.Count} interface(s)...", Colors.LightGreen);

            DataModels.natTranslatedIp = await GetNatTranslatedIpAsync();

            _writeTask = CsvWriter.WriteAggregatedDataToCsv(csvPath, ct);

            _activeDevices.Clear();
            foreach (var dev in selectedDevices)
            {
                if (ct.IsCancellationRequested) break;

                // Skip loopback interfaces
                if (dev is LibPcapLiveDevice pcapDev &&
                    pcapDev.Interface.Name.Contains("loopback", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dev.OnPacketArrival += (object sender, PacketCapture e) =>
                {
                    if (ct.IsCancellationRequested) return;

                    try
                    {
                        var device = sender as ICaptureDevice;
                        var rawPacket = e.GetPacket();
                        LinkLayers linkLayers = (device is LibPcapLiveDevice liveDev) ?
                            liveDev.LinkType : LinkLayers.Ethernet;
                        var packet = Packet.ParsePacket(linkLayers, rawPacket.Data);

                        var eth = packet.Extract<EthernetPacket>();
                        var ip = packet.Extract<IPPacket>();
                        var udp = packet.Extract<UdpPacket>();
                        var tcp = packet.Extract<TcpPacket>();

                        if (ip == null) return;

                        string vlanId = "0";
                        if (eth != null && (ushort)eth.Type == 0x8100)
                        {
                            var vlanBytes = rawPacket.Data.Skip(14).Take(2).ToArray();
                            if (vlanBytes.Length == 2)
                                vlanId = ((vlanBytes[0] << 8) | vlanBytes[1]).ToString();
                        }

                        string srcIp = ip.SourceAddress?.ToString() ?? "";
                        string dstIp = ip.DestinationAddress?.ToString() ?? "";
                        string ethType = eth != null ? GetEthernetType((ushort)eth.Type) : "";
                        string netLayer = ip.Version.ToString();
                        string trafficType = GetTrafficTypeProtocol(ip, udp, tcp);
                        string dstPortStr = (udp != null ? udp.DestinationPort :
                                           tcp != null ? tcp.DestinationPort : 0).ToString();

                        string commonNameLan = GetCommonName(srcIp);
                        string commonNameWan = GetCommonName(dstIp);

                        bool isWan = IsWan(ip.DestinationAddress);
                        bool isVpnTraffic = udp != null && DataModels.udpPorts.Contains(udp.DestinationPort);

                        bool isDhcp = udp != null && (udp.DestinationPort == 67 || udp.DestinationPort == 68);
                        string dhcpMac = "";
                        string dhcpRange = "";
                        if (isDhcp && udp != null)
                        {
                            var payload = udp.PayloadData;
                            if (payload != null && payload.Length >= 34)
                            {
                                dhcpMac = new PhysicalAddress(payload.Skip(28).Take(6).ToArray()).ToString() ?? "Unknown";
                                dhcpRange = new IPAddress(payload.Skip(16).Take(4).ToArray()).ToString() ?? "Unknown";
                            }
                        }

                        bool isDns = udp != null && (udp.DestinationPort == 53 || udp.DestinationPort == 5353);
                        string fqdn = "";
                        bool isSrvQuery = false;
                        bool isNonPublicDomain = false;
                        if (isDns)
                        {
                            fqdn = ParseDnsQuery(packet.PayloadData);
                            isSrvQuery = IsSrvQuery(packet.PayloadData);
                            isNonPublicDomain = IsNonPublicDomain(fqdn);
                        }

                        lock (dataLock)
                        {
                            if (!string.IsNullOrEmpty(device?.Description))
                            {
                                string lanKey = vlanId + "|" + device.Description;
                                if (!DataModels.lanConfigs.TryGetValue(lanKey, out var lanConfig))
                                {
                                    lanConfig = new DataModels.LanInterfaceConfig
                                    {
                                        VLANId = vlanId,
                                        LanDevice = device.Description
                                    };
                                    DataModels.lanConfigs[lanKey] = lanConfig;
                                }
                                lanConfig.CommonNames.Add(commonNameLan);
                                lanConfig.SourceIps.Add(srcIp);
                                lanConfig.DestinationIps.Add(dstIp);
                                lanConfig.EthernetTypes.Add(ethType);
                                lanConfig.NetworkLayers.Add(netLayer);
                                lanConfig.Protocols.Add(trafficType);
                                lanConfig.DestinationPorts.Add(dstPortStr);
                            }

                            if (isWan)
                            {
                                string wanKey = vlanId + "|" + dstIp + "|" + device.Description;
                                if (!DataModels.wanConfigs.TryGetValue(wanKey, out var wanConfig))
                                {
                                    wanConfig = new DataModels.WanInterfaceConfig
                                    {
                                        VLANId = vlanId,
                                        WanAddress = dstIp
                                    };
                                    DataModels.wanConfigs[wanKey] = wanConfig;
                                }
                                wanConfig.SourceIps.Add(srcIp);
                                wanConfig.DestinationIps.Add(dstIp);
                                wanConfig.EthernetTypes.Add(ethType);
                                wanConfig.NetworkLayers.Add(netLayer);
                                wanConfig.Protocols.Add(trafficType);
                                wanConfig.DestinationPorts.Add(dstPortStr);
                            }

                            if (!string.IsNullOrEmpty(device?.Description))
                            {
                                string vlanKey = vlanId + "|" + device.Description;
                                if (!DataModels.vlanConfigs.TryGetValue(vlanKey, out var vlanConfig))
                                {
                                    vlanConfig = new DataModels.VLANInterfaceConfig
                                    {
                                        VLANId = vlanId,
                                        LanDevice = device.Description
                                    };
                                    DataModels.vlanConfigs[vlanKey] = vlanConfig;
                                }
                                vlanConfig.SourceIps.Add(srcIp);
                                vlanConfig.DestinationIps.Add(dstIp);
                                vlanConfig.EthernetTypes.Add(ethType);
                                vlanConfig.NetworkLayers.Add(netLayer);
                                vlanConfig.Protocols.Add(trafficType);
                                vlanConfig.DestinationPorts.Add(dstPortStr);
                            }

                            // Always record NAT configs
                            {
                                string natKey = vlanId + "|" + device.Description;
                                if (!DataModels.natConfigs.TryGetValue(natKey, out var natConfig))
                                {
                                    natConfig = new DataModels.NatConfig
                                    {
                                        VLANId = vlanId,
                                        NatTranslatedIp = DataModels.natTranslatedIp
                                    };
                                    DataModels.natConfigs[natKey] = natConfig;
                                }
                                natConfig.SourceIps.Add(srcIp);
                                natConfig.DestinationIps.Add(dstIp);
                                natConfig.Protocols.Add(trafficType);
                                natConfig.DestinationPorts.Add(dstPortStr);
                            }

                            if (isDhcp)
                            {
                                string dhcpKey = vlanId + "|" + device.Description;
                                if (!DataModels.dhcpServers.TryGetValue(dhcpKey, out var dhcp))
                                {
                                    dhcp = new DataModels.DhcpServerUse
                                    {
                                        VLANId = vlanId
                                    };
                                    DataModels.dhcpServers[dhcpKey] = dhcp;
                                }
                                dhcp.InterfaceSubnets.Add(device?.Description ?? "");
                                dhcp.DhcpServerIps.Add(dstIp);
                                dhcp.DhcpServerMacs.Add(dhcpMac);
                                dhcp.OfferedIpRanges.Add(dhcpRange);
                                dhcp.Protocols.Add(trafficType);
                            }

                            if (isDns)
                            {
                                string dnsKey = vlanId + "|" + device.Description;
                                if (!DataModels.dnsServers.TryGetValue(dnsKey, out var dns))
                                {
                                    dns = new DataModels.DnsServerUse
                                    {
                                        VLANId = vlanId
                                    };
                                    DataModels.dnsServers[dnsKey] = dns;
                                }
                                dns.SourceIps.Add(srcIp);
                                dns.DnsServerIps.Add(dstIp);
                                if (!string.IsNullOrEmpty(fqdn))
                                    dns.Fqdns.Add(fqdn);
                                dns.Protocols.Add(trafficType);
                            }

                            if (isVpnTraffic)
                            {
                                string vpnKey = vlanId + "|" + device.Description;
                                if (!DataModels.vpnTunnels.TryGetValue(vpnKey, out var vpn))
                                {
                                    vpn = new DataModels.VpnTunnelUse
                                    {
                                        VLANId = vlanId
                                    };
                                    DataModels.vpnTunnels[vpnKey] = vpn;
                                }
                                vpn.DestinationIps.Add(dstIp);
                                vpn.DestinationPorts.Add(dstPortStr);
                                vpn.NetworkLayers.Add(netLayer);
                                vpn.Protocols.Add(trafficType);
                            }

                            // Always record AD configs
                            {
                                string adKey = vlanId + "|" + device.Description;
                                if (!DataModels.adControllers.TryGetValue(adKey, out var ad))
                                {
                                    ad = new DataModels.AdControllerNonPublicDns
                                    {
                                        VLANId = vlanId
                                    };
                                    DataModels.adControllers[adKey] = ad;
                                }
                                ad.SourceIps.Add(srcIp);
                                ad.DestinationIps.Add(dstIp);
                                ad.SrvQueries.Add(isSrvQuery);
                                ad.NonPublicDomains.Add(isNonPublicDomain);
                                ad.Protocols.Add(trafficType);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Packet parsing error: {ex.Message}");
                    }
                };

                try
                {
                    dev.Open();
                    dev.StartCapture();
                    _activeDevices.Add(dev);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start capture on {dev.Description}: {ex.Message}");
                    if (aiChatterCallback != null)
                        await aiChatterCallback($"Failed to start capture on {dev.Description}: {ex.Message}", Colors.Orange);
                }
            }

            if (aiChatterCallback != null)
                await aiChatterCallback($"Network analysis started on {_activeDevices.Count} interface(s). Data is being written to REALTIME_LOG.csv.", Colors.LightGreen);

            return (true, _activeDevices);
        }

        public static async Task StopCaptureAsync()
        {
            if (_globalCts != null && !_globalCts.IsCancellationRequested)
            {
                _globalCts.Cancel();
                try
                {
                    foreach (var dev in _activeDevices)
                    {
                        try
                        {
                            if (dev.Started)
                            {
                                dev.StopCapture();
                                dev.Close();
                            }
                        }
                        catch { }
                    }
                    _activeDevices.Clear();

                    if (_writeTask != null && !_writeTask.IsCompleted)
                    {
                        await Task.WhenAny(_writeTask, Task.Delay(2000));
                    }
                }
                catch { }
            }
        }

        private static string GetEthernetType(ushort ethType)
        {
            if (ethType == 0x8100) return "802.1Q Tag (VLAN/Q-Tag)";
            if (ethType <= 0x05DC) return "Ethernet II (DIX)";
            return $"Ethernet Type 0x{ethType:X4}";
        }

        private static string GetTrafficTypeProtocol(IPPacket ip, UdpPacket? udp, TcpPacket? tcp)
        {
            if (udp != null)
            {
                int port = udp.DestinationPort;
                if (port == 520)
                    return "RIP";
                return "UDP";
            }
            else if (tcp != null)
            {
                int port = tcp.DestinationPort;
                if (port == 179)
                    return "BGP";
                return "TCP";
            }
            else if (ip != null)
            {
                int protoNum = (int)ip.Protocol;
                return protoNum switch
                {
                    50 => "ESP",
                    51 => "AH",
                    89 => "OSPF",
                    _ => ip.Protocol.ToString()
                };
            }
            return "Unknown";
        }

        private static async Task<string> GetNatTranslatedIpAsync()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "nslookup",
                    Arguments = "myip.opendns.com resolver1.opendns.com",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return "Unknown";

                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (IPAddress.TryParse(lines[i].Trim(), out _))
                        return lines[i].Trim();
                }
            }
            catch
            {
                string[] ipServices =
                {
                    "https://api.ipify.org",
                    "https://ipinfo.io/ip",
                    "https://checkip.amazonaws.com",
                    "https://ifconfig.me/ip"
                };

                foreach (var service in ipServices)
                {
                    try
                    {
                        string ip = await httpClient.GetStringAsync(service);
                        ip = ip.Trim();
                        if (IsValidIp(ip)) return ip;
                    }
                    catch { }
                }
            }
            return "Unknown";
        }

        private static bool IsValidIp(string ip)
        {
            return !string.IsNullOrEmpty(ip) &&
                   (ip.Contains('.') || ip.Contains(':')) &&
                   !ip.Contains(' ') &&
                   !ip.Contains('\n');
        }

        private static string GetCommonName(string? ip)
        {
            if (string.IsNullOrEmpty(ip) || string.Equals(ip, "Unknown", StringComparison.OrdinalIgnoreCase)) return ip ?? "";
            try
            {
                var host = Dns.GetHostEntry(ip);
                return host.HostName;
            }
            catch
            {
                return ip; // Fallback to IP address
            }
        }

        private static bool IsWan(IPAddress? ip)
        {
            if (ip == null) return false;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 10) return false;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                if (bytes[0] == 192 && bytes[1] == 168) return false;
                return true;
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6SiteLocal) return false;
                if (ip.IsIPv6LinkLocal) return false;
                return true;
            }

            return false;
        }

        private static string ParseDnsQuery(byte[]? payload)
        {
            if (payload == null || payload.Length < 12) return "";
            try
            {
                int idx = 12; // Skip DNS header
                List<string> labels = new();
                while (idx < payload.Length && payload[idx] != 0)
                {
                    int len = payload[idx++];
                    if (len == 0 || idx + len > payload.Length) break;
                    labels.Add(Encoding.ASCII.GetString(payload, idx, len));
                    idx += len;
                }
                if (idx + 4 <= payload.Length)
                {
                    ushort qtype = (ushort)((payload[idx + 1] << 8) | payload[idx + 2]);
                    if (qtype == 33) // SRV record
                        return string.Join(".", labels) + " (SRV)";
                }
                return string.Join(".", labels);
            }
            catch { return ""; }
        }

        private static bool IsSrvQuery(byte[]? payload)
        {
            if (payload == null || payload.Length < 16) return false;
            try
            {
                int idx = 12;
                while (idx < payload.Length && payload[idx] != 0)
                {
                    int len = payload[idx++];
                    if (len == 0 || idx + len > payload.Length) break;
                    idx += len;
                }
                if (idx + 4 <= payload.Length)
                {
                    ushort qtype = (ushort)((payload[idx + 1] << 8) | payload[idx + 2]);
                    return qtype == 33; // SRV record type
                }
                return false;
            }
            catch { return false; }
        }

        private static bool IsNonPublicDomain(string fqdn)
        {
            if (string.IsNullOrEmpty(fqdn)) return false;
            return fqdn.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                   fqdn.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                   fqdn.EndsWith(".lan", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task ShowMessageDialog(string message, Window parentWindow)
        {
            var dialog = new ContentDialog
            {
                Title = "Network Analysis",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = parentWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    // ===================== CSV Writer =====================
    public static class CsvWriter
    {
        private static readonly object dataLock = new();

        public static async Task WriteAggregatedDataToCsv(string csvPath, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                List<string[]> totalNetworkRows, lanRows, wanRows, vlanRows, natRows, dhcpRows, dnsRows, vpnRows, adRows, routingRows;

                lock (dataLock)
                {
                    var totalHeader = new[] { "Total Network:", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                    var totalColumns = new[]
                    {
                        "vLAN ID", "Interface/Subnet Seen", "Common Name", "WAN Address",
                        "DHCP Server IP Address", "DHCP Server MAC Address", "Source IP Address", "NAT Translated IP",
                        "Destination IP Address", "DNS Server IP (Destination)", "Destination Port", "Ethernet Type",
                        "Network Layer", "Traffic Type/Protocol", "FQDN Queried", "SRV Query", "Non-Public Domain Queried", "Offered IP Range", "Routing Protocol Type"
                    };
                    totalNetworkRows = new List<string[]>();
                    totalNetworkRows.Add(totalHeader);
                    totalNetworkRows.Add(totalColumns);

                    var totalNetworkDict = new Dictionary<string, string[]>();

                    // LAN part
                    foreach (var lanKey in DataModels.lanConfigs.Keys)
                    {
                        var lan = DataModels.lanConfigs[lanKey];
                        var parts = lanKey.Split('|');
                        if (parts.Length >= 2)
                        {
                            string actualVlanId = parts[0];
                            string deviceDescription = parts[1];
                            foreach (var srcIp in lan.SourceIps)
                            {
                                foreach (var dstIp in lan.DestinationIps)
                                {
                                    string key = $"{actualVlanId}|{deviceDescription}|{srcIp}|{dstIp}";
                                    if (!totalNetworkDict.ContainsKey(key))
                                    {
                                        totalNetworkDict[key] = new string[19];
                                        totalNetworkDict[key][0] = actualVlanId;
                                        totalNetworkDict[key][1] = deviceDescription;
                                        totalNetworkDict[key][2] = string.Join(", ", lan.CommonNames.Distinct());
                                        totalNetworkDict[key][6] = srcIp;
                                        totalNetworkDict[key][8] = dstIp;
                                        totalNetworkDict[key][10] = string.Join(", ", lan.DestinationPorts.Distinct());
                                        totalNetworkDict[key][11] = string.Join(", ", lan.EthernetTypes.Distinct());
                                        totalNetworkDict[key][12] = string.Join(", ", lan.NetworkLayers.Distinct());
                                        totalNetworkDict[key][13] = string.Join(", ", lan.Protocols.Distinct());
                                    }
                                }
                            }
                        }
                    }

                    // DHCP part
                    foreach (var dhcpKey in DataModels.dhcpServers.Keys)
                    {
                        var dhcp = DataModels.dhcpServers[dhcpKey];
                        var parts = dhcpKey.Split('|');
                        if (parts.Length >= 2)
                        {
                            string actualVlanId = parts[0];
                            string deviceDescription = parts[1];
                            foreach (var key in totalNetworkDict.Keys)
                            {
                                var keyParts = key.Split('|');
                                if (keyParts.Length >= 4 && keyParts[0] == actualVlanId && keyParts[1] == deviceDescription)
                                {
                                    var row = totalNetworkDict[key];
                                    row[4] = string.Join(", ", dhcp.DhcpServerIps.Distinct());
                                    row[5] = string.Join(", ", dhcp.DhcpServerMacs.Distinct());
                                    row[17] = string.Join(", ", dhcp.OfferedIpRanges.Distinct());
                                    row[13] = string.Join(", ", dhcp.Protocols.Distinct());
                                }
                            }
                        }
                    }

                    // WAN part
                    foreach (var wanKey in DataModels.wanConfigs.Keys)
                    {
                        var wan = DataModels.wanConfigs[wanKey];
                        var parts = wanKey.Split('|');
                        if (parts.Length >= 3)
                        {
                            string actualVlanId = parts[0];
                            string dstIp = parts[1];
                            string deviceDescription = parts[2];
                            foreach (var srcIp in wan.SourceIps)
                            {
                                string key = $"{actualVlanId}|{deviceDescription}|{srcIp}|{dstIp}";
                                if (totalNetworkDict.TryGetValue(key, out var row))
                                {
                                    row[3] = wan.WanAddress;
                                }
                            }
                        }
                    }

                    // NAT part
                    foreach (var natKey in DataModels.natConfigs.Keys)
                    {
                        var nat = DataModels.natConfigs[natKey];
                        var parts = natKey.Split('|');
                        if (parts.Length >= 2)
                        {
                            string actualVlanId = parts[0];
                            string deviceDescription = parts[1];
                            foreach (var srcIp in nat.SourceIps)
                            {
                                foreach (var dstIp in nat.DestinationIps)
                                {
                                    string key = $"{actualVlanId}|{deviceDescription}|{srcIp}|{dstIp}";
                                    if (totalNetworkDict.TryGetValue(key, out var row))
                                    {
                                        row[7] = nat.NatTranslatedIp;
                                    }
                                }
                            }
                        }
                    }

                    // DNS part
                    foreach (var dnsKey in DataModels.dnsServers.Keys)
                    {
                        var dns = DataModels.dnsServers[dnsKey];
                        var parts = dnsKey.Split('|');
                        if (parts.Length >= 2)
                        {
                            string actualVlanId = parts[0];
                            string deviceDescription = parts[1];
                            foreach (var srcIp in dns.SourceIps)
                            {
                                foreach (var dstIp in dns.DnsServerIps)
                                {
                                    string key = $"{actualVlanId}|{deviceDescription}|{srcIp}|{dstIp}";
                                    if (totalNetworkDict.TryGetValue(key, out var row))
                                    {
                                        row[9] = dstIp;
                                        row[14] = string.Join(", ", dns.Fqdns.Distinct());
                                        row[13] = string.Join(", ", dns.Protocols.Distinct());
                                    }
                                }
                            }
                        }
                    }

                    // AD part
                    foreach (var adKey in DataModels.adControllers.Keys)
                    {
                        var ad = DataModels.adControllers[adKey];
                        var parts = adKey.Split('|');
                        if (parts.Length >= 2)
                        {
                            string actualVlanId = parts[0];
                            string deviceDescription = parts[1];
                            foreach (var srcIp in ad.SourceIps)
                            {
                                foreach (var dstIp in ad.DestinationIps)
                                {
                                    string key = $"{actualVlanId}|{deviceDescription}|{srcIp}|{dstIp}";
                                    if (totalNetworkDict.TryGetValue(key, out var row))
                                    {
                                        row[15] = string.Join(", ", ad.SrvQueries.Select(b => b ? "Yes" : "No").Distinct());
                                        row[16] = string.Join(", ", ad.NonPublicDomains.Select(b => b ? "Yes" : "No").Distinct());
                                        row[13] = string.Join(", ", ad.Protocols.Distinct());
                                    }
                                }
                            }
                        }
                    }

                    // Add rows to totalNetworkRows
                    foreach (var row in totalNetworkDict.Values)
                    {
                        for (int i = 0; i < row.Length; i++)
                            if (row[i] == null) row[i] = "";
                        totalNetworkRows.Add(row);
                    }

                    // LAN Interface Configurations
                    var lanHeader = new[] { "LAN Interface Configurations:", "", "", "", "", "", "", "", "" };
                    var lanColumns = new[] { "vLAN ID", "LAN Device", "Common Name", "Source IP Address", "Destination IP Address", "Ethernet Type", "Network Layer", "Traffic Type/Protocol", "Destination Port" };
                    lanRows = new List<string[]>();
                    lanRows.Add(lanHeader);
                    lanRows.Add(lanColumns);
                    foreach (var lan in DataModels.lanConfigs.Values.OrderBy(l => l.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        lanRows.Add(new[]
                        {
                            lan.VLANId,
                            lan.LanDevice,
                            string.Join(", ", lan.CommonNames.Distinct()),
                            string.Join(", ", lan.SourceIps.Distinct()),
                            string.Join(", ", lan.DestinationIps.Distinct()),
                            string.Join(", ", lan.EthernetTypes.Distinct()),
                            string.Join(", ", lan.NetworkLayers.Distinct()),
                            string.Join(", ", lan.Protocols.Distinct()),
                            string.Join(", ", lan.DestinationPorts.Distinct())
                        });
                    }

                    // WAN Interface Configurations
                    var wanHeader = new[] { "WAN Interface Configurations:", "", "", "", "", "", "", "" };
                    var wanColumns = new[] { "vLAN ID", "WAN Address", "Source IP Address", "Destination IP Address", "Ethernet Type", "Network Layer", "Traffic Type/Protocol", "Destination Port" };
                    wanRows = new List<string[]>();
                    wanRows.Add(wanHeader);
                    wanRows.Add(wanColumns);
                    foreach (var wan in DataModels.wanConfigs.Values.OrderBy(w => w.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        wanRows.Add(new[]
                        {
                            wan.VLANId,
                            wan.WanAddress,
                            string.Join(", ", wan.SourceIps.Distinct()),
                            string.Join(", ", wan.DestinationIps.Distinct()),
                            string.Join(", ", wan.EthernetTypes.Distinct()),
                            string.Join(", ", wan.NetworkLayers.Distinct()),
                            string.Join(", ", wan.Protocols.Distinct()),
                            string.Join(", ", wan.DestinationPorts.Distinct())
                        });
                    }

                    // vLAN Interface Configurations
                    var vlanHeader = new[] { "vLAN Interface Configurations:", "", "", "", "", "", "", "" };
                    var vlanColumns = new[] { "vLAN ID", "LAN Device", "Source IP Address", "Destination IP Address", "Ethernet Type", "Network Layer", "Traffic Type/Protocol", "Destination Port" };
                    vlanRows = new List<string[]>();
                    vlanRows.Add(vlanHeader);
                    vlanRows.Add(vlanColumns);
                    foreach (var vlan in DataModels.vlanConfigs.Values.OrderBy(v => v.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        vlanRows.Add(new[]
                        {
                            vlan.VLANId,
                            vlan.LanDevice,
                            string.Join(", ", vlan.SourceIps.Distinct()),
                            string.Join(", ", vlan.DestinationIps.Distinct()),
                            string.Join(", ", vlan.EthernetTypes.Distinct()),
                            string.Join(", ", vlan.NetworkLayers.Distinct()),
                            string.Join(", ", vlan.Protocols.Distinct()),
                            string.Join(", ", vlan.DestinationPorts.Distinct())
                        });
                    }

                    // NAT Configurations
                    var natHeader = new[] { "NAT Configurations:", "", "", "", "", "" };
                    var natColumns = new[] { "vLAN ID", "Source IP Address", "NAT Translated IP", "Destination IP Address", "Traffic Type/Protocol", "Destination Port" };
                    natRows = new List<string[]>();
                    natRows.Add(natHeader);
                    natRows.Add(natColumns);
                    foreach (var nat in DataModels.natConfigs.Values.OrderBy(n => n.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        natRows.Add(new[]
                        {
                            nat.VLANId,
                            string.Join(", ", nat.SourceIps.Distinct()),
                            nat.NatTranslatedIp,
                            string.Join(", ", nat.DestinationIps.Distinct()),
                            string.Join(", ", nat.Protocols.Distinct()),
                            string.Join(", ", nat.DestinationPorts.Distinct())
                        });
                    }

                    // DHCP Servers in Use
                    var dhcpHeader = new[] { "DHCP Servers in Use:", "", "", "", "", "" };
                    var dhcpColumns = new[] { "vLAN ID", "Interface/Subnet Seen", "DHCP Server IP Address", "DHCP Server MAC Address", "Offered IP Range", "Traffic Type/Protocol" };
                    dhcpRows = new List<string[]>();
                    dhcpRows.Add(dhcpHeader);
                    dhcpRows.Add(dhcpColumns);
                    foreach (var dhcp in DataModels.dhcpServers.Values.OrderBy(d => d.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        dhcpRows.Add(new[]
                        {
                            dhcp.VLANId,
                            string.Join(", ", dhcp.InterfaceSubnets.Distinct()),
                            string.Join(", ", dhcp.DhcpServerIps.Distinct()),
                            string.Join(", ", dhcp.DhcpServerMacs.Distinct()),
                            string.Join(", ", dhcp.OfferedIpRanges.Distinct()),
                            string.Join(", ", dhcp.Protocols.Distinct())
                        });
                    }

                    // DNS Servers in Use
                    var dnsHeader = new[] { "DNS Servers in Use:", "", "", "", "" };
                    var dnsColumns = new[] { "vLAN ID", "Source IP (Client)", "DNS Server IP (Destination)", "FQDN Queried", "Traffic Type/Protocol" };
                    dnsRows = new List<string[]>();
                    dnsRows.Add(dnsHeader);
                    dnsRows.Add(dnsColumns);
                    foreach (var dns in DataModels.dnsServers.Values.OrderBy(d => d.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        dnsRows.Add(new[]
                        {
                            dns.VLANId,
                            string.Join(", ", dns.SourceIps.Distinct()),
                            string.Join(", ", dns.DnsServerIps.Distinct()),
                            string.Join(", ", dns.Fqdns.Distinct()),
                            string.Join(", ", dns.Protocols.Distinct())
                        });
                    }

                    // VPN Tunnels in Use
                    var vpnHeader = new[] { "VPN Tunnels in Use:", "", "", "", "" };
                    var vpnColumns = new[] { "vLAN ID", "Destination IP Address", "Destination Port", "Network Layer", "Traffic Type/Protocol" };
                    vpnRows = new List<string[]>();
                    vpnRows.Add(vpnHeader);
                    vpnRows.Add(vpnColumns);
                    foreach (var vpn in DataModels.vpnTunnels.Values.OrderBy(v => v.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        vpnRows.Add(new[]
                        {
                            vpn.VLANId,
                            string.Join(", ", vpn.DestinationIps.Distinct()),
                            string.Join(", ", vpn.DestinationPorts.Distinct()),
                            string.Join(", ", vpn.NetworkLayers.Distinct()),
                            string.Join(", ", vpn.Protocols.Distinct())
                        });
                    }

                    // Active Directory Controllers / Non-Public DNS Domains
                    var adHeader = new[] { "Active Directory Controllers / Non-Public DNS Domains:", "", "", "", "", "" };
                    var adColumns = new[] { "vLAN ID", "Source IP Address", "Destination IP Address", "SRV Query", "Non-Public Domain Queried", "Traffic Type/Protocol" };
                    adRows = new List<string[]>();
                    adRows.Add(adHeader);
                    adRows.Add(adColumns);
                    foreach (var ad in DataModels.adControllers.Values.OrderBy(a => a.VLANId, StringComparer.OrdinalIgnoreCase))
                    {
                        adRows.Add(new[]
                        {
                            ad.VLANId,
                            string.Join(", ", ad.SourceIps.Distinct()),
                            string.Join(", ", ad.DestinationIps.Distinct()),
                            string.Join(", ", ad.SrvQueries.Select(b => b ? "Yes" : "No").Distinct()),
                            string.Join(", ", ad.NonPublicDomains.Select(b => b ? "Yes" : "No").Distinct()),
                            string.Join(", ", ad.Protocols.Distinct())
                        });
                    }

                    // Routing Protocols Observed
                    var routingHeader = new[] { "Routing Protocols Observed (Listed):" };
                    var routingColumns = new[] { "Routing Protocol Type" };
                    routingRows = new List<string[]>();
                    routingRows.Add(routingHeader);
                    routingRows.Add(routingColumns);
                    var routingProtocols = DataModels.lanConfigs.Values.SelectMany(l => l.Protocols)
                        .Concat(DataModels.wanConfigs.Values.SelectMany(w => w.Protocols))
                        .Concat(DataModels.vlanConfigs.Values.SelectMany(v => v.Protocols))
                        .Concat(DataModels.natConfigs.Values.SelectMany(n => n.Protocols))
                        .Concat(DataModels.dhcpServers.Values.SelectMany(d => d.Protocols))
                        .Concat(DataModels.dnsServers.Values.SelectMany(d => d.Protocols))
                        .Concat(DataModels.vpnTunnels.Values.SelectMany(v => v.Protocols))
                        .Concat(DataModels.adControllers.Values.SelectMany(a => a.Protocols))
                        .Distinct()
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    foreach (var protocol in routingProtocols)
                    {
                        routingRows.Add(new[] { protocol });
                    }

                    var lines = new List<string>();

                    void AddTableRows(List<string[]> tableRows)
                    {
                        foreach (var row in tableRows)
                        {
                            lines.Add(string.Join(",", row.Select(EscapeCell)));
                        }
                        lines.Add("");
                    }

                    AddTableRows(totalNetworkRows);
                    AddTableRows(lanRows);
                    AddTableRows(wanRows);
                    AddTableRows(vlanRows);
                    AddTableRows(natRows);
                    AddTableRows(dhcpRows);
                    AddTableRows(dnsRows);
                    AddTableRows(vpnRows);
                    AddTableRows(adRows);
                    AddTableRows(routingRows);

                    string EscapeCell(string? cell)
                    {
                        if (string.IsNullOrEmpty(cell))
                            return "";
                        if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n"))
                            return $"\"{cell.Replace("\"", "\"\"")}\"";
                        return cell;
                    }

                    try
                    {
                        File.WriteAllLines(csvPath, lines);
                    }
                    catch
                    {
                        // Ignore write errors (file might be locked)
                    }
                }

                await Task.Delay(5000, ct);
            }
            System.Diagnostics.Debug.WriteLine("Write task stopped due to cancellation.");
        }
    }
}
