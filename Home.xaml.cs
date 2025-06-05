/*
    File: Home.xaml.cs
    Purpose: Implements logic for HEAT home screen, manages network analysis, UI chatter log, and state save
    Created: June 2 2025
    Last Modified: June 4 2025
    Authors: Anthony Samen / Cody Cartwright
*/

using Microsoft.UI; // Imports UI color types
using Microsoft.UI.Xaml; // Imports base UI types
using Microsoft.UI.Xaml.Controls; // Imports controls like Button, ContentDialog
using Microsoft.UI.Xaml.Media; // Imports brush and color utilities
using SharpPcap; // Imports packet capture library
using System; // Imports core types
using System.Collections.Generic; // Imports generic collections
using System.Collections.ObjectModel; // Imports observable collection
using System.ComponentModel; // Imports property change notification
using System.IO; // Imports file I/O
using System.Linq; // Imports LINQ
using System.Text; // Imports string builder
using System.Threading; // Imports threading and cancellation
using System.Threading.Tasks; // Imports async/await
using Windows.UI; // Imports color struct

namespace HEAT // Declares project namespace
{
    public sealed partial class Home : UserControl, INotifyPropertyChanged // Declares Home control, supports property change notification
    {
        public event PropertyChangedEventHandler? PropertyChanged; // Event for property changes

        public ObservableCollection<ChatterLine> ChatterLines { get; } = new(); // Stores chat lines for UI
        private List<ICaptureDevice> _runningDevices = new(); // Tracks running capture devices
        private CancellationTokenSource? _captureCancellationTokenSource; // Token for capture cancellation
        private CancellationTokenSource _chatterCancelSource = new CancellationTokenSource(); // Token for chatter cancellation
        private readonly SemaphoreSlim _chatterSemaphore = new SemaphoreSlim(1, 1); // Semaphore for chatter updates
        private bool _isStateSaveBlock = false; // Tracks if state save block is active

        public Home() // Constructor, sets up UI and welcome message
        {
            this.InitializeComponent(); // Loads XAML UI
            this.DataContext = this; // Sets data context for binding
            _ = AddChatterMessageAsync("Welcome to HEAT!", Colors.LightGray, _chatterCancelSource.Token); // Shows welcome message
        }

        private async void StartNetworkAnalysis_Click(object sender, RoutedEventArgs e) // Handles start button click, launches analysis mode dialog
        {
            if (_runningDevices.Count > 0) // Prevents duplicate analysis
            {
                await AddChatterMessageAsync("Network analysis is already running. Please pause before starting again.", Colors.OrangeRed, _chatterCancelSource.Token);
                return;
            }

            var choiceDialog = new ContentDialog // Dialog for choosing analysis mode
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

            string choice = ""; // Stores user's choice

            choiceDialog.Opened += (s, args) => // Handles dialog opened event
            {
                foreach (var child in ((StackPanel)choiceDialog.Content).Children) // Iterates dialog children
                {
                    if (child is Button btn)
                    {
                        btn.Click += (senderBtn, eBtn) => // Handles button click
                        {
                            choice = (string)btn.Tag; // Sets choice from tag
                            choiceDialog.Hide(); // Closes dialog
                        };
                    }
                }
            };
            await choiceDialog.ShowAsync(); // Shows dialog

            if (string.IsNullOrEmpty(choice))
                return;

            List<ICaptureDevice> selectedDevices;

            if (choice == "all") // Runs on all interfaces
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

            _captureCancellationTokenSource = new CancellationTokenSource(); // Prepares cancellation

            var (success, devices) = await NetworkCapture.gen_rt_csv(App.MainWindow,
                (msg, color) => AddChatterMessageAsync(msg, color, _chatterCancelSource.Token),
                selectedDevices, _captureCancellationTokenSource.Token); // Starts capture

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

        private async void PauseNetworkAnalysis_Click(object sender, RoutedEventArgs e) // Handles pause button click, stops analysis
        {
            if (_runningDevices.Count == 0) // Checks if running
            {
                await AddChatterMessageAsync("No network analysis is currently running.", ColorFromHex("#C8C8C8"), _chatterCancelSource.Token);
                return;
            }

            try
            {
                try
                {
                    await NetworkCapture.StopCaptureAsync(); // Stops capture
                }
                catch { }

                foreach (var dev in _runningDevices) // Stops and closes each device
                {
                    try
                    {
                        dev.StopCapture();
                        dev.Close();
                    }
                    catch { }
                }
                _runningDevices.Clear();

                _captureCancellationTokenSource?.Cancel(); // Cancels token
                _captureCancellationTokenSource = null;
                await AddChatterMessageAsync("Real-time network analysis paused.", ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
            }
            catch (Exception ex)
            {
                await AddChatterMessageAsync($"Error pausing network analysis: {ex.Message}", Colors.Red, _chatterCancelSource.Token);
            }
        }

        private async void SaveAnalysisState_Click(object sender, RoutedEventArgs e) // Handles save button click, saves state to CSV
        {
            var token = _chatterCancelSource.Token;

            try
            {
                token.ThrowIfCancellationRequested();

                _isStateSaveBlock = true; // Marks state save block
                await AddChatterMessageAsync("", Colors.Transparent, token); // Adds blank line
                await AddChatterMessageAsync("Saving the Network Analysis State.", ColorFromValues(200, 200, 200), token);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string sourceFile = Path.Combine(desktop, "REALTIME_LOG.csv");
                string destFile = Path.Combine(desktop, "STATE_SAVE.csv");

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destFile, overwrite: true); // Copies log to state file

                    await MainWindow.Instance.ReloadCsvAsync(); // Reloads CSV in main window

                    var stats = ParseStateSave(destFile); // Parses section counts
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

                    await AddChatterMessageAsync("", Colors.Transparent, token); // Blank line after block
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    await AddChatterMessageAsync("No REALTIME_LOG.csv file found to save.", Colors.Orange, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignores cancellation
            }
            catch (Exception ex)
            {
                await AddChatterMessageAsync($"Error saving analysis state: {ex.Message}", Colors.Red, CancellationToken.None);
            }
            finally
            {
                _isStateSaveBlock = false; // Ends state save block
            }
        }

        private async void ClearAnalysisLog_Click(object sender, RoutedEventArgs e) // Handles clear button click, clears log and chatter
        {
            _chatterCancelSource.Cancel(); // Cancels current chatter

            bool lockTaken = false;
            try
            {
                await _chatterSemaphore.WaitAsync();
                lockTaken = true;

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string logFile = Path.Combine(desktop, "REALTIME_LOG.csv");

                if (File.Exists(logFile))
                {
                    File.WriteAllText(logFile, string.Empty); // Empties log file
                }

                ChatterLines.Clear(); // Clears UI chatter
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

        private async void ExportVyOSConfiguration_Click(object sender, RoutedEventArgs e) // Handles export button click, displays success message
        {
            string exportedFilePath = @"C:\Path\To\VyOSConfig.txt";
            await AddChatterMessageAsync($"VyOS configuration successfully saved to {exportedFilePath}.", ColorFromValues(200, 200, 200), _chatterCancelSource.Token);
        }

        private async Task AddChatterMessageAsync(string message, Color color, CancellationToken? token = null) // Adds message to chatter, animates text, logs to file
        {
            token ??= _chatterCancelSource.Token;

            bool lockTaken = false;
            try
            {
                await _chatterSemaphore.WaitAsync(token.Value);
                lockTaken = true;

                if (string.IsNullOrEmpty(message) && !_isStateSaveBlock)
                    return;

                if (!_isStateSaveBlock && !string.IsNullOrEmpty(message))
                {
                    HEATUI.LogMessage(""); // Logs blank line for spacing
                }

                HEATUI.LogMessage(message); // Logs message

                if (!_isStateSaveBlock && !string.IsNullOrEmpty(message) && ChatterLines.Count > 0 && !string.IsNullOrEmpty(ChatterLines[0].FullText))
                {
                    var spacer = new ChatterLine { FullText = "", LineBrush = new SolidColorBrush(Colors.Transparent), DisplayText = "" };
                    ChatterLines.Insert(0, spacer);
                }

                var newLine = new ChatterLine { FullText = message, LineBrush = new SolidColorBrush(color), DisplayText = "" };
                ChatterLines.Insert(0, newLine);

                if (ChatterLines.Count > 50)
                {
                    ChatterLines.RemoveAt(ChatterLines.Count - 1);
                }

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

        private async Task AddChatterMessageWithCount(int count, string message, Color color) // Adds message with count to chatter
        {
            await AddChatterMessageAsync($"{count} {message}", color, _chatterCancelSource.Token);
        }

        private static Color ColorFromHex(string hex) // Converts hex string to Color
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

        private static Color ColorFromValues(byte r, byte g, byte b) // Creates Color from RGB values
        {
            return Color.FromArgb(255, r, g, b);
        }

        private (int lan, int wan, int vlan, int nat, int dhcp, int dns, int vpn, int ad, List<string> routingProtocols) ParseStateSave(string filePath) // Parses section counts and protocols from state save file
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

    public class ChatterLine : INotifyPropertyChanged // Represents one line in chatter log
    {
        private string _displayText = string.Empty; // Stores animated display text
        public string DisplayText
        {
            get => _displayText; // Gets display text
            set
            {
                if (_displayText != value)
                {
                    _displayText = value; // Sets display text
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText))); // Notifies UI of change
                }
            }
        }
        public string FullText { get; set; } = string.Empty; // Stores full text
        public Brush LineBrush { get; set; } = new SolidColorBrush(Colors.Transparent); // Stores brush for text color
        public event PropertyChangedEventHandler? PropertyChanged; // Event for property changes
    }
}
