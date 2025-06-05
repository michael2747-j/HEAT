# HEAT Main Window - Comprehensive README

## Table of Contents

- [Overview](#overview)
- [File Structure](#file-structure)
- [Main Features](#main-features)
- [UI Layout & Navigation](#ui-layout--navigation)
- [CSV Data Handling](#csv-data-handling)
- [Notifications System](#notifications-system)
- [Settings & Utilities](#settings--utilities)
- [Events & Code Structure](#events--code-structure)
- [Custom Styles and Resources](#custom-styles-and-resources)
- [Extending the Main Window](#extending-the-main-window)
- [Authors and Credits](#authors-and-credits)

---

## Overview

This README covers the **main window** implementation of the HEAT app, focusing on two tightly-coupled files:

- `MainWindow.xaml`: Defines the visual layout and structure of the main application window.
- `MainWindow.xaml.cs`: Contains the code-behind, providing logic for navigation, CSV data parsing, notifications, and settings.

The HEAT app is a modern Windows desktop application using WinUI 3, designed for efficient navigation, robust notification handling, and dynamic data loading from CSV files.

---

## File Structure

```
HEAT/
│
├── MainWindow.xaml         # Main window UI definition (XAML)
├── MainWindow.xaml.cs      # Main window logic (C# code-behind)
├── Home.xaml/.cs           # Home tab content (not detailed here)
├── Dashboard.xaml/.cs      # Dashboard tab content (not detailed here)
├── Links.xaml/.cs          # Links tab content (not detailed here)
├── Upgrades.xaml/.cs       # Upgrades tab content (not detailed here)
├── HEATUI.cs               # Utilities for notification UI (assumed)
└── ...                     # Other resources, styles, and assets
```

---

## Main Features

- **Tabbed Navigation**: Switch between Home, Dashboard, Links, and Upgrades.
- **Notification System**: In-app notifications with a flyout panel and "Clear All" functionality.
- **Settings Flyout**: Access version info, update checks, tutorial replay, debug mode, and About.
- **CSV Data Handling**: Efficiently loads, parses, and caches structured CSV data for use in the app.
- **Responsive UI**: Maximizes window on startup, uses modern WinUI controls and styles.

---

## UI Layout & Navigation

### Top Navigation Bar

- **Notification Bell** (Left): Opens a flyout with notifications.
- **Tab Buttons** (Center): Home, Dashboard, Links, Upgrades.
- **Settings Gear** (Right): Opens a flyout with settings and utilities.

### Main Content Area

- Displays the currently selected tab's content (user controls: `Home`, `Dashboard`, etc.).
- Encapsulated in a rounded, blue-bordered panel for visual clarity.

### Styles

- Uses custom styles for tab buttons, icons, and flyouts.
- Dark theme with blue accent borders.

---

## CSV Data Handling

### Purpose

HEAT loads and parses a structured CSV file (`STATE_SAVE.csv`) to populate data-driven views.

### How It Works

- **Asynchronous Loading**: CSV is loaded and parsed in a background task on app startup and on demand.
- **Section Parsing**: The CSV may contain multiple sections, each with its own title, headers, and rows.
- **Special Handling**: Sections like "Total Network" are parsed with custom logic to handle multi-column headers/rows.
- **Caching**: Parsed data is cached in `CsvSectionsByTitle` for efficient access by other components.
- **Events**: The `CsvCacheReloaded` event notifies listeners when new data is available.

### Key Members

- `CsvSectionsByTitle`: Dictionary mapping section titles to their data (headers, rows).
- `LoadCsvAsync()`: Loads and parses the CSV asynchronously.
- `ReloadCsvAsync()`: Clears cache and reloads CSV data.
- `ParseCsvSectionsOptimized()`: Efficiently parses the CSV into structured sections.

---

## Notifications System

- **Notification Bell**: Opens a flyout showing a scrollable list of notifications.
- **Clear All**: Button to clear all notifications.
- **Notification UI**: Managed via `HEATUI.InitializeNotificationUI()` and related methods.

---

## Settings & Utilities

Accessible via the gear icon in the top-right:

- **Version Display**: Shows current app version.
- **Check for Updates**: Button opens a dialog (currently always "latest version").
- **Replay Tutorial**: Button opens a dialog to replay the tutorial.
- **Debug Mode**: Toggle switch for enabling/disabling debug features.
- **About H.E.A.T**: Opens a browser link with more info.

---

## Events & Code Structure

### Main Events

- **Tab_Click**: Handles navigation between tabs.
- **NotificationBell_Click**: Opens notification flyout.
- **ClearNotifications_Click**: Clears notifications.
- **SettingsButton_Click**: Opens settings flyout and updates version text.
- **CheckForUpdates_Click**: Checks for updates (dialog).
- **ReplayTutorial_Click**: Replays tutorial (dialog).
- **DebugModeToggle_Toggled**: Handles debug mode toggle.
- **AboutHEAT_Click**: Opens About link in browser.

### Singleton Pattern

- `MainWindow.Instance`: Static reference for global access.

### Maximizing on Startup

- Uses WinUI interop to maximize the window when the app launches.

---

## Custom Styles and Resources

- **NoBorderFlyoutPresenterStyle**: Removes borders and adds rounded corners to flyouts.
- **IconButtonStyle**: Uniform style for icon buttons (bell, gear).
- **TabButtonStyle/ActiveTabButtonStyle**: Distinct styles for selected and unselected tabs.
- **BorderBlue**: Blue accent color for borders and dividers.
- **IconColor**: Consistent icon color.

---

## Extending the Main Window

### Adding New Tabs

1. Create a new UserControl (e.g., `Reports.xaml/.cs`).
2. Add a new button to the tab StackPanel in `MainWindow.xaml`.
3. Update `SetActiveTab()` in `MainWindow.xaml.cs` to handle the new tab.

### Customizing Notifications

- Extend `HEATUI` to add new notification types or behaviors.
- Modify the notification flyout in XAML for additional features.

### Enhancing CSV Parsing

- Update `ParseCsvSectionsOptimized()` to support new CSV formats or data structures.
- Listen to `CsvCacheReloaded` for real-time updates in other components.

---

## Authors and Credits

- **Anthony Samen**
- **Cody Cartwright**

_Last Modified: June 4, 2025_

---

## Troubleshooting & FAQ

- **CSV Not Loading?**  
  Ensure the path in `CsvPath` is correct and the file is accessible.

- **UI Not Updating After CSV Change?**  
  Call `ReloadCsvAsync()` to force a refresh.

- **Adding New Features?**  
  Follow the extension guidelines above and keep UI logic in the code-behind for maintainability.

---