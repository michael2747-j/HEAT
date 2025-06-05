/*
    File: MainWindow.xaml.cs
    Purpose: Provides main window logic for HEAT app, including tab navigation, CSV parsing, and notification handling
    Created: June 2 2025
    Last Modified: June 4 2025
    Authors: Anthony Samen / Cody Cartwright
*/

using Microsoft.UI.Xaml; // Imports base UI types
using Microsoft.UI.Xaml.Controls; // Imports controls like Button, ContentDialog
using Microsoft.UI.Xaml.Media; // Imports brush/color utilities
using System.IO; // Imports file operations
using System.Collections.Generic; // Imports generic collections
using System.Diagnostics; // Imports process and debug tools
using System.Text; // Imports string builder/encoding
using System.Threading.Tasks; // Imports async/await support
using System.Linq; // Imports LINQ
using System; // Imports core types

namespace HEAT // Declares project namespace
{
    public sealed partial class MainWindow : Window // Declares MainWindow, inherits Window
    {
        private const string CsvPath = @"C:\Users\antho\Desktop\STATE_SAVE.csv"; // Sets path for CSV file

        public Dictionary<string, (string TableTitle, List<string> Headers, List<IDictionary<string, string>> Rows)> CsvSectionsByTitle { get; private set; } = new(); // Stores parsed CSV sections
        public bool CsvParsed { get; private set; } = false; // Tracks if CSV has been parsed

        public static MainWindow? Instance { get; private set; } // Holds singleton instance reference

        public event Action? CsvCacheReloaded; // Event for notifying CSV reload

        public MainWindow() // Constructor, initializes window and UI
        {
            Instance = this; // Sets singleton instance
            this.InitializeComponent(); // Loads XAML UI
            MaximizeOnStartup(); // Maximizes window on launch
            run_ui(); // Sets initial tab
            _ = LoadCsvAsync(); // Loads CSV data asynchronously

            HEATUI.InitializeNotificationUI(NotificationList, NotificationFlyout, NotificationBell); // Sets up notification UI
        }

        private void MaximizeOnStartup() // Maximizes window on startup
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // Gets window handle
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd); // Gets window ID
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId); // Gets app window object
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) // Checks presenter type
                {
                    presenter.Maximize(); // Maximizes window
                }
            }
            catch { }
        }

        private void Tab_Click(object sender, RoutedEventArgs e) // Handles tab button click, sets active tab
        {
            if (sender is Button btn)
            {
                SetActiveTab(btn.Name); // Sets tab by name
            }
        }

        private void SetActiveTab(string tabName) // Sets tab styles and content based on tab name
        {
            HomeTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            DashboardTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            LinksTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            UpgradesTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];

            switch (tabName)
            {
                case "HomeTab":
                    HomeTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Home(); // Loads Home control
                    break;
                case "DashboardTab":
                    DashboardTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Dashboard(); // Loads Dashboard control
                    break;
                case "LinksTab":
                    LinksTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Links(); // Loads Links control
                    break;
                case "UpgradesTab":
                    UpgradesTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Upgrades(); // Loads Upgrades control
                    break;
            }
        }

        void run_ui() { SetActiveTab("HomeTab"); } // Sets Home tab as default

        public async Task LoadCsvAsync() // Loads and parses CSV asynchronously, updates UI on completion
        {
            var parsedSections = await Task.Run(() => ParseCsvSectionsOptimized()); // Parses CSV in background
            DispatcherQueue.TryEnqueue(() =>
            {
                CsvSectionsByTitle = parsedSections; // Updates section cache
                CsvParsed = true; // Marks CSV as parsed
                CsvCacheReloaded?.Invoke(); // Triggers reload event
            });
        }

        private Dictionary<string, (string TableTitle, List<string> Headers, List<IDictionary<string, string>> Rows)> ParseCsvSectionsOptimized() // Parses CSV file into sections, returns dictionary
        {
            var result = new Dictionary<string, (string, List<string>, List<IDictionary<string, string>>)>();

            if (!File.Exists(CsvPath))
                return result;

            var lines = File.ReadAllLines(CsvPath, Encoding.UTF8); // Reads all lines from CSV
            int i = 0;
            int totalLines = lines.Length;

            while (i < totalLines)
            {
                while (i < totalLines && string.IsNullOrWhiteSpace(lines[i]))
                    i++;
                if (i >= totalLines)
                    break;

                string tableTitleRaw = lines[i].Split(',')[0].Trim(); // Gets raw table title
                string tableTitle = tableTitleRaw.TrimEnd(':'); // Removes colon from end
                i++;

                while (i < totalLines && string.IsNullOrWhiteSpace(lines[i]))
                    i++;
                if (i >= totalLines)
                    break;

                var header = SplitCsvLineFast(lines[i]); // Parses header line
                i++;

                if (tableTitle == "Total Network") // Handles special case for total network
                {
                    int previewIdx = i;
                    while (previewIdx < totalLines && string.IsNullOrWhiteSpace(lines[previewIdx]))
                        previewIdx++;
                    if (previewIdx < totalLines)
                    {
                        int pipeCount = lines[previewIdx].Split(',')[0].Count(c => c == '|'); // Counts pipe separators
                        if (pipeCount > 0)
                        {
                            var firstHeader = header[0];
                            header.RemoveAt(0);
                            for (int k = 0; k <= pipeCount; k++)
                                header.Insert(k, $"{firstHeader} {k + 1}"); // Expands header columns
                        }
                    }
                }

                var rows = new List<IDictionary<string, string>>(128); // Prepares row list
                while (i < totalLines && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    var values = SplitCsvLineFast(lines[i]); // Parses row values
                    if (tableTitle == "Total Network" && values.Count > 0)
                    {
                        var firstField = values[0];
                        var splitFirst = firstField.Split('|');
                        if (splitFirst.Length > 1)
                        {
                            values.RemoveAt(0);
                            for (int k = splitFirst.Length - 1; k >= 0; k--)
                                values.Insert(0, splitFirst[k]); // Expands row columns
                        }
                    }
                    while (values.Count < header.Count)
                        values.Add(""); // Pads missing columns
                    while (values.Count > header.Count)
                        values.RemoveAt(values.Count - 1); // Trims extra columns

                    var dict = new Dictionary<string, string>(header.Count); // Maps headers to values
                    for (int j = 0; j < header.Count; j++)
                        dict[header[j]] = values[j];

                    rows.Add(dict); // Adds row to list
                    i++;
                }
                result[tableTitle] = (tableTitleRaw, header, rows); // Stores section
            }
            return result;
        }

        private static List<string> SplitCsvLineFast(string line) // Splits CSV line by commas, returns list of values
        {
            var result = new List<string>();
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',')
                {
                    result.Add(line.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(line.Substring(start));
            return result;
        }

        public async Task ReloadCsvAsync() // Reloads CSV data, clears cache, updates UI
        {
            CsvParsed = false; // Marks CSV as not parsed
            CsvSectionsByTitle.Clear(); // Clears section cache
            await LoadCsvAsync(); // Loads CSV data
        }

        private void NotificationBell_Click(object sender, RoutedEventArgs e) // Handles notification bell click, shows flyout
        {
            HEATUI.ShowNotificationFlyout();
        }

        private void ClearNotifications_Click(object sender, RoutedEventArgs e) // Handles clear notifications button, clears log
        {
            HEATUI.ClearAllNotifications();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) // Handles settings button click, sets version text
        {
            ReleaseVersionText.Text = "v1.0.0";
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e) // Handles update check button, shows dialog
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Check for Updates",
                Content = "You are running the latest version.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        private void ReplayTutorial_Click(object sender, RoutedEventArgs e) // Handles tutorial replay button, shows dialog
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Replay Tutorial",
                Content = "Tutorial replay started.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        private void DebugModeToggle_Toggled(object sender, RoutedEventArgs e) // Handles debug mode toggle
        {
            // Debug mode toggle logic
        }

        private void AboutHEAT_Click(object sender, RoutedEventArgs e) // Handles about button, opens link in browser
        {
            var url = "https://your-placeholder-link.com";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
