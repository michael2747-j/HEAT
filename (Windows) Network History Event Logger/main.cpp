#include <windows.h>
#include <winevt.h>
#include <iostream>
#include <fstream>
#include <string>
#include <ctime>
#include <regex>
#include <sstream>
#include <map>
#include <set>
#include <vector>
#include <algorithm>

#pragma comment(lib, "wevtapi.lib")

// Lookup table for common Event IDs (expand as needed)
std::map<int, std::string> eventIdDescriptions = {
    // NetworkProfile/Operational
    {4001, "Network identification started"},
    {4002, "Network identification completed"},
    {4003, "Network location awareness: location change detected"},
    {4004, "Network state change event"},
    {10000, "Network connected"},
    {10001, "Network disconnected"},
    {10002, "Network profile changed"},
    {10003, "Network category changed"},
    {10004, "Network connected (IPv4)"},
    {10005, "Network disconnected (IPv4)"},
    {10006, "Network connected (IPv6)"},
    {10007, "Network disconnected (IPv6)"},
    {10008, "Network profile name changed"},
    // WLAN-AutoConfig/Operational
    {8000, "WLAN AutoConfig service started a connection to a wireless network"},
    {8001, "WLAN connected"},
    {8002, "WLAN disconnected"},
    {8003, "WLAN connection attempt"},
    {8004, "WLAN connection failed"},
    {8005, "WLAN profile applied"},
    {8006, "WLAN profile deleted"},
    {8007, "WLAN scan started"},
    {8008, "WLAN scan finished"},
    {8009, "WLAN authentication succeeded"},
    {8010, "WLAN authentication failed"},
    {8011, "WLAN profile applied"},
    {8012, "WLAN profile deleted"},
    // Security/Authentication/Association
    {11000, "Wireless security started"},
    {11001, "Wireless security succeeded"},
    {11002, "Wireless security failed"},
    {11004, "Wireless security failed"},
    {11005, "Wireless security authentication failed"},
    {11010, "Wireless security key mismatch or other security-related failure"},
    // WLAN Scan/Other
    {20002, "Wireless network scan completed"},
    {20019, "WLAN scan result available (undocumented, likely scan or connection event)"},
    {20020, "WLAN scan result processed (undocumented, likely scan or connection event)"}
};

// Set to collect unique Event IDs
std::set<int> encounteredEventIds;

// Extract EventID from XML and add to set
int ExtractEventId(const std::wstring& xml) {
    std::wregex id_regex(L"<EventID>(\\d+)</EventID>");
    std::wsmatch match;
    if (std::regex_search(xml, match, id_regex) && match.size() > 1) {
        int eventId = std::stoi(match[1].str());
        encounteredEventIds.insert(eventId);
        return eventId;
    }
    return -1;
}

// Extract a summary from event XML (basic)
std::string ExtractSummary(const std::wstring& xml) {
    int eventId = ExtractEventId(xml);
    std::string summary;
    if (eventId != -1) {
        summary = "EventID: " + std::to_string(eventId);
    }
    else {
        summary = "Event";
    }
    // Extract a brief description from the <EventData> or <Data> tag
    std::wregex data_regex(L"<Data[^>]*>(.*?)</Data>");
    std::wsmatch match;
    if (std::regex_search(xml, match, data_regex) && match.size() > 1) {
        summary += " - ";
        std::wstring ws(match[1].first, match[1].second);
        summary += std::string(ws.begin(), ws.end());
    }
    return summary;
}

// Write both full and summary logs
void WriteEventToLog(std::ofstream& log, std::ofstream& summary, const std::wstring& logName, const EVT_HANDLE& event) {
    DWORD bufferSize = 0, propCount = 0;
    EvtRender(NULL, event, EvtRenderEventXml, 0, NULL, &bufferSize, &propCount);
    std::wstring xml(bufferSize / sizeof(wchar_t), L'\0');
    if (EvtRender(NULL, event, EvtRenderEventXml, bufferSize, &xml[0], &bufferSize, &propCount)) {
        // Full log
        log << "Log: " << std::string(logName.begin(), logName.end()) << std::endl;
        log << "Event: " << std::string(xml.begin(), xml.end()) << std::endl << std::endl;
        // Summary log
        summary << ExtractSummary(xml) << std::endl;
    }
}

// Query Windows Event Log by log name and time window
void ExportEvents(const std::wstring& logName, time_t since, std::ofstream& log, std::ofstream& summary) {
    struct tm ptm;
    gmtime_s(&ptm, &since);
    wchar_t query[512];
    swprintf(query, 512, L"*[System[TimeCreated[@SystemTime >= '%04d-%02d-%02dT%02d:%02d:%02d.000Z']]]",
        ptm.tm_year + 1900, ptm.tm_mon + 1, ptm.tm_mday,
        ptm.tm_hour, ptm.tm_min, ptm.tm_sec);

    EVT_HANDLE hResults = EvtQuery(NULL, logName.c_str(), query, EvtQueryReverseDirection);
    if (!hResults) {
        std::wcerr << L"Failed to query " << logName << std::endl;
        return;
    }

    DWORD returned = 0;
    EVT_HANDLE events[10];
    while (EvtNext(hResults, 10, events, INFINITE, 0, &returned)) {
        for (DWORD i = 0; i < returned; ++i) {
            WriteEventToLog(log, summary, logName, events[i]);
            EvtClose(events[i]);
        }
    }
    EvtClose(hResults);
}

// Read Windows Firewall log (if enabled)
void ExportFirewallLog(const std::string& path, std::ofstream& log, std::ofstream& summary) {
    std::ifstream fwlog(path.c_str());
    if (!fwlog.is_open()) return;
    log << "=== Windows Firewall Log Entries ===" << std::endl;
    summary << "=== Windows Firewall Log Entries ===" << std::endl;
    std::string line;
    while (std::getline(fwlog, line)) {
        log << line << std::endl;
        // For summary, only log lines that are not comments
        if (!line.empty() && line[0] != '#') {
            std::string date, time, action, protocol, srcIP, dstIP;
            std::istringstream iss(line);
            iss >> date >> time >> action >> protocol >> srcIP >> dstIP;
            if (!action.empty() && !protocol.empty() && !srcIP.empty() && !dstIP.empty()) {
                summary << "Firewall: " << action << " " << protocol << " " << srcIP << " -> " << dstIP << std::endl;
            }
            else {
                summary << "Firewall: " << line << std::endl;
            }
        }
    }
    log << std::endl;
    summary << std::endl;
    fwlog.close();
}

// Write the event ID reference file
void WriteEventIdReference(const std::string& filename) {
    std::ofstream ref(filename.c_str(), std::ios::out | std::ios::trunc);
    if (!ref.is_open()) {
        std::cerr << "Failed to open event ID reference file." << std::endl;
        return;
    }
    ref << "EventID Reference List\n";
    ref << "----------------------\n";
    for (std::set<int>::const_iterator it = encounteredEventIds.begin(); it != encounteredEventIds.end(); ++it) {
        int id = *it;
        ref << "EventID: " << id << " - ";
        if (eventIdDescriptions.find(id) != eventIdDescriptions.end()) {
            ref << eventIdDescriptions[id];
        }
        else {
            ref << "Unknown network event";
        }
        ref << std::endl;
    }
    ref.close();
}

// ====================
// CSV Summary Section
// ====================

struct ConnSummary {
    std::string deviceName;
    std::string ipOrHost;
    std::set<std::string> srcPorts;
    std::set<std::string> dstPorts;
    std::string protocol;
    int totalFreq;
    long long totalBytes;
    ConnSummary() : totalFreq(0), totalBytes(0) {}
};

std::string join(const std::set<std::string>& s, const std::string& delim) {
    std::string result;
    for (std::set<std::string>::const_iterator it = s.begin(); it != s.end(); ++it) {
        if (it != s.begin()) result += delim;
        result += *it;
    }
    return result;
}

std::string GetPrimaryAdapterName() {
    return "Unknown Adapter";
}

void ExportCSVSummary(const std::string& fwlogPath, const std::string& csvPath) {
    std::ifstream fwlog(fwlogPath.c_str());
    if (!fwlog.is_open()) {
        std::cerr << "Firewall log not found at: " << fwlogPath << std::endl;
        return;
    }

    std::map<std::string, ConnSummary> summaryMap;
    std::string line;
    std::string deviceName = GetPrimaryAdapterName();

    std::map<std::string, int> colIdx;
    bool headerParsed = false;

    while (std::getline(fwlog, line)) {
        if (line.empty()) continue;
        if (line[0] == '#') {
            if (line.find("#Fields:") == 0) {
                std::istringstream iss(line.substr(8));
                std::string col;
                int idx = 0;
                while (iss >> col) {
                    colIdx[col] = idx++;
                }
                headerParsed = true;
            }
            continue;
        }
        if (!headerParsed) continue;

        std::istringstream iss(line);
        std::vector<std::string> cols;
        std::string val;
        while (iss >> val) cols.push_back(val);

        if (colIdx.count("protocol") == 0 || colIdx.count("src-ip") == 0 || colIdx.count("dst-ip") == 0 ||
            colIdx.count("src-port") == 0 || colIdx.count("dst-port") == 0) continue;

        int maxIdx = std::max(std::max(colIdx["protocol"], colIdx["src-ip"]),
            std::max(colIdx["dst-ip"], std::max(colIdx["src-port"], colIdx["dst-port"])));
        if ((int)cols.size() <= maxIdx)
            continue;

        std::string protocol = cols[colIdx["protocol"]];
        std::string srcIP = cols[colIdx["src-ip"]];
        std::string dstIP = cols[colIdx["dst-ip"]];
        std::string srcPort = cols[colIdx["src-port"]];
        std::string dstPort = cols[colIdx["dst-port"]];
        std::string bytes = (colIdx.count("size") && colIdx["size"] < (int)cols.size()) ? cols[colIdx["size"]] : "";

        std::string key = deviceName + "|" + dstIP + "|" + protocol;
        ConnSummary& entry = summaryMap[key];
        entry.deviceName = deviceName;
        entry.ipOrHost = dstIP;
        entry.protocol = protocol;
        entry.srcPorts.insert(srcPort);
        entry.dstPorts.insert(dstPort);
        entry.totalFreq += 1;
        if (!bytes.empty() && std::all_of(bytes.begin(), bytes.end(), [](char c) { return std::isdigit(static_cast<unsigned char>(c)); })) {
            entry.totalBytes += std::stoll(bytes);
        }
    }
    fwlog.close();

    std::ofstream csv(csvPath.c_str());
    csv << "Nickname,Device Name,IP/Hostname,Source Ports,Destination Ports,Protocol,Avg Frequency/sec,Total Frequency,Total Bytes\n";
    for (std::map<std::string, ConnSummary>::const_iterator it = summaryMap.begin(); it != summaryMap.end(); ++it) {
        const ConnSummary& entry = it->second;
        csv << "," // Nickname: not available
            << entry.deviceName << ","
            << entry.ipOrHost << ","
            << join(entry.srcPorts, ";") << ","
            << join(entry.dstPorts, ";") << ","
            << entry.protocol << ","
            << "," // Avg Frequency/sec: not calculated
            << entry.totalFreq << ","
            << entry.totalBytes << "\n";
    }
    csv.close();
    std::cout << "CSV summary exported to " << csvPath << std::endl;
}


int main() {
    // Calculate time 2 weeks ago
    time_t now = time(0);
    time_t twoWeeksAgo = now - (14 * 24 * 60 * 60);

    std::ofstream log("historical_network_log.txt", std::ios::out | std::ios::trunc);
    std::ofstream summary("summary_network_log.txt", std::ios::out | std::ios::trunc);

    if (!log.is_open() || !summary.is_open()) {
        std::cerr << "Failed to open log files." << std::endl;
        return 1;
    }

    log << "=== Network Profile Events ===" << std::endl;
    summary << "=== Network Profile Events ===" << std::endl;
    ExportEvents(L"Microsoft-Windows-NetworkProfile/Operational", twoWeeksAgo, log, summary);

    log << "=== WLAN AutoConfig Events ===" << std::endl;
    summary << "=== WLAN AutoConfig Events ===" << std::endl;
    ExportEvents(L"Microsoft-Windows-WLAN-AutoConfig/Operational", twoWeeksAgo, log, summary);

    ExportFirewallLog("C:\\Windows\\System32\\LogFiles\\Firewall\\pfirewall.log", log, summary);

    log.close();
    summary.close();

    // Write the event ID reference file
    WriteEventIdReference("event_id_reference.txt");

    // Export CSV summary from Windows Firewall log
    ExportCSVSummary("C:\\Windows\\System32\\LogFiles\\Firewall\\pfirewall.log", "network_summary.csv");

    std::cout << "Logs exported to historical_network_log.txt, summary_network_log.txt, event_id_reference.txt, and network_summary.csv" << std::endl;
    return 0;
}
