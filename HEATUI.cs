/*
    File: HEATUI.cs
    Purpose: Manages notification UI, logging, and flyout display for HEAT application
    Created: June 4 2025
    Last Modified: June 5 2025
    Author: Cody Cartwright
*/

using Microsoft.UI.Xaml; // Imports UI framework types
using Microsoft.UI.Xaml.Controls; // Imports controls like ItemsControl, Flyout
using Microsoft.UI.Xaml.Media; // Imports brush and color utilities
using System; // Imports core types
using System.Collections.Generic; // Imports generic collections
using System.Diagnostics; // Imports debugging utilities
using System.IO; // Imports file operations
using System.Linq; // Imports LINQ for collections
using System.Text; // Imports text encoding
using System.Threading.Tasks; // Imports async/await support

namespace HEAT // Declares project namespace
{
    public static class HEATUI // Declares static class for notification UI management
    {
        private const string LogFileName = "CHATTER_LOG.txt"; // Sets log file name constant
        private static ItemsControl? _notificationList; // Holds reference to notification list control
        private static Flyout? _notificationFlyout; // Holds reference to notification flyout
        private static FrameworkElement? _flyoutTarget; // Holds reference to flyout anchor element
        private static string _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), LogFileName); // Sets log file path on desktop

        public static void InitializeNotificationUI(ItemsControl notificationList, Flyout notificationFlyout, FrameworkElement flyoutTarget) // Sets up notification UI, takes controls as arguments, returns void
        {
            _notificationList = notificationList ?? throw new ArgumentNullException(nameof(notificationList)); // Assigns notification list, throws if null
            _notificationFlyout = notificationFlyout ?? throw new ArgumentNullException(nameof(notificationFlyout)); // Assigns flyout, throws if null
            _flyoutTarget = flyoutTarget ?? throw new ArgumentNullException(nameof(flyoutTarget)); // Assigns flyout target, throws if null

            if (!File.Exists(_logFilePath)) // Checks if log file exists
            {
                File.Create(_logFilePath).Close(); // Creates empty log file if missing
            }
        }

        public static void LoadNotifications() // Loads notifications from log file, populates UI, returns void
        {
            if (_notificationList == null) return; // Exits if notification list not set

            _notificationList.Items.Clear(); // Clears current notifications

            try
            {
                if (File.Exists(_logFilePath)) // Checks if log file exists
                {
                    var lines = File.ReadLines(_logFilePath).Reverse(); // Reads lines, newest first
                    bool any = false; // Tracks if any notifications found

                    foreach (var line in lines) // Iterates each line
                    {
                        string displayText = string.IsNullOrWhiteSpace(line) ? "" : line; // Uses blank for empty lines
                        _notificationList.Items.Add(new TextBlock // Adds notification as TextBlock
                        {
                            Text = displayText,
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                        any = true;
                    }

                    if (!any) // If no notifications found
                    {
                        ShowNoNotificationsMessage(); // Shows "No notifications" message
                    }
                }
                else
                {
                    ShowNoNotificationsMessage(); // Shows "No notifications" if file missing
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading notifications: {ex.Message}"); // Logs error to debug output
                _notificationList.Items.Add(new TextBlock // Adds error message to UI
                {
                    Text = "Error loading notifications.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                    FontSize = 14
                });
            }
        }

        public static void LogMessage(string message) // Appends message to log file, takes string, returns void
        {
            try
            {
                string textToWrite = string.IsNullOrWhiteSpace(message)
                    ? Environment.NewLine // Writes empty line for blank messages
                    : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}"; // Formats message with timestamp
                File.AppendAllText(_logFilePath, textToWrite, Encoding.UTF8); // Appends to log file
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}"); // Logs error to debug output
            }
        }

        private static void ShowNoNotificationsMessage() // Adds "No notifications" message to UI, returns void
        {
            if (_notificationList == null) return; // Exits if notification list not set

            _notificationList.Items.Add(new TextBlock // Adds gray message to UI
            {
                Text = "No notifications.",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                FontSize = 14
            });
        }

        public static void ClearAllNotifications() // Clears log file and UI, returns void
        {
            try
            {
                File.WriteAllText(_logFilePath, string.Empty); // Empties log file
                LoadNotifications(); // Reloads notification UI
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing notifications: {ex.Message}"); // Logs error to debug output
            }
        }

        public static void ShowNotificationFlyout() // Shows notification flyout at target, returns void
        {
            if (_notificationFlyout == null || _flyoutTarget == null) return; // Exits if flyout or target not set

            LoadNotifications(); // Loads notifications into UI
            _notificationFlyout.ShowAt(_flyoutTarget); // Displays flyout at anchor element
        }
    }
}
