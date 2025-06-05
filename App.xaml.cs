/*
    File: App.xaml.cs
    Purpose: Entry point for HEAT application, manages startup and main window activation
    Created: June 2 2025
    Last Modified: June 3 2025
    Author: Anthony Samen
*/

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
