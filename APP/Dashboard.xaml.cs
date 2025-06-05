using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HEAT
{
    public class FlatRow
    {
        public string Col0 { get; set; }
        public string Col1 { get; set; }
        public string Col2 { get; set; }
        public string Col3 { get; set; }
        public string Col4 { get; set; }
        public string Col5 { get; set; }
        public string Col6 { get; set; }
        public string Col7 { get; set; }
        public string Col8 { get; set; }
        public string Col9 { get; set; }

        public static FlatRow FromHeaders(List<string> headers)
        {
            return new FlatRow
            {
                Col0 = headers.Count > 0 ? headers[0] : "",
                Col1 = headers.Count > 1 ? headers[1] : "",
                Col2 = headers.Count > 2 ? headers[2] : "",
                Col3 = headers.Count > 3 ? headers[3] : "",
                Col4 = headers.Count > 4 ? headers[4] : "",
                Col5 = headers.Count > 5 ? headers[5] : "",
                Col6 = headers.Count > 6 ? headers[6] : "",
                Col7 = headers.Count > 7 ? headers[7] : "",
                Col8 = headers.Count > 8 ? headers[8] : "",
                Col9 = headers.Count > 9 ? headers[9] : ""
            };
        }

        public static FlatRow FromDictionary(IDictionary<string, string> dict, List<string> headers)
        {
            var row = new FlatRow();
            if (headers.Count > 0) row.Col0 = dict.TryGetValue(headers[0], out var v0) ? v0 : "";
            if (headers.Count > 1) row.Col1 = dict.TryGetValue(headers[1], out var v1) ? v1 : "";
            if (headers.Count > 2) row.Col2 = dict.TryGetValue(headers[2], out var v2) ? v2 : "";
            if (headers.Count > 3) row.Col3 = dict.TryGetValue(headers[3], out var v3) ? v3 : "";
            if (headers.Count > 4) row.Col4 = dict.TryGetValue(headers[4], out var v4) ? v4 : "";
            if (headers.Count > 5) row.Col5 = dict.TryGetValue(headers[5], out var v5) ? v5 : "";
            if (headers.Count > 6) row.Col6 = dict.TryGetValue(headers[6], out var v6) ? v6 : "";
            if (headers.Count > 7) row.Col7 = dict.TryGetValue(headers[7], out var v7) ? v7 : "";
            if (headers.Count > 8) row.Col8 = dict.TryGetValue(headers[8], out var v8) ? v8 : "";
            if (headers.Count > 9) row.Col9 = dict.TryGetValue(headers[9], out var v9) ? v9 : "";
            return row;
        }
    }

    public sealed partial class Dashboard : UserControl
    {
        private readonly Dictionary<string, string> TabToSectionTitle = new()
        {
            { "TotalNetworkTab", "Total Network" },
            { "LanTab", "LAN Interface Configurations" },
            { "WanTab", "WAN Interface Configurations" },
            { "VlanTab", "vLAN Interface Configurations" },
            { "NatTab", "NAT Configurations" },
            { "DhcpTab", "DHCP Servers in Use" },
            { "DnsTab", "DNS Servers in Use" },
            { "VpnTab", "VPN Tunnels in Use" },
            { "AdTab", "Active Directory Controllers / Non-Public DNS Domains" },
            { "RoutingTab", "Routing Protocols Observed (Listed)" },
            { "AllTablesTab", "All Tables" }
        };

        private Dictionary<string, (List<string> headers, List<Dictionary<string, string>> rows)> _sections =
            new Dictionary<string, (List<string>, List<Dictionary<string, string>>)>();

        private List<string> _allColumns = new();
        private HashSet<string> _visibleColumns = new();

        private string GetDesktopCsvPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return Path.Combine(desktopPath, "STATE_SAVE.csv");
        }

        private string GetDesktopExportPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return Path.Combine(desktopPath, "STATE_SAVE_EXPORT.csv");
        }

        public Dashboard()
        {
            this.InitializeComponent();
            LoadCsvAndInitialize();
        }

        private void LoadCsvAndInitialize()
        {
            string csvPath = GetDesktopCsvPath();
            if (!File.Exists(csvPath))
            {
                ShowDialog("STATE_SAVE.csv not found on Desktop.");
                return;
            }

            _sections = ParseStateSaveCsvWithColonHeaders(csvPath);

            SetSelectedTab(TotalNetworkTab);
            ShowSectionForTab("TotalNetworkTab");
        }

        private Dictionary<string, (List<string> headers, List<Dictionary<string, string>> rows)> ParseStateSaveCsvWithColonHeaders(string path)
        {
            var result = new Dictionary<string, (List<string>, List<Dictionary<string, string>>)>();
            string[] lines = File.ReadAllLines(path);
            string currentSection = null;
            List<string> headers = null;
            List<Dictionary<string, string>> rows = null;

            for (int idx = 0; idx < lines.Length; idx++)
            {
                var line = lines[idx];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmed = line.TrimEnd();
                string trimmedForHeader = trimmed.TrimEnd(',');
                if (trimmedForHeader.EndsWith(":"))
                {
                    if (currentSection != null && headers != null && rows != null)
                        result[currentSection] = (headers, rows);

                    currentSection = trimmedForHeader.TrimEnd(':').Trim();
                    headers = null;
                    rows = new List<Dictionary<string, string>>();
                }
                else if (headers == null)
                {
                    if (rows == null)
                        continue;
                    headers = ParseCsvLine(line);
                }
                else
                {
                    if (rows == null)
                        continue;
                    var values = ParseCsvLine(line);
                    if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace)) continue;
                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < headers.Count && i < values.Count; i++)
                        dict[headers[i]] = values[i];
                    rows.Add(dict);
                }
            }
            if (currentSection != null && headers != null && rows != null)
                result[currentSection] = (headers, rows);

            return result;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
                return result;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private void SetSelectedTab(Button selectedButton)
        {
            foreach (var child in TabGrid.Children)
            {
                if (child is Button tabBtn)
                {
                    tabBtn.Style = (Style)Resources["TabButtonStyle"];
                }
            }
            selectedButton.Style = (Style)Resources["SelectedTabButtonStyle"];
        }

        private void Subtab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetSelectedTab(btn);
                ShowSectionForTab(btn.Name);
            }
        }

        private void ShowSectionForTab(string tabName)
        {
            if (tabName == "AllTablesTab")
            {
                SingleTableGrid.Visibility = Visibility.Collapsed;
                AllTablesScrollViewer.Visibility = Visibility.Visible;
                RenderAllTablesTab();
                return;
            }
            else
            {
                SingleTableGrid.Visibility = Visibility.Visible;
                AllTablesScrollViewer.Visibility = Visibility.Collapsed;
            }

            if (!TabToSectionTitle.TryGetValue(tabName, out string sectionTitle))
            {
                MainTableListView.ItemsSource = null;
                MainTableListView.Header = null;
                TableTitle.Text = "No data for this tab";
                return;
            }

            TableTitle.Text = sectionTitle;

            if (!_sections.TryGetValue(sectionTitle, out var section) || section.headers == null || section.rows == null)
            {
                MainTableListView.ItemsSource = null;
                MainTableListView.Header = null;
                return;
            }

            _allColumns = section.headers;
            if (_visibleColumns.Count == 0)
                _visibleColumns = new HashSet<string>(_allColumns);

            var visibleHeaders = _allColumns.Where(c => _visibleColumns.Contains(c)).ToList();

            MainTableListView.Header = FlatRow.FromHeaders(visibleHeaders);

            var flatRows = new ObservableCollection<FlatRow>(
                section.rows.Select(r => FlatRow.FromDictionary(r, visibleHeaders))
            );
            MainTableListView.ItemsSource = flatRows;
        }

        private void SelectVisibleColumns_Click(object sender, RoutedEventArgs e)
        {
            var flyout = new Flyout();
            var stack = new StackPanel();

            foreach (var col in _allColumns)
            {
                var toggle = new ToggleSwitch
                {
                    Header = col,
                    IsOn = _visibleColumns.Contains(col),
                    Margin = new Thickness(0, 2, 0, 2),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                toggle.Toggled += (s, args) =>
                {
                    if (toggle.IsOn)
                        _visibleColumns.Add(col);
                    else if (_visibleColumns.Count > 1)
                        _visibleColumns.Remove(col);
                    else
                        toggle.IsOn = true;

                    ShowSectionForTab(GetCurrentTabName());

                    foreach (var child in stack.Children)
                    {
                        if (child is ToggleSwitch ts)
                            ts.IsEnabled = _visibleColumns.Count > 1 || ts.IsOn;
                    }
                };

                stack.Children.Add(toggle);
            }

            foreach (var child in stack.Children)
            {
                if (child is ToggleSwitch ts)
                    ts.IsEnabled = _visibleColumns.Count > 1 || ts.IsOn;
            }

            flyout.Content = stack;
            flyout.ShowAt((FrameworkElement)sender);
        }

        private string GetCurrentTabName()
        {
            foreach (var child in TabGrid.Children)
            {
                if (child is Button btn && btn.Style == (Style)Resources["SelectedTabButtonStyle"])
                    return btn.Name;
            }
            return "TotalNetworkTab";
        }

        private void RenderAllTablesTab()
        {
            if (AllTablesStackPanel == null || _sections == null)
                return;

            AllTablesStackPanel.Children.Clear();

            foreach (var kv in _sections)
            {
                var sectionTitle = kv.Key;
                var headers = kv.Value.headers;
                var rows = kv.Value.rows;

                if (headers == null || headers.Count == 0)
                    continue;

                var titleBlock = new TextBlock
                {
                    Text = sectionTitle ?? "",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 20,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                    Margin = new Thickness(0, 24, 0, 8)
                };
                AllTablesStackPanel.Children.Add(titleBlock);

                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
                for (int i = 0; i < headers.Count; i++)
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

                for (int i = 0; i < headers.Count; i++)
                {
                    var tb = new TextBlock
                    {
                        Text = headers[i] ?? "",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Margin = new Thickness(12, 8, 12, 8),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    ToolTipService.SetToolTip(tb, headers[i] ?? "");
                    Grid.SetColumn(tb, i);
                    headerGrid.Children.Add(tb);
                }
                AllTablesStackPanel.Children.Add(headerGrid);

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        if (row == null)
                            continue;
                        var dataGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
                        for (int i = 0; i < headers.Count; i++)
                            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

                        for (int i = 0; i < headers.Count; i++)
                        {
                            var val = (row != null && row.TryGetValue(headers[i], out var v) && v != null) ? v : "";
                            var tb = new TextBlock
                            {
                                Text = val,
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                                Margin = new Thickness(12, 8, 12, 8),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            ToolTipService.SetToolTip(tb, val);
                            Grid.SetColumn(tb, i);
                            dataGrid.Children.Add(tb);
                        }
                        AllTablesStackPanel.Children.Add(dataGrid);
                    }
                }
            }
        }

        private async void ExportVyOS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string csvPath = GetDesktopCsvPath();
                string exportPath = GetDesktopExportPath();
                File.Copy(csvPath, exportPath, overwrite: true);
                await ShowDialog("VyOS configuration exported successfully to Desktop as STATE_SAVE_EXPORT.csv.");
            }
            catch (Exception ex)
            {
                await ShowDialog($"Error exporting VyOS configuration: {ex.Message}");
            }
        }

        private async void DeleteSaveState_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string csvPath = GetDesktopCsvPath();
                if (File.Exists(csvPath))
                {
                    File.Delete(csvPath);
                }
                await ShowDialog("STATE_SAVE.csv deleted from Desktop.");
                _sections.Clear();
                MainTableListView.ItemsSource = null;
                MainTableListView.Header = null;
                AllTablesStackPanel.Children.Clear();
            }
            catch (Exception ex)
            {
                await ShowDialog($"Error deleting STATE_SAVE.csv: {ex.Message}");
            }
        }

        private async Task ShowDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Dashboard",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
