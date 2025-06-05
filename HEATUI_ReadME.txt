# HEATUI.cs  
**Comprehensive README**

---

## Overview

**HEATUI.cs** is a static utility class for the HEAT application, designed to manage notification UI, logging, and flyout display. It provides a simple API for integrating notification functionality into your app, including persistent logging, UI population, and user interaction via flyouts.

---

## Table of Contents

1. [File Purpose](#file-purpose)
2. [Features](#features)
3. [Usage](#usage)
    - [Initialization](#initialization)
    - [Logging Notifications](#logging-notifications)
    - [Loading and Displaying Notifications](#loading-and-displaying-notifications)
    - [Clearing Notifications](#clearing-notifications)
    - [Showing the Notification Flyout](#showing-the-notification-flyout)
4. [UI Integration](#ui-integration)
5. [File Structure and Constants](#file-structure-and-constants)
6. [Error Handling](#error-handling)
7. [Customization](#customization)
8. [Troubleshooting](#troubleshooting)
9. [Author and History](#author-and-history)

---

## File Purpose

- **Manages notification UI**: Populates a UI control (e.g., `ItemsControl`) with notification messages.
- **Handles persistent logging**: Stores notifications in a log file on the user's desktop.
- **Controls flyout display**: Shows notifications in a flyout anchored to a UI element.

---

## Features

- **Persistent log file**: All notifications are saved in `CHATTER_LOG.txt` on the desktop.
- **Reverse chronological display**: Newest notifications appear at the top.
- **"No notifications" fallback**: UI shows a friendly message if there are no notifications.
- **Clear all**: Quickly remove all notifications from both UI and log.
- **Error handling**: UI feedback and debug logging for file errors.

---

## Usage

### Initialization

Before using any notification features, you must initialize the UI references:

```csharp
HEATUI.InitializeNotificationUI(
    notificationList: myItemsControl,
    notificationFlyout: myFlyout,
    flyoutTarget: myButton // or any FrameworkElement
);
```

- **notificationList**: An `ItemsControl` (e.g., `ListView`, `ListBox`) for displaying notifications.
- **notificationFlyout**: A `Flyout` control for pop-up notifications.
- **flyoutTarget**: The UI element to which the flyout will be anchored.

---

### Logging Notifications

To add a new notification (which is also logged):

```csharp
HEATUI.LogMessage("This is a new notification!");
```

- Automatically timestamped and appended to the log file.
- Blank messages are written as empty lines.

---

### Loading and Displaying Notifications

To refresh the notification UI from the log file:

```csharp
HEATUI.LoadNotifications();
```

- Clears the current UI and reloads all notifications from the log.
- If no notifications exist, displays "No notifications."

---

### Clearing Notifications

To clear all notifications (both UI and log):

```csharp
HEATUI.ClearAllNotifications();
```

- Empties the log file and updates the UI accordingly.

---

### Showing the Notification Flyout

To display the notification flyout at the specified anchor:

```csharp
HEATUI.ShowNotificationFlyout();
```

- Loads notifications and shows the flyout at the `flyoutTarget`.

---

## UI Integration

- **ItemsControl**: Can be a `ListView`, `ListBox`, or any control that supports an `Items` collection.
- **Flyout**: Standard WinUI `Flyout` control.
- **FrameworkElement**: Any UI element (e.g., a button) to anchor the flyout.

**Example XAML:**

```xml


    

```

**Example Code-behind:**

```csharp
HEATUI.InitializeNotificationUI(NotificationList, NotificationFlyout, NotificationButton);
NotificationButton.Click += (s, e) => HEATUI.ShowNotificationFlyout();
```

---

## File Structure and Constants

- **Log file**: `CHATTER_LOG.txt` on the user's Desktop.
- **UI references**: Held privately within the static class.
- **Colors**: Notifications are white; errors are red; "No notifications" is gray.

---

## Error Handling

- All file operations are wrapped in try-catch blocks.
- Errors are written to the Debug output.
- UI displays a red error message if notifications cannot be loaded.

---

## Customization

- **Log file location**: Change the `_logFilePath` assignment if you want a different location.
- **UI styling**: Modify the `TextBlock` properties in `LoadNotifications()` and `ShowNoNotificationsMessage()` for different fonts, sizes, or colors.
- **Notification format**: Adjust the timestamp or message formatting in `LogMessage()` as needed.

---

## Troubleshooting

- **Notifications not appearing**: Ensure `InitializeNotificationUI` is called before any other method.
- **Log file missing**: The file is auto-created; check desktop permissions if issues persist.
- **Flyout not showing**: Make sure both `notificationFlyout` and `flyoutTarget` are valid and initialized.

---

## Author and History

- **Author**: Cody Cartwright
- **Created**: June 4, 2025
- **Last Modified**: June 5, 2025

---