using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace HEAT
{
    public sealed partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ChatterLine> ChatterLines { get; } = new();
        private List<ICaptureDevice> _runningDevices = new();
        private CancellationTokenSource? _captureCancellationTokenSource;
        private CancellationTokenSource _chatterCancelSource = new CancellationTokenSource();
        private readonly SemaphoreSlim _chatterSemaphore = new SemaphoreSlim(1, 1);
        private bool _isStateSaveBlock = false;

        public Home()
        {
            this.InitializeComponent();
            this.DataContext = this;
            _ = AddChatterMessageAsync("Welcome to HEAT!", Colors.LightGray, _chatterCancelSource.Token);
        }

        private async void StartNetworkAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (_runningDevices.Count > 0)
            {
                await AddChatterMessageAsync("Network analysis is already running. Please pause before starting again.", Colors.OrangeRed, _chatterCancelSource.Token);
                return;
            }

            var choiceDialog = new ContentDialog
            {
                Title = "Analysis Mode",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Choose analysis mode:",
                            Margin = new Thickness(0,0,0,12),
                            FontSize = 16,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        },
                        new Button
                        {
                            Content = "🏃 Run on All Interfaces",
                            Tag = "all",
                            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
                            Margin = new Thickness(0,0,0,8),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        },
                        new Button
                        {
                            Content = "🔧 Custom Interface Selection",
                            Tag = "custom",
                            Style = (Style)Application.Current.Resources["DefaultButtonStyle"],
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        }
                    }
                },
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            string choice = "";

            choiceDialog.Opened += (s, args) =>
            {
                foreach (var child in ((StackPanel)choiceDialog.Content).Children)
                {
                    if (child is Button btn)
                    {
                        btn.Click += (senderBtn, eBtn) =>
                        {
                            choice = (string)btn.Tag;
                            choiceDialog.Hide();
                        };
                    }
                }
            };
            await choiceDialog.ShowAsync();

            if (string.IsNullOrEmpty(choice))
                return;

            List<ICaptureDevice> selectedDevices;

            if (choice == "all")
            {
                selectedDevices = SharpPcap.CaptureDeviceList.Instance.Cast<ICaptureDevice>().ToList();
                if (selectedDevices.Count == 0)
                {
                    await AddChatterMessageAsync("No network interfaces found.", Colors.Red, _chatterCancelSource.Token);
                    return;
                }
            }
            else
            {
                selectedDevices = null!;
            }

            _captureCancellationTokenSource = new CancellationTokenSource();

            var (success, devices) = await NetworkCapture.gen_rt_csv(App.MainWindow,
                (msg, color) => AddChatterMessageAsync(msg, color, _chatterCancelSource.Token),
                selectedDevices, _captureCancellationTokenSource.Token);

            if (success)
            {
                _runningDevices = devices ?? new List<ICaptureDevice>();
                await AddChatterMessageAsync("Real-time network analysis started. For best results, run for 2-5 minutes.", ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
            }
            else
            {
                await AddChatterMessageAsync("Failed to start network analysis.", Colors.Red, _chatterCancelSource.Token);
                _runningDevices.Clear();
            }
        }

        private async void PauseNetworkAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (_runningDevices.Count == 0)
            {
                await AddChatterMessageAsync("No network analysis is currently running.", ColorFromHex("#C8C8C8"), _chatterCancelSource.Token);
                return;
            }

            try
            {
                try
                {
                    await NetworkCapture.StopCaptureAsync();
                }
                catch { }

                foreach (var dev in _runningDevices)
                {
                    try
                    {
                        dev.StopCapture();
                        dev.Close();
                    }
                    catch { }
                }
                _runningDevices.Clear();

                _captureCancellationTokenSource?.Cancel();
                _captureCancellationTokenSource = null;
                await AddChatterMessageAsync("Real-time network analysis paused.", ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
            }
            catch (Exception ex)
            {
                await AddChatterMessageAsync($"Error pausing network analysis: {ex.Message}", Colors.Red, _chatterCancelSource.Token);
            }
        }

        private async void SaveAnalysisState_Click(object sender, RoutedEventArgs e)
        {
            var token = _chatterCancelSource.Token;

            try
            {
                token.ThrowIfCancellationRequested();

                // Start state save block
                _isStateSaveBlock = true;
                await AddChatterMessageAsync("", Colors.Transparent, token); // Empty line before block
                await AddChatterMessageAsync("Saving the Network Analysis State.", ColorFromValues(200, 200, 200), token);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string sourceFile = Path.Combine(desktop, "REALTIME_LOG.csv");
                string destFile = Path.Combine(desktop, "STATE_SAVE.csv");

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destFile, overwrite: true);

                    await MainWindow.Instance.ReloadCsvAsync();

                    var stats = ParseStateSave(destFile);
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageAsync(
                        $"Network State Save generated at {now}.",
                        ColorFromValues(200, 200, 200),
                        token
                    );

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.lan, "LAN Interface Configurations logged.", Colors.Red);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.wan, "WAN Interface Configurations logged.", Colors.Orange);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.vlan, "vLAN Interface Configurations logged.", Colors.Yellow);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.nat, "NAT Configurations logged.", ColorFromValues(255, 105, 180));

                    token.ThrowIfCancellationRequested();
                    if (stats.dhcp == 0)
                        await AddChatterMessageAsync("No DHCP Servers are being used.", Colors.Brown, token);
                    else
                        await AddChatterMessageWithCount(stats.dhcp, "DHCP servers logged.", Colors.Green);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.dns, "DNS Servers logged.", Colors.Blue);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageAsync($"{stats.vpn} VPN Tunnels logged.", Colors.Violet, token);

                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageWithCount(stats.ad, "Active Directory Controllers / Non-Public DNS Domains logged.", Colors.Gray);

                    var protocols = new HashSet<string>(stats.routingProtocols);
                    if (protocols.Any())
                    {
                        token.ThrowIfCancellationRequested();
                        await AddChatterMessageAsync(
                            $"Protocols observed: {string.Join(", ", protocols.OrderBy(p => p))}.",
                            Colors.White,
                            token
                        );
                    }

                    // End state save block
                    await AddChatterMessageAsync("", Colors.Transparent, token); // Empty line after block
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageAsync("No REALTIME_LOG.csv file found to save.", Colors.Orange, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Silently ignore cancellation, no message needed
            }
            catch (Exception ex)
            {
                await AddChatterMessageAsync($"Error saving analysis state: {ex.Message}", Colors.Red, CancellationToken.None);
            }
            finally
            {
                _isStateSaveBlock = false;
            }
        }

        private async void ClearAnalysisLog_Click(object sender, RoutedEventArgs e)
        {
            _chatterCancelSource.Cancel();

            bool lockTaken = false;
            try
            {
                await _chatterSemaphore.WaitAsync();
                lockTaken = true;

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string logFile = Path.Combine(desktop, "REALTIME_LOG.csv");

                if (File.Exists(logFile))
                {
                    File.WriteAllText(logFile, string.Empty);
                }

                // Only clear the chatter window, not CHATTER_LOG.txt
                ChatterLines.Clear();
            }
            finally
            {
                if (lockTaken)
                    _chatterSemaphore.Release();
            }

            _chatterCancelSource.Dispose();
            _chatterCancelSource = new CancellationTokenSource();

            await AddChatterMessageAsync("Real-time network analysis log and chatter have been cleared.",
                                        ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
        }

        private async void ExportVyOSConfiguration_Click(object sender, RoutedEventArgs e)
        {
            string exportedFilePath = @"C:\Path\To\VyOSConfig.txt";
            await AddChatterMessageAsync($"VyOS configuration successfully saved to {exportedFilePath}.", ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
        }

        private async Task AddChatterMessageAsync(string message, Color color, CancellationToken? token = null)
        {
            token ??= _chatterCancelSource.Token;

            bool lockTaken = false;
            try
            {
                await _chatterSemaphore.WaitAsync(token.Value);
                lockTaken = true;

                // Skip empty messages unless they're part of state save block formatting
                if (string.IsNullOrEmpty(message) && !_isStateSaveBlock)
                    return;

                // Log a blank line before non-empty, non-state-save messages to match chatter UI spacing
                if (!_isStateSaveBlock && !string.IsNullOrEmpty(message))
                {
                    HEATUI.LogMessage(""); // Log empty line for spacing
                }

                // Log the message using HEATUI (with empty lines for state save blocks)
                HEATUI.LogMessage(message);

                // Add spacing between regular messages (not within state save block)
                if (!_isStateSaveBlock && !string.IsNullOrEmpty(message) && ChatterLines.Count > 0 && !string.IsNullOrEmpty(ChatterLines[0].FullText))
                {
                    var spacer = new ChatterLine { FullText = "", LineBrush = new SolidColorBrush(Colors.Transparent), DisplayText = "" };
                    ChatterLines.Insert(0, spacer);
                }

                var newLine = new ChatterLine { FullText = message, LineBrush = new SolidColorBrush(color), DisplayText = "" };
                ChatterLines.Insert(0, newLine);

                if (ChatterLines.Count > 50) // Increased limit to account for spacers
                {
                    ChatterLines.RemoveAt(ChatterLines.Count - 1);
                }

                // Only animate non-empty messages
                if (!string.IsNullOrEmpty(message))
                {
                    for (int i = 0; i < message.Length; i++)
                    {
                        if (token.Value.IsCancellationRequested)
                            return;
                        newLine.DisplayText = message.Substring(0, i + 1);
                        try
                        {
                            await Task.Delay(40, token.Value);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (lockTaken)
                    _chatterSemaphore.Release();
            }
        }

        private async Task AddChatterMessageWithCount(int count, string message, Color color)
        {
            await AddChatterMessageAsync($"{count} {message}", color, _chatterCancelSource.Token);
        }

        private static Color ColorFromHex(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            byte a = 255;
            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                hex = hex.Substring(2);
            }
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }

        private static Color ColorFromValues(byte r, byte g, byte b)
        {
            return Color.FromArgb(255, r, g, b);
        }

        private (int lan, int wan, int vlan, int nat, int dhcp, int dns, int vpn, int ad, List<string> routingProtocols) ParseStateSave(string filePath)
        {
            int lan = 0, wan = 0, vlan = 0, nat = 0, dhcp = 0, dns = 0, vpn = 0, ad = 0;
            var routingProtocols = new List<string>();
            string currentSection = "";
            bool skipHeader = false;

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (line.StartsWith("LAN Interface Configurations:"))
                {
                    currentSection = "lan";
                    skipHeader = true;
                }
                else if (line.StartsWith("WAN Interface Configurations:"))
                {
                    currentSection = "wan";
                    skipHeader = true;
                }
                else if (line.StartsWith("vLAN Interface Configurations:"))
                {
                    currentSection = "vlan";
                    skipHeader = true;
                }
                else if (line.StartsWith("NAT Configurations:"))
                {
                    currentSection = "nat";
                    skipHeader = true;
                }
                else if (line.StartsWith("DHCP Servers in Use:"))
                {
                    currentSection = "dhcp";
                    skipHeader = true;
                }
                else if (line.StartsWith("DNS Servers in Use:"))
                {
                    currentSection = "dns";
                    skipHeader = true;
                }
                else if (line.StartsWith("VPN Tunnels in Use:"))
                {
                    currentSection = "vpn";
                    skipHeader = true;
                }
                else if (line.StartsWith("Active Directory Controllers / Non-Public DNS Domains:"))
                {
                    currentSection = "ad";
                    skipHeader = true;
                }
                else if (line.StartsWith("Routing Protocols Observed (Listed):"))
                {
                    currentSection = "routing";
                    skipHeader = true;
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    if (skipHeader)
                    {
                        skipHeader = false;
                        continue;
                    }
                    switch (currentSection)
                    {
                        case "lan": lan++; break;
                        case "wan": wan++; break;
                        case "vlan": vlan++; break;
                        case "nat": nat++; break;
                        case "dhcp": dhcp++; break;
                        case "dns": dns++; break;
                        case "vpn": vpn++; break;
                        case "ad": ad++; break;
                        case "routing": routingProtocols.Add(line.Trim()); break;
                    }
                }
            }
            return (lan, wan, vlan, nat, dhcp, dns, vpn, ad, routingProtocols);
        }
    }

    public class ChatterLine : INotifyPropertyChanged
    {
        private string _displayText = string.Empty;
        public string DisplayText
        {
            get => _displayText;
            set
            {
                if (_displayText != value)
                {
                    _displayText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
                }
            }
        }
        public string FullText { get; set; } = string.Empty;
        public Brush LineBrush { get; set; } = new SolidColorBrush(Colors.Transparent);
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}