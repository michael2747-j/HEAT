# HEAT Application Core Files README

## Overview

This document covers the two main files responsible for the HEAT application’s startup, window management, and shared UI resources.

---

## 1. App.xaml

**File:** `App.xaml`  
**Purpose:** Defines shared resources, styles, and templates for the HEAT application’s user interface.  
**Author:** Anthony Samen  
**Created:** June 2, 2025  
**LastModified:** June 3, 2025

### Structure

- **Root Element:** ``
  - **Namespaces:**  
    - `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` (Default UI elements)
    - `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` (XAML features)
    - `xmlns:local="using:HEAT"` (Local project types)
  - **Class:** `x:Class="HEAT.App"` (Application entry point)
- **Application.Resources:**  
  - **ResourceDictionary:** Container for all shared resources.
  - **MergedDictionaries:** Imports external resource dictionaries (e.g., WinUI control styles).

### Resource Highlights

- **Shared Brushes:** Colors for tabs, borders, and icons.
- **Tab Button Template:** Custom template with hover and pressed animations.
- **Tab Button Styles:** Normal and active tab styles.
- **Icon Button Template:** Template for icon-only buttons with scale animations.
- **Icon Button Style:** Style for icon buttons.
- **Global Centering Styles:** Styles for `Page` and `UserControl` to center content and limit width.

### Usage

- **Applying Styles:** Use `Style="{StaticResource ...}"` on buttons and controls.
- **Using Brushes:** Use `{StaticResource ...}` for colors.
- **Global Styles:** Automatically applied to all `Page` and `UserControl` elements.

---

## 2. App.xaml.cs

**File:** `App.xaml.cs`  
**Purpose:** Entry point for the HEAT application, manages startup and main window activation.  
**Author:** Anthony Samen  
**Created:** June 2, 2025  
**LastModified:** June 3, 2025

### Structure

```csharp
using Microsoft.UI.Xaml; // Imports types for XAML-based UI framework

namespace HEAT // Declares namespace for HEAT project
{
    public partial class App : Application // Defines App class, inherits from Application, enables custom startup logic
    {
        public static Window MainWindow { get; private set; } // Declares static property MainWindow, holds reference to main window, allows only internal setting

        public App() // Constructor for App class, takes no arguments, called at application startup
        {
            this.InitializeComponent(); // Calls method to initialize components defined in XAML, sets up application resources
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args) // Method overrides OnLaunched, takes LaunchActivatedEventArgs as argument, triggers on application launch
        {
            MainWindow = new MainWindow(); // Instantiates MainWindow, assigns instance to static MainWindow property
            MainWindow.Activate(); // Calls Activate on MainWindow, displays window to user
        }
    }
}
```

### Key Features

- **Application Entry Point:** The `App` class is the entry point for the application.
- **MainWindow Property:** Static property to access the main window from anywhere in the app.
- **Initialization:** The constructor calls `InitializeComponent()` to set up resources defined in `App.xaml`.
- **Window Activation:** The `OnLaunched` method creates and activates the main window when the app starts.

---

## Integration and Usage

- **Startup:** The `App` class initializes the application and creates the main window.
- **UI Resources:** All shared styles, templates, and brushes are defined in `App.xaml` and are available throughout the application.
- **Window Management:** The `MainWindow` property provides a static reference to the main window for use across the app.

---

## Example Workflow

1. **Application Starts:**  
   - The `App` constructor is called.
   - `InitializeComponent()` loads resources from `App.xaml`.
2. **Window Creation:**  
   - The `OnLaunched` method is called.
   - A new `MainWindow` is created and activated.
3. **UI Consistency:**  
   - All buttons, pages, and user controls use the styles and templates defined in `App.xaml`.

---