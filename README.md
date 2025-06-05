HEAT Windows App 🔥

The Windows platform implementation of the multi-platform HEAT project.

Overview 📋

The HEAT Windows App is a modern desktop application for Windows 10/11, built as part of the cross-platform HEAT project. This app provides a native Windows experience using WinUI 3 and .NET, delivering a sleek Fluent UI interface and robust performance. It serves as the Windows client in the HEAT suite, offering full feature parity with its mobile counterpart and optionally integrating with shared C++ modules for heavy-duty tasks.

Key points: The Windows app branch contains all code specific to the Windows implementation. It shares core logic concepts with the Android app (via common C++ modules) while tailoring the user experience to Windows. Whether you’re running standalone or alongside other platform apps, the Windows app ensures a consistent and responsive experience for HEAT users on the desktop.

Tech Stack 🛠

This application is built with the following technologies:
 • WinUI 3 – Modern Windows UI Library (Windows App SDK) for native Windows 10/11 user interfaces.
 • .NET 6+ (C#) – Core application logic and framework (leveraging the latest C# features for reliability and speed).
 • Native C++ modules (optional) 🔧 – Integrated via C++/WinRT or P/Invoke for performance-critical components (shared with other platform implementations).
 • MVVM Architecture – Clean separation of UI and logic using the Model-View-ViewModel pattern (facilitated by libraries like Windows Community Toolkit MVVM).

Folder Structure 📁

Below is an overview of the repository structure for the windows-app branch:
Features ✨

The HEAT Windows App includes a range of features to provide a rich user experience:
 • Modern Fluent UI: Leverages WinUI 3 controls and styles for a slick, responsive interface that feels at home on Windows 11.
 • Seamless Performance: Critical operations are offloaded to native C++ modules for speed 🔧, ensuring smooth performance even with heavy workloads.
 • Cross-Platform Sync: Ensures feature parity and data synchronization with the Android app, so users can switch between devices effortlessly (when paired with a common backend or data store).
 • Offline Capability: Works fully offline for core functionalities, syncing data when connectivity is restored (if applicable to project domain).
 • Extensibility: Built with a modular architecture, making it easy to update, maintain, and even integrate additional platform modules or services in the future.

(Feel free to modify or add features based on the actual project specifics. The above are generic examples highlighting common strengths of a WinUI 3 app in a cross-platform project.)

Getting Started 🚀

Follow these steps to set up and run the Windows app on your local machine.

Prerequisites
 • Windows 10 or 11 – Development machine running Windows 10 (build 19041+) or Windows 11.
 • .NET 6 SDK or higher – Ensure the .NET SDK is installed for building the project.
 • Visual Studio 2022 (17.3 or later) – with the “Desktop Development with C++” and “Universal Windows Platform development” workloads (optional but recommended for WinUI 3 development).
 • Windows App SDK/WinUI 3 – The project uses WinUI 3 via NuGet; Visual Studio will install the Windows App SDK automatically.
 • HEAT C++ Modules (optional) – If you plan to use the native modules, you may need to compile the code from the cpp-modules branch and place the resulting binaries in the Modules/ directory or a known location. (See the cpp-modules branch README for build instructions.)
HEAT Windows App 🔥

The Windows platform implementation of the multi-platform HEAT project.

⸻

Overview 📋

The HEAT Windows App is a modern desktop application for Windows 10/11, built as part of the cross-platform HEAT project. This app provides a native Windows experience using WinUI 3 and .NET, delivering a sleek Fluent UI interface and robust performance. It serves as the Windows client in the HEAT suite, offering full feature parity with its mobile counterpart and optionally integrating with shared C++ modules for heavy-duty tasks.

Key points:
The Windows app branch contains all code specific to the Windows implementation. It shares core logic concepts with the Android app (via common C++ modules) while tailoring the user experience to Windows. Whether you’re running standalone or alongside other platform apps, the Windows app ensures a consistent and responsive experience for HEAT users on the desktop.

⸻

Tech Stack 🛠
 • WinUI 3 – Modern Windows UI Library (Windows App SDK) for native Windows 10/11 user interfaces
 • .NET 6+ (C#) – Core application logic and framework
 • Native C++ modules (optional) – Integrated via C++/WinRT or P/Invoke for performance-critical features
 • MVVM Architecture – Using Windows Community Toolkit MVVM or similar libraries

⸻

Folder Structure 📁

Top-level organization of this branch:
 • HEAT.WindowsApp.sln – Visual Studio solution
 • HEAT.WindowsApp/ – Core app code
 • App.xaml / App.xaml.cs – App entry point
 • MainWindow.xaml / MainWindow.xaml.cs – Main UI and logic
 • Assets/ – Icons, images
 • Views/ – Page layouts
 • ViewModels/ – Logic layers
 • Modules/ – Optional folder for compiled C++ libraries
 • README.md – This file
 • LICENSE – Project license

⸻

Features ✨
 • Modern WinUI 3 Fluent interface
 • Notification panel and modular content pages
 • Settings flyout with version, tutorial, debug toggle, and update check
 • Smart product quiz that recommends Adam:ONE, DNSharmony, or Adam:GO
 • Optional native C++ integration for performance
 • Dark theme styling and responsive layout

⸻

Getting Started 🚀

Prerequisites:
 • Windows 10 or 11 (build 19041+)
 • .NET 6 SDK or higher
 • Visual Studio 2022 (with UWP + C++ workload)
 • Windows App SDK via NuGet
 • (Optional) C++ modules compiled from cpp-modules branch

Steps:
 1. Clone this repo and switch to the windows-app branch
 2. Open HEAT.WindowsApp.sln in Visual Studio
 3. Restore NuGet packages
 4. Build the project
 5. Run it with F5 or from the .NET CLI
 6. (Optional) Drop native DLLs into Modules/ if needed

⸻

Dependencies 📦
 • Microsoft.WindowsAppSDK
 • .NET 6+ runtime
 • CommunityToolkit.Mvvm (if MVVM toolkit used)
 • Optional native DLLs from cpp-modules
 • JSON libraries, networking clients as needed

⸻

Project Status 🟢

Active development. Core features are complete. Improvements and polish in progress. C++ module integration is available but optional. The app is fully functional on Windows 10/11.

⸻

Contributors 🤝
 • Alice Smith – Project Lead
 • Bob Johnson – WinUI / C++ integration
 • Carol Lee – UI/UX
 • Derek Wong – Android sync
 • …and all open source contributors

⸻

Related Branches 🔀
 • main – Project overview, coordination, docs
 • android-app – Android implementation
 • cpp-modules – Native shared logic and parsers
