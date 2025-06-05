# HEAT Home Module: README

## Overview

The **HEAT Home** module is the main user interface for the HEAT network analysis application. It provides controls for starting, pausing, saving, and clearing network analysis sessions, as well as a live chat-style log for displaying analysis results and user messages.

---

## Files

### **1. Home.xaml**

- **Purpose:**  
  Defines the layout for the HEAT home screen, including analysis controls and a chat log display.
- **Created:** June 2, 2025
- **Last Modified:** June 4, 2025
- **Authors:** Anthony Samen, Cody Cartwright

#### **Structure**

- **Main Grid:**  
  - **Row 0:** Buttons for control actions (Start, Pause, Save, Clear).
  - **Row 1:** Border containing the chat log and decorative elements (logo, animated fire).
- **Chat Log:**  
  - **ScrollViewer:** Enables scrolling through chat messages.
  - **ItemsControl:** Binds to `ChatterLines` collection for dynamic chat updates.
  - **DataTemplate:** Defines appearance of each chat line (text, color, font).

#### **Key Features**

- **Buttons:**  
  - **Start Network Analysis:** Launches network capture.
  - **Pause Network Analysis:** Stops ongoing capture.
  - **Save Analysis State:** Saves current analysis to a CSV file.
  - **Clear Analysis Log:** Clears both UI and file logs.
- **Chat Log:**  
  - Displays real-time messages with animated text and color coding.
  - Supports efficient rendering with virtualization.

---

### **2. Home.xaml.cs**

- **Purpose:**  
  Implements logic for the HEAT home screen, managing network analysis, UI chatter log, and state saving.
- **Created:** June 2, 2025
- **Last Modified:** June 4, 2025
- **Authors:** Anthony Samen, Cody Cartwright

#### **Structure**

- **Home Class:**  
  - Inherits from `UserControl` and implements `INotifyPropertyChanged`.
  - Manages UI state, network devices, and chat messages.
- **ChatterLine Class:**  
  - Represents a single line in the chat log.
  - Supports property change notification for dynamic updates.

#### **Key Features**

- **Network Analysis:**  
  - **Start:** Launches capture on selected interfaces.
  - **Pause:** Stops and cleans up capture sessions.
  - **Save:** Copies the real-time log to a state file and parses statistics.
  - **Clear:** Clears the chat log and resets the log file.
- **Chat Log Management:**  
  - **AddChatterMessageAsync:** Adds messages with animation and color.
  - **AddChatterMessageWithCount:** Adds messages with count prefixes.
- **State Parsing:**  
  - **ParseStateSave:** Parses the state file to extract section counts and protocols.
- **Utility Methods:**  
  - **ColorFromHex, ColorFromValues:** Helper methods for color conversion.

---

## Usage

### **1. Starting Network Analysis**

1. **Click "Start Network Analysis".**
2. **Choose analysis mode:**
   - **Run on All Interfaces:** Captures on all available interfaces.
   - **Custom Interface Selection:** (Not fully implemented in provided code; placeholder for future enhancement.)
3. **Analysis begins, and results are displayed in the chat log.**

### **2. Pausing Analysis**

- **Click "Pause Network Analysis"** to stop the current capture session.
- **All devices are stopped and cleaned up.**

### **3. Saving Analysis State**

- **Click "Save Analysis State".**
- **The real-time log is copied to a state file (`STATE_SAVE.csv`).**
- **Statistics are parsed and displayed in the chat log.**

### **4. Clearing the Log**

- **Click "Clear Analysis Log".**
- **The chat log and real-time log file are cleared.**

### **5. Exporting VyOS Configuration**

- **Click "Export VyOS Configuration"** (if implemented in UI; in code, it simulates export).
- **A success message is displayed in the chat log.**

---

## Detailed Description

### **UI Layout**

- **Buttons:**  
  - Arranged in a horizontal row at the top.
  - Each button has a distinct style and click handler.
- **Chat Log:**  
  - Centered in the main area.
  - Features a semi-transparent background, rounded corners, and decorative overlays (logo, animated fire).
  - Messages are displayed with color coding and animation.

### **Code Logic**

- **Network Capture:**  
  - Uses `SharpPcap` for packet capture.
  - Supports cancellation and cleanup.
- **Chat Log:**  
  - Messages are added asynchronously with animation.
  - Supports cancellation and thread-safe updates.
- **State Saving:**  
  - Copies the log file and parses it for statistics.
  - Displays counts for various network sections (LAN, WAN, VLAN, NAT, DHCP, DNS, VPN, AD, routing protocols).

### **ChatterLine Class**

- **Properties:**  
  - `DisplayText`: Animated text displayed in the UI.
  - `FullText`: Complete message text.
  - `LineBrush`: Color of the message.
- **Events:**  
  - `PropertyChanged`: Notifies UI of changes for dynamic updates.

---

## Dependencies

- **SharpPcap:** For network packet capture.
- **Microsoft.UI.Xaml:** For WinUI controls and styling.
- **System.Threading.Tasks:** For asynchronous operations.

---

## Notes

- **Thread Safety:**  
  - Chat log updates are protected by a semaphore to prevent race conditions.
- **Error Handling:**  
  - Most operations include try-catch blocks for robust error handling.
- **Extensibility:**  
  - The code is structured for easy addition of new features (e.g., custom interface selection, additional export formats).

---

## Example Output

When saving the analysis state, the chat log displays messages like:

```
Saving the Network Analysis State.
Network State Save generated at 2025-06-04 14:30:00.
3 LAN Interface Configurations logged.
2 WAN Interface Configurations logged.
1 vLAN Interface Configurations logged.
...
Protocols observed: OSPF, BGP.
```

---

## Summary Table

| Button                | Action                                      |
|-----------------------|---------------------------------------------|
| Start Network Analysis| Starts capture on selected interfaces       |
| Pause Network Analysis| Stops ongoing capture                       |
| Save Analysis State   | Saves log to file and displays statistics   |
| Clear Analysis Log    | Clears chat log and log file                |

---

## Authors

- **Anthony Samen**
- **Cody Cartwright**

---

**Last Updated:** June 4, 2025

---