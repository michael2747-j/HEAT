using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAT
{
    public static class HEATUI
    {
        private const string LogFileName = "CHATTER_LOG.txt";
        private static ItemsControl? _notificationList;
        private static Flyout? _notificationFlyout;
        private static FrameworkElement? _flyoutTarget;
        private static string _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), LogFileName);

        public static void InitializeNotificationUI(ItemsControl notificationList, Flyout notificationFlyout, FrameworkElement flyoutTarget)
        {
            _notificationList = notificationList ?? throw new ArgumentNullException(nameof(notificationList));
            _notificationFlyout = notificationFlyout ?? throw new ArgumentNullException(nameof(notificationFlyout));
            _flyoutTarget = flyoutTarget ?? throw new ArgumentNullException(nameof(flyoutTarget));

            // Ensure log file exists
            if (!File.Exists(_logFilePath))
            {
                File.Create(_logFilePath).Close();
            }
        }

        public static void LoadNotifications()
        {
            if (_notificationList == null) return;

            _notificationList.Items.Clear();

            try
            {
                if (File.Exists(_logFilePath))
                {
                    var lines = File.ReadLines(_logFilePath).Reverse(); // Show newest first
                    bool any = false;

                    foreach (var line in lines)
                    {
                        // Allow empty lines to be displayed as spacers
                        string displayText = string.IsNullOrWhiteSpace(line) ? "" : line;
                        _notificationList.Items.Add(new TextBlock
                        {
                            Text = displayText,
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                        any = true;
                    }

                    if (!any)
                    {
                        ShowNoNotificationsMessage();
                    }
                }
                else
                {
                    ShowNoNotificationsMessage();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading notifications: {ex.Message}");
                _notificationList.Items.Add(new TextBlock
                {
                    Text = "Error loading notifications.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                    FontSize = 14
                });
            }
        }

        public static void LogMessage(string message)
        {
            try
            {
                // Write empty or whitespace messages without a timestamp
                string textToWrite = string.IsNullOrWhiteSpace(message)
                    ? Environment.NewLine
                    : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, textToWrite, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        private static void ShowNoNotificationsMessage()
        {
            if (_notificationList == null) return;

            _notificationList.Items.Add(new TextBlock
            {
                Text = "No notifications.",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                FontSize = 14
            });
        }

        public static void ClearAllNotifications()
        {
            try
            {
                File.WriteAllText(_logFilePath, string.Empty);
                LoadNotifications();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing notifications: {ex.Message}");
            }
        }

        public static void ShowNotificationFlyout()
        {
            if (_notificationFlyout == null || _flyoutTarget == null) return;

            LoadNotifications();
            _notificationFlyout.ShowAt(_flyoutTarget);
        }
    }
}