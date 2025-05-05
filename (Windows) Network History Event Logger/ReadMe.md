Network Event Log and Firewall Summary Tool - README
====================================================

Description
-----------
This program extracts and summarizes historical Windows network-related events and Windows Firewall connection logs. It generates four files:

1. historical_network_log.txt
   - Full, detailed XML/text of each relevant network event from Windows Event Logs and the Windows Firewall log.

2. summary_network_log.txt
   - Concise, one-line summaries for each event, making it easy to scan and review network activity.

3. event_id_reference.txt
   - A reference list of all unique Event IDs found in the logs, with human-readable descriptions for each.

4. network_summary.csv
   - A CSV summary of network connection activity, aggregated from the Windows Firewall log. Columns include device name, IP/hostname, protocol, source/destination ports, frequency, and total bytes.

What the App Does
-----------------
- Scans your Windows system’s event logs for historical network activity, including Wi-Fi connections, disconnections, network profile changes, and more.
- Reads the Windows Firewall log (if enabled) and aggregates per-destination statistics for network connections.
- Outputs comprehensive logs for deep analysis, summaries for quick review, a reference for Event IDs, and a CSV for spreadsheet analysis.

Prerequisites & Setup
---------------------
- **Operating System:** Windows 10/11 or Windows Server
- **Compiler:** Microsoft Visual Studio (2015 or newer recommended)
- **Permissions:** Run the program as Administrator to ensure access to all system logs and the Windows Firewall log.
- **Libraries:** Links against `wevtapi.lib` (included with the Windows SDK).

**Firewall Log Setup (Required for CSV Output):**
1. Open Windows Defender Firewall with Advanced Security (`wf.msc`).
2. Right-click "Windows Defender Firewall with Advanced Security on Local Computer" and choose "Properties".
3. For each profile (Domain, Private, Public):
   - Click the "Customize..." button in the "Logging" section.
   - Set "Log dropped packets" and/or "Log successful connections" to **Yes**.
   - Note the "Log file path" (default: `C:\Windows\System32\LogFiles\Firewall\pfirewall.log`).
   - Click OK.
4. Click OK to close all dialogs.
5. After some network activity, the log file will be created and populated.

How to Build and Run
--------------------
1. Open the solution/project in Visual Studio.
2. Ensure you have included all necessary source files.
3. Build the project (Ctrl+Shift+B).
4. Run the compiled executable as Administrator.

   - You can do this by right-clicking the .exe and selecting "Run as administrator" or by starting Visual Studio as Administrator and running from there.

5. After execution, you will find four files in the same directory as the executable:
   - `historical_network_log.txt`
   - `summary_network_log.txt`
   - `event_id_reference.txt`
   - `network_summary.csv`

Troubleshooting
---------------
- If the program reports "Failed to open log files," ensure you have write permissions in the output directory.
- If you see "Failed to query" messages, make sure you are running as Administrator.
- If you see "Firewall log not found," ensure you have enabled logging as described above and that the log has been populated by network activity.
- Some Event IDs may be labeled as "undocumented" or "unknown"; these are rare or system-specific events not covered in public documentation.

Customization
-------------
- To add more Event IDs or descriptions, edit the `eventIdDescriptions` map in the source code.
- You can adjust the time window by changing the value of `twoWeeksAgo` in the code.
- The CSV output currently uses "Unknown Adapter" for device name; you can enhance this by mapping IPs to adapters using Windows APIs.
