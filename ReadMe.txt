Network Event Log Extractor - README
====================================

Description
-----------
This program extracts historical Windows network-related events and summarizes them for analysis. It generates three files:

1. historical_network_log.txt
   - Contains the full, detailed XML/text of each relevant network event from Windows Event Logs and the Windows Firewall log.

2. summary_network_log.txt
   - Contains a concise, one-line summary for each event, making it easy to scan and review network activity.

3. event_id_reference.txt
   - Lists all unique Event IDs found in the logs, with a human-readable description for each (covering all relevant network and Wi-Fi events).

What the App Does
-----------------
This application automatically scans your Windows system’s event logs for historical network activity-including Wi-Fi connections, disconnections, network profile changes, and firewall events. It then exports:

- A comprehensive record of all relevant events (for deep analysis)
- A quick, human-readable summary of activity (for easy review)
- A reference list explaining what each encountered Event ID means

This helps users, administrators, or analysts quickly understand the network activity history on a Windows computer, without having to manually sift through raw event logs.

Prerequisites & Setup
---------------------
- **Permissions:** Run the program as an Administrator to ensure access to all system logs and the Windows Firewall log.

Optional:
- Enable Windows Firewall logging if you want firewall events included. This can be done via Windows Defender Firewall settings (see "Logging" tab).

How to Build and Run
--------------------
1. Open the solution/project in Visual Studio.
2. Ensure you have included all necessary source files.
3. Build the project (Ctrl+Shift+B).
4. Run the compiled executable as Administrator.

   - You can do this by right-clicking the .exe and selecting "Run as administrator" or by starting Visual Studio as Administrator and running from there.

5. After execution, you will find three files in the same directory as the executable:
   - `historical_network_log.txt`
   - `summary_network_log.txt`
   - `event_id_reference.txt`

Troubleshooting
---------------
- If the program reports "Failed to open log files," ensure you have write permissions in the output directory.
- If you see "Failed to query" messages, make sure you are running as Administrator.
- Some Event IDs may be labeled as "undocumented" or "unknown"; these are rare or system-specific events not covered in public documentation.

Customization
-------------
- To add more Event IDs or descriptions, edit the `eventIdDescriptions` map in the source code.
- You can adjust the time window by changing the value of `twoWeeksAgo` in the code.