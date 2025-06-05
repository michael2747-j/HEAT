/*
    File: Dashboard.xaml.cs
    Purpose: Handles logic for HEAT dashboard UI, manages tab switching, data parsing, export, and user actions
    Created: June 2 2025
    Last Modified: June 5 2025
    Author: Anthony Samen
*/

using Microsoft.UI.Xaml; // Imports base UI types for WinUI
using Microsoft.UI.Xaml.Controls; // Imports controls like Button, Grid, ListView
using Microsoft.UI.Xaml.Media; // Imports brush and color utilities
using System.Collections.ObjectModel; // Imports observable collection for data binding
using System.Collections.Generic; // Imports generic collections
using System.Linq; // Imports LINQ for collection queries
using System; // Imports system utilities
using System.IO; // Imports file I/O operations
using System.Text; // Imports string builder
using System.Threading.Tasks; // Imports async/await support

namespace HEAT // Declares namespace for project
{
    public class FlatRow // Declares FlatRow class for table row data
    {
        public string Col0 { get; set; } // Property for column 0 value
        public string Col1 { get; set; } // Property for column 1 value
        public string Col2 { get; set; } // Property for column 2 value
        public string Col3 { get; set; } // Property for column 3 value
        public string Col4 { get; set; } // Property for column 4 value
        public string Col5 { get; set; } // Property for column 5 value
        public string Col6 { get; set; } // Property for column 6 value
        public string Col7 { get; set; } // Property for column 7 value
        public string Col8 { get; set; } // Property for column 8 value
        public string Col9 { get; set; } // Property for column 9 value

        public static FlatRow FromHeaders(List<string> headers) // Static method, takes header list, returns FlatRow with header values
        {
            return new FlatRow
            {
                Col0 = headers.Count > 0 ? headers[0] : "", // Assigns header 0 or empty
                Col1 = headers.Count > 1 ? headers[1] : "", // Assigns header 1 or empty
                Col2 = headers.Count > 2 ? headers[2] : "", // Assigns header 2 or empty
                Col3 = headers.Count > 3 ? headers[3] : "", // Assigns header 3 or empty
                Col4 = headers.Count > 4 ? headers[4] : "", // Assigns header 4 or empty
                Col5 = headers.Count > 5 ? headers[5] : "", // Assigns header 5 or empty
                Col6 = headers.Count > 6 ? headers[6] : "", // Assigns header 6 or empty
                Col7 = headers.Count > 7 ? headers[7] : "", // Assigns header 7 or empty
                Col8 = headers.Count > 8 ? headers[8] : "", // Assigns header 8 or empty
                Col9 = headers.Count > 9 ? headers[9] : ""  // Assigns header 9 or empty
            };
        }

        public static FlatRow FromDictionary(IDictionary<string, string> dict, List<string> headers) // Static method, takes dictionary and header list, returns FlatRow with mapped values
        {
            var row = new FlatRow(); // Instantiates FlatRow
            if (headers.Count > 0) row.Col0 = dict.TryGetValue(headers[0], out var v0) ? v0 : ""; // Maps value for column 0
            if (headers.Count > 1) row.Col1 = dict.TryGetValue(headers[1], out var v1) ? v1 : ""; // Maps value for column 1
            if (headers.Count > 2) row.Col2 = dict.TryGetValue(headers[2], out var v2) ? v2 : ""; // Maps value for column 2
            if (headers.Count > 3) row.Col3 = dict.TryGetValue(headers[3], out var v3) ? v3 : ""; // Maps value for column 3
            if (headers.Count > 4) row.Col4 = dict.TryGetValue(headers[4], out var v4) ? v4 : ""; // Maps value for column 4
            if (headers.Count > 5) row.Col5 = dict.TryGetValue(headers[5], out var v5) ? v5 : ""; // Maps value for column 5
            if (headers.Count > 6) row.Col6 = dict.TryGetValue(headers[6], out var v6) ? v6 : ""; // Maps value for column 6
            if (headers.Count > 7) row.Col7 = dict.TryGetValue(headers[7], out var v7) ? v7 : ""; // Maps value for column 7
            if (headers.Count > 8) row.Col8 = dict.TryGetValue(headers[8], out var v8) ? v8 : ""; // Maps value for column 8
            if (headers.Count > 9) row.Col9 = dict.TryGetValue(headers[9], out var v9) ? v9 : ""; // Maps value for column 9
            return row; // Returns populated FlatRow
        }
    }

    public sealed partial class Dashboard : UserControl // Declares Dashboard class, inherits UserControl, sealed for no further inheritance
    {
        private readonly Dictionary<string, string> TabToSectionTitle = new() // Maps tab names to section titles
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

        private Dictionary<string, (List<string> headers, List<Dictionary<string, string>> rows)> _sections = // Stores parsed CSV data by section
            new Dictionary<string, (List<string>, List<Dictionary<string, string>>)>();

        private List<string> _allColumns = new(); // Stores all column names for current section
        private HashSet<string> _visibleColumns = new(); // Tracks columns currently visible in table

        private string GetDesktopCsvPath() // Returns full path for STATE_SAVE.csv on desktop, takes no arguments, returns string
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Gets desktop folder path
            return Path.Combine(desktopPath, "STATE_SAVE.csv"); // Combines path with file name
        }

        private string GetDesktopExportPath() // Returns full path for export file on desktop, takes no arguments, returns string
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Gets desktop folder path
            return Path.Combine(desktopPath, "STATE_SAVE_EXPORT.csv"); // Combines path with export file name
        }

        public Dashboard() // Constructor for Dashboard, takes no arguments, called on control creation
        {
            this.InitializeComponent(); // Initializes UI components from XAML
            LoadCsvAndInitialize(); // Loads CSV data, sets up dashboard
        }

        private void LoadCsvAndInitialize() // Loads CSV, parses data, sets default tab, takes no arguments, returns void
        {
            string csvPath = GetDesktopCsvPath(); // Gets CSV file path
            if (!File.Exists(csvPath)) // Checks if file exists
            {
                ShowDialog("STATE_SAVE.csv not found on Desktop."); // Shows dialog if file missing
                return; // Exits method
            }

            _sections = ParseStateSaveCsvWithColonHeaders(csvPath); // Parses CSV into sections

            SetSelectedTab(TotalNetworkTab); // Sets default selected tab
            ShowSectionForTab("TotalNetworkTab"); // Shows data for default tab
        }

        private Dictionary<string, (List<string> headers, List<Dictionary<string, string>> rows)> ParseStateSaveCsvWithColonHeaders(string path) // Parses CSV with section headers, returns dictionary of sections
        {
            var result = new Dictionary<string, (List<string>, List<Dictionary<string, string>>)>();
            string[] lines = File.ReadAllLines(path); // Reads all lines from file
            string currentSection = null; // Tracks current section name
            List<string> headers = null; // Tracks current headers
            List<Dictionary<string, string>> rows = null; // Tracks current rows

            for (int idx = 0; idx < lines.Length; idx++) // Iterates over each line
            {
                var line = lines[idx]; // Gets current line
                if (string.IsNullOrWhiteSpace(line)) // Skips empty lines
                    continue;

                string trimmed = line.TrimEnd(); // Removes trailing whitespace
                string trimmedForHeader = trimmed.TrimEnd(','); // Removes trailing commas
                if (trimmedForHeader.EndsWith(":")) // Detects section header
                {
                    if (currentSection != null && headers != null && rows != null) // Saves previous section if present
                        result[currentSection] = (headers, rows);

                    currentSection = trimmedForHeader.TrimEnd(':').Trim(); // Extracts section name
                    headers = null; // Resets headers
                    rows = new List<Dictionary<string, string>>(); // Resets rows
                }
                else if (headers == null) // Detects header line
                {
                    if (rows == null)
                        continue;
                    headers = ParseCsvLine(line); // Parses header columns
                }
                else // Handles data row
                {
                    if (rows == null)
                        continue;
                    var values = ParseCsvLine(line); // Parses row values
                    if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace)) continue; // Skips empty rows
                    var dict = new Dictionary<string, string>(); // Maps header to value
                    for (int i = 0; i < headers.Count && i < values.Count; i++)
                        dict[headers[i]] = values[i];
                    rows.Add(dict); // Adds row to list
                }
            }
            if (currentSection != null && headers != null && rows != null) // Saves last section if present
                result[currentSection] = (headers, rows);

            return result; // Returns all parsed sections
        }

        private List<string> ParseCsvLine(string line) // Parses CSV line into list of values, handles quotes, returns list of strings
        {
            var result = new List<string>(); // Stores parsed values
            if (string.IsNullOrEmpty(line)) // Handles empty line
                return result;

            var sb = new StringBuilder(); // Builds current value
            bool inQuotes = false; // Tracks if inside quoted string
            for (int i = 0; i < line.Length; i++) // Iterates over characters
            {
                char c = line[i]; // Gets character
                if (c == '\"') // Handles quote character
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"') // Handles escaped quote
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes; // Toggles quote state
                    }
                }
                else if (c == ',' && !inQuotes) // Handles delimiter outside quotes
                {
                    result.Add(sb.ToString()); // Adds value to result
                    sb.Clear(); // Resets builder
                }
                else
                {
                    sb.Append(c); // Adds character to builder
                }
            }
            result.Add(sb.ToString()); // Adds last value
            return result; // Returns parsed values
        }

        private void SetSelectedTab(Button selectedButton) // Updates tab styles, takes selected button, returns void
        {
            foreach (var child in TabGrid.Children) // Iterates over tab buttons
            {
                if (child is Button tabBtn) // Checks if child is button
                {
                    tabBtn.Style = (Style)Resources["TabButtonStyle"]; // Sets style to default
                }
            }
            selectedButton.Style = (Style)Resources["SelectedTabButtonStyle"]; // Sets style for selected tab
        }

        private void Subtab_Click(object sender, RoutedEventArgs e) // Handles tab click event, takes sender and event args, returns void
        {
            if (sender is Button btn) // Checks if sender is button
            {
                SetSelectedTab(btn); // Updates tab style
                ShowSectionForTab(btn.Name); // Shows section for clicked tab
            }
        }

        private void ShowSectionForTab(string tabName) // Displays data for tab, takes tab name, returns void
        {
            if (tabName == "AllTablesTab") // Checks if all tables tab
            {
                SingleTableGrid.Visibility = Visibility.Collapsed; // Hides single table grid
                AllTablesScrollViewer.Visibility = Visibility.Visible; // Shows all tables scroll viewer
                RenderAllTablesTab(); // Renders all tables
                return;
            }
            else
            {
                SingleTableGrid.Visibility = Visibility.Visible; // Shows single table grid
                AllTablesScrollViewer.Visibility = Visibility.Collapsed; // Hides all tables scroll viewer
            }

            if (!TabToSectionTitle.TryGetValue(tabName, out string sectionTitle)) // Gets section title for tab
            {
                MainTableListView.ItemsSource = null; // Clears table items
                MainTableListView.Header = null; // Clears table header
                TableTitle.Text = "No data for this tab"; // Shows no data message
                return;
            }

            TableTitle.Text = sectionTitle; // Sets table title

            if (!_sections.TryGetValue(sectionTitle, out var section) || section.headers == null || section.rows == null) // Checks if section exists
            {
                MainTableListView.ItemsSource = null; // Clears items
                MainTableListView.Header = null; // Clears header
                return;
            }

            _allColumns = section.headers; // Updates all columns
            if (_visibleColumns.Count == 0)
                _visibleColumns = new HashSet<string>(_allColumns); // Sets visible columns to all if empty

            var visibleHeaders = _allColumns.Where(c => _visibleColumns.Contains(c)).ToList(); // Gets visible headers

            MainTableListView.Header = FlatRow.FromHeaders(visibleHeaders); // Sets table header

            var flatRows = new ObservableCollection<FlatRow>(
                section.rows.Select(r => FlatRow.FromDictionary(r, visibleHeaders)) // Maps rows to FlatRow
            );
            MainTableListView.ItemsSource = flatRows; // Sets table items
        }

        private void SelectVisibleColumns_Click(object sender, RoutedEventArgs e) // Handles column selection, takes sender and event args, returns void
        {
            var flyout = new Flyout(); // Creates flyout for column toggles
            var stack = new StackPanel(); // Stack for toggle switches

            foreach (var col in _allColumns) // Iterates columns
            {
                var toggle = new ToggleSwitch
                {
                    Header = col, // Sets toggle label
                    IsOn = _visibleColumns.Contains(col), // Sets toggle state
                    Margin = new Thickness(0, 2, 0, 2), // Sets margin
                    HorizontalAlignment = HorizontalAlignment.Stretch // Stretches toggle
                };

                toggle.Toggled += (s, args) => // Handles toggle event
                {
                    if (toggle.IsOn)
                        _visibleColumns.Add(col); // Adds column to visible
                    else if (_visibleColumns.Count > 1)
                        _visibleColumns.Remove(col); // Removes column if more than one remains
                    else
                        toggle.IsOn = true; // Prevents hiding all columns

                    ShowSectionForTab(GetCurrentTabName()); // Updates table

                    foreach (var child in stack.Children) // Updates toggle enabled state
                    {
                        if (child is ToggleSwitch ts)
                            ts.IsEnabled = _visibleColumns.Count > 1 || ts.IsOn;
                    }
                };

                stack.Children.Add(toggle); // Adds toggle to stack
            }

            foreach (var child in stack.Children) // Sets enabled state for toggles
            {
                if (child is ToggleSwitch ts)
                    ts.IsEnabled = _visibleColumns.Count > 1 || ts.IsOn;
            }

            flyout.Content = stack; // Sets flyout content
            flyout.ShowAt((FrameworkElement)sender); // Shows flyout at sender
        }

        private string GetCurrentTabName() // Returns name of currently selected tab, takes no arguments, returns string
        {
            foreach (var child in TabGrid.Children) // Iterates tab buttons
            {
                if (child is Button btn && btn.Style == (Style)Resources["SelectedTabButtonStyle"])
                    return btn.Name; // Returns name of selected tab
            }
            return "TotalNetworkTab"; // Returns default tab if none selected
        }

        private void RenderAllTablesTab() // Renders all tables, takes no arguments, returns void
        {
            if (AllTablesStackPanel == null || _sections == null)
                return;

            AllTablesStackPanel.Children.Clear(); // Clears all tables panel

            foreach (var kv in _sections) // Iterates sections
            {
                var sectionTitle = kv.Key; // Gets section title
                var headers = kv.Value.headers; // Gets headers
                var rows = kv.Value.rows; // Gets rows

                if (headers == null || headers.Count == 0)
                    continue;

                var titleBlock = new TextBlock
                {
                    Text = sectionTitle ?? "", // Sets section title
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold, // Bold text
                    FontSize = 20, // Large font
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), // White text
                    Margin = new Thickness(0, 24, 0, 8)
                };
                AllTablesStackPanel.Children.Add(titleBlock); // Adds title to panel

                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) }; // Grid for headers
                for (int i = 0; i < headers.Count; i++)
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Adds columns

                for (int i = 0; i < headers.Count; i++)
                {
                    var tb = new TextBlock
                    {
                        Text = headers[i] ?? "", // Sets header text
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), // White text
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold, // Bold font
                        Margin = new Thickness(12, 8, 12, 8),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    ToolTipService.SetToolTip(tb, headers[i] ?? ""); // Sets tooltip
                    Grid.SetColumn(tb, i); // Sets grid column
                    headerGrid.Children.Add(tb); // Adds to grid
                }
                AllTablesStackPanel.Children.Add(headerGrid); // Adds header grid to panel

                if (rows != null)
                {
                    foreach (var row in rows) // Iterates data rows
                    {
                        if (row == null)
                            continue;
                        var dataGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) }; // Grid for row
                        for (int i = 0; i < headers.Count; i++)
                            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Adds columns

                        for (int i = 0; i < headers.Count; i++)
                        {
                            var val = (row != null && row.TryGetValue(headers[i], out var v) && v != null) ? v : ""; // Gets value
                            var tb = new TextBlock
                            {
                                Text = val, // Sets text
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), // White text
                                Margin = new Thickness(12, 8, 12, 8),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            ToolTipService.SetToolTip(tb, val); // Sets tooltip
                            Grid.SetColumn(tb, i); // Sets column
                            dataGrid.Children.Add(tb); // Adds to grid
                        }
                        AllTablesStackPanel.Children.Add(dataGrid); // Adds row grid to panel
                    }
                }
            }
        }

        private async void ExportVyOS_Click(object sender, RoutedEventArgs e) // Handles export button click, copies CSV, shows dialog, returns void
        {
            try
            {
                string csvPath = GetDesktopCsvPath(); // Gets source path
                string exportPath = GetDesktopExportPath(); // Gets export path
                File.Copy(csvPath, exportPath, overwrite: true); // Copies file
                await ShowDialog("VyOS configuration exported successfully to Desktop as STATE_SAVE_EXPORT.csv."); // Shows success dialog
            }
            catch (Exception ex)
            {
                await ShowDialog($"Error exporting VyOS configuration: {ex.Message}"); // Shows error dialog
            }
        }

        private async void DeleteSaveState_Click(object sender, RoutedEventArgs e) // Handles delete button click, deletes CSV, resets UI, returns void
        {
            try
            {
                string csvPath = GetDesktopCsvPath(); // Gets CSV path
                if (File.Exists(csvPath))
                {
                    File.Delete(csvPath); // Deletes file
                }
                await ShowDialog("STATE_SAVE.csv deleted from Desktop."); // Shows deleted dialog
                _sections.Clear(); // Clears data
                MainTableListView.ItemsSource = null; // Clears table
                MainTableListView.Header = null; // Clears header
                AllTablesStackPanel.Children.Clear(); // Clears all tables
            }
            catch (Exception ex)
            {
                await ShowDialog($"Error deleting STATE_SAVE.csv: {ex.Message}"); // Shows error dialog
            }
        }

        private async Task ShowDialog(string message) // Shows dialog with message, takes string, returns Task
        {
            var dialog = new ContentDialog
            {
                Title = "Dashboard", // Sets dialog title
                Content = message, // Sets dialog content
                CloseButtonText = "OK", // Sets close button text
                XamlRoot = this.XamlRoot // Sets XAML root for dialog placement
            };
            await dialog.ShowAsync(); // Displays dialog asynchronously
        }
    }
}
