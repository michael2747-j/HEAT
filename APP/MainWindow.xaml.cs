using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HEAT
{
    public sealed partial class MainWindow : Window
    {
        private const string CsvPath = @"C:\Users\antho\Desktop\STATE_SAVE.csv";

        public Dictionary<string, (string TableTitle, List<string> Headers, List<IDictionary<string, string>> Rows)> CsvSectionsByTitle { get; private set; } = new();
        public bool CsvParsed { get; private set; } = false;

        public static MainWindow? Instance { get; private set; }

        public event Action? CsvCacheReloaded;

        public MainWindow()
        {
            Instance = this;
            this.InitializeComponent();
            MaximizeOnStartup();
            run_ui();
            _ = LoadCsvAsync();

            // Initialize HEATUI with notification controls
            HEATUI.InitializeNotificationUI(NotificationList, NotificationFlyout, NotificationBell);
        }

        private void MaximizeOnStartup()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
            }
            catch { }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetActiveTab(btn.Name);
            }
        }

        private void SetActiveTab(string tabName)
        {
            HomeTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            DashboardTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            LinksTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];
            UpgradesTab.Style = (Style)Application.Current.Resources["TabButtonStyle"];

            switch (tabName)
            {
                case "HomeTab":
                    HomeTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Home();
                    break;
                case "DashboardTab":
                    DashboardTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Dashboard();
                    break;
                case "LinksTab":
                    LinksTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Links();
                    break;
                case "UpgradesTab":
                    UpgradesTab.Style = (Style)Application.Current.Resources["ActiveTabButtonStyle"];
                    TabContent.Content = new Upgrades();
                    break;
            }
        }

        void run_ui() { SetActiveTab("HomeTab"); }

        public async Task LoadCsvAsync()
        {
            var parsedSections = await Task.Run(() => ParseCsvSectionsOptimized());
            DispatcherQueue.TryEnqueue(() =>
            {
                CsvSectionsByTitle = parsedSections;
                CsvParsed = true;
                CsvCacheReloaded?.Invoke();
            });
        }

        private Dictionary<string, (string TableTitle, List<string> Headers, List<IDictionary<string, string>> Rows)> ParseCsvSectionsOptimized()
        {
            var result = new Dictionary<string, (string, List<string>, List<IDictionary<string, string>>)>();

            if (!File.Exists(CsvPath))
                return result;

            var lines = File.ReadAllLines(CsvPath, Encoding.UTF8);
            int i = 0;
            int totalLines = lines.Length;

            while (i < totalLines)
            {
                while (i < totalLines && string.IsNullOrWhiteSpace(lines[i]))
                    i++;
                if (i >= totalLines)
                    break;

                string tableTitleRaw = lines[i].Split(',')[0].Trim();
                string tableTitle = tableTitleRaw.TrimEnd(':');
                i++;

                while (i < totalLines && string.IsNullOrWhiteSpace(lines[i]))
                    i++;
                if (i >= totalLines)
                    break;

                var header = SplitCsvLineFast(lines[i]);
                i++;

                if (tableTitle == "Total Network")
                {
                    int previewIdx = i;
                    while (previewIdx < totalLines && string.IsNullOrWhiteSpace(lines[previewIdx]))
                        previewIdx++;
                    if (previewIdx < totalLines)
                    {
                        int pipeCount = lines[previewIdx].Split(',')[0].Count(c => c == '|');
                        if (pipeCount > 0)
                        {
                            var firstHeader = header[0];
                            header.RemoveAt(0);
                            for (int k = 0; k <= pipeCount; k++)
                                header.Insert(k, $"{firstHeader} {k + 1}");
                        }
                    }
                }

                var rows = new List<IDictionary<string, string>>(128);
                while (i < totalLines && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    var values = SplitCsvLineFast(lines[i]);
                    if (tableTitle == "Total Network" && values.Count > 0)
                    {
                        var firstField = values[0];
                        var splitFirst = firstField.Split('|');
                        if (splitFirst.Length > 1)
                        {
                            values.RemoveAt(0);
                            for (int k = splitFirst.Length - 1; k >= 0; k--)
                                values.Insert(0, splitFirst[k]);
                        }
                    }
                    while (values.Count < header.Count)
                        values.Add("");
                    while (values.Count > header.Count)
                        values.RemoveAt(values.Count - 1);

                    var dict = new Dictionary<string, string>(header.Count);
                    for (int j = 0; j < header.Count; j++)
                        dict[header[j]] = values[j];

                    rows.Add(dict);
                    i++;
                }
                result[tableTitle] = (tableTitleRaw, header, rows);
            }
            return result;
        }

        private static List<string> SplitCsvLineFast(string line)
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

        public async Task ReloadCsvAsync()
        {
            CsvParsed = false;
            CsvSectionsByTitle.Clear();
            await LoadCsvAsync();
        }

        private void NotificationBell_Click(object sender, RoutedEventArgs e)
        {
            HEATUI.ShowNotificationFlyout();
        }

        private void ClearNotifications_Click(object sender, RoutedEventArgs e)
        {
            HEATUI.ClearAllNotifications();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ReleaseVersionText.Text = "v1.0.0";
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
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

        private void ReplayTutorial_Click(object sender, RoutedEventArgs e)
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

        private void DebugModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // Debug mode toggle logic
        }

        private void AboutHEAT_Click(object sender, RoutedEventArgs e)
        {
            var url = "https://your-placeholder-link.com";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}