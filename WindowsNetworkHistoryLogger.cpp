#include <windows.h>
#include <winevt.h>
#include <iostream>
#include <string>
#include <regex>
#include <unordered_map>

#pragma comment(lib, "wevtapi.lib")

// Helper: Extract XML tag content
std::wstring ExtractXmlTag(const std::wstring& xml, const std::wstring& tag) {
    std::wregex rgx(L"<" + tag + L"[^>]*>(.*?)</" + tag + L">");
    std::wsmatch match;
    if (std::regex_search(xml, match, rgx)) {
        return match[1].str();
    }
    return L"";
}

// Helper: Extract attribute value from XML tag
std::wstring ExtractXmlAttribute(const std::wstring& xml, const std::wstring& tag, const std::wstring& attr) {
    std::wregex rgx(L"<" + tag + L"[^>]*\\b" + attr + L"=['\"]([^'\"]+)['\"][^>]*>");
    std::wsmatch match;
    if (std::regex_search(xml, match, rgx)) {
        return match[1].str();
    }
    return L"";
}

// Extract arbitrary <Data Name='fieldName'>value</Data> from EventData
std::wstring ExtractEventDataField(const std::wstring& xml, const std::wstring& fieldName) {
    std::wregex rgx(L"<Data Name='" + fieldName + L"'>(.*?)</Data>");
    std::wsmatch match;
    if (std::regex_search(xml, match, rgx)) {
        return match[1].str();
    }
    return L"";
}

// Map Level number to string
std::wstring LevelToString(const std::wstring& level) {
    if (level == L"1") return L"Critical";
    if (level == L"2") return L"Error";
    if (level == L"3") return L"Warning";
    if (level == L"4") return L"Information";
    if (level == L"0") return L"Log Always";
    return L"Unknown";
}

// Structures for different event types
struct NetworkEventInfo {
    std::wstring providerName;
    std::wstring eventID;
    std::wstring level;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring protocol;
    std::wstring sourceIp;
    std::wstring sourcePort;
    std::wstring destinationIp;
    std::wstring destinationPort;
    // Add new WiFi connection fields
    std::wstring ssid;
    std::wstring interfaceGuid;
    std::wstring macAddress;
    std::wstring bssType;
    int frequency = 1;
};

struct DnsQueryEventInfo {
    std::wstring eventID;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring queryName;
    std::wstring queryStatus;
    int frequency = 1;
};

struct LogonEventInfo {
    std::wstring eventID;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring accountName;
    std::wstring accountDomain;
    std::wstring logonType;
    std::wstring ipAddress;
    int frequency = 1;
};

struct ProcessEventInfo {
    std::wstring eventID;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring processName;
    std::wstring processId;
    std::wstring parentProcessId;
    std::wstring commandLine;
    int frequency = 1;
};

struct SecurityChangeEventInfo {
    std::wstring eventID;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring policyChangeDescription;
    int frequency = 1;
};

struct SystemErrorEventInfo {
    std::wstring eventID;
    std::wstring timeCreated;
    std::wstring computer;
    std::wstring message;
    int frequency = 1;
};

// Generate unique keys for aggregation
std::wstring MakeNetworkEventKey(const NetworkEventInfo& info) {
    return info.providerName + L"|" + info.eventID + L"|" +
        info.protocol + L"|" + info.sourceIp + L":" + info.sourcePort + L"->" +
        info.destinationIp + L":" + info.destinationPort + L"|" +
        info.ssid + L"|" + info.interfaceGuid + L"|" + info.macAddress;
}

std::wstring MakeDnsQueryEventKey(const DnsQueryEventInfo& info) {
    return info.eventID + L"|" + info.queryName + L"|" + info.queryStatus;
}

std::wstring MakeLogonEventKey(const LogonEventInfo& info) {
    return info.eventID + L"|" + info.accountName + L"|" + info.logonType + L"|" + info.ipAddress;
}

std::wstring MakeProcessEventKey(const ProcessEventInfo& info) {
    return info.eventID + L"|" + info.processName + L"|" + info.processId + L"|" + info.parentProcessId;
}

std::wstring MakeSecurityChangeEventKey(const SecurityChangeEventInfo& info) {
    return info.eventID + L"|" + info.policyChangeDescription;
}

std::wstring MakeSystemErrorEventKey(const SystemErrorEventInfo& info) {
    return info.eventID + L"|" + info.message;
}

// Parsing functions for each event type
bool ParseNetworkEvent(EVT_HANDLE hEvent, NetworkEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.providerName = ExtractXmlAttribute(xml, L"Provider", L"Name");
                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.level = ExtractXmlTag(xml, L"Level");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");

                outInfo.protocol = ExtractEventDataField(xml, L"Protocol");
                if (!outInfo.protocol.empty()) {
                    outInfo.sourceIp = ExtractEventDataField(xml, L"SourceIp");
                    outInfo.sourcePort = ExtractEventDataField(xml, L"SourcePort");
                    outInfo.destinationIp = ExtractEventDataField(xml, L"DestinationIp");
                    outInfo.destinationPort = ExtractEventDataField(xml, L"DestinationPort");
                }

                // Extract WiFi connection information
                outInfo.ssid = ExtractEventDataField(xml, L"SSID");
                outInfo.interfaceGuid = ExtractEventDataField(xml, L"InterfaceGuid");
                outInfo.macAddress = ExtractEventDataField(xml, L"MacAddress");
                outInfo.bssType = ExtractEventDataField(xml, L"BSSType");

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

bool ParseDnsQueryEvent(EVT_HANDLE hEvent, DnsQueryEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");
                outInfo.queryName = ExtractEventDataField(xml, L"QueryName");
                outInfo.queryStatus = ExtractEventDataField(xml, L"QueryStatus");

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

bool ParseLogonEvent(EVT_HANDLE hEvent, LogonEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");
                outInfo.accountName = ExtractEventDataField(xml, L"TargetUserName");
                outInfo.accountDomain = ExtractEventDataField(xml, L"TargetDomainName");
                outInfo.logonType = ExtractEventDataField(xml, L"LogonType");
                outInfo.ipAddress = ExtractEventDataField(xml, L"IpAddress");

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

bool ParseProcessEvent(EVT_HANDLE hEvent, ProcessEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");
                outInfo.processName = ExtractEventDataField(xml, L"NewProcessName");
                outInfo.processId = ExtractEventDataField(xml, L"ProcessId");
                outInfo.parentProcessId = ExtractEventDataField(xml, L"ParentProcessId");
                outInfo.commandLine = ExtractEventDataField(xml, L"CommandLine");

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

bool ParseSecurityChangeEvent(EVT_HANDLE hEvent, SecurityChangeEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");

                // For simplicity, just capture all EventData as a concatenated string
                // You can customize to extract specific fields per event ID
                std::wregex dataRg(L"<Data>(.*?)</Data>");
                std::wsmatch match;
                std::wstring allData;
                std::wstring xmlCopy = xml;
                while (std::regex_search(xmlCopy, match, dataRg)) {
                    allData += match[1].str() + L"; ";
                    xmlCopy = match.suffix().str();
                }
                outInfo.policyChangeDescription = allData;

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

bool ParseSystemErrorEvent(EVT_HANDLE hEvent, SystemErrorEventInfo& outInfo) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;
    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                outInfo.eventID = ExtractXmlTag(xml, L"EventID");
                outInfo.timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                outInfo.computer = ExtractXmlTag(xml, L"Computer");

                // Extract message or description
                outInfo.message = ExtractXmlTag(xml, L"RenderingInfo");
                if (outInfo.message.empty()) {
                    // fallback: extract all data fields concatenated
                    std::wregex dataRg(L"<Data>(.*?)</Data>");
                    std::wsmatch match;
                    std::wstring allData;
                    std::wstring xmlCopy = xml;
                    while (std::regex_search(xmlCopy, match, dataRg)) {
                        allData += match[1].str() + L"; ";
                        xmlCopy = match.suffix().str();
                    }
                    outInfo.message = allData;
                }

                return true;
            }
            else {
                delete[] buffer;
            }
        }
    }
    return false;
}

// Print functions for each event type
void PrintNetworkEventWithFrequency(const NetworkEventInfo& info) {
    std::wcout << L"--- Network Event ---\n";
    std::wcout << L"Provider: " << info.providerName << L"\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Level: " << LevelToString(info.level) << L" (" << info.level << L")\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";

    if (!info.protocol.empty()) {
        std::wcout << L"Protocol: " << info.protocol << L"\n";
        std::wcout << L"Source IP: " << info.sourceIp << L"\n";
        std::wcout << L"Source Port: " << info.sourcePort << L"\n";
        std::wcout << L"Destination IP: " << info.destinationIp << L"\n";
        std::wcout << L"Destination Port: " << info.destinationPort << L"\n";
    }

    // Print WiFi connection information
    if (!info.ssid.empty()) {
        std::wcout << L"Network SSID: " << info.ssid << L"\n";
    }
    if (!info.interfaceGuid.empty()) {
        std::wcout << L"Interface GUID: " << info.interfaceGuid << L"\n";
    }
    if (!info.macAddress.empty()) {
        std::wcout << L"Local MAC Address: " << info.macAddress << L"\n";
    }
    if (!info.bssType.empty()) {
        std::wcout << L"BSS Type: " << info.bssType << L"\n";
    }

    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

void PrintDnsQueryEventWithFrequency(const DnsQueryEventInfo& info) {
    std::wcout << L"--- DNS Query Event ---\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";
    std::wcout << L"Query Name: " << info.queryName << L"\n";
    std::wcout << L"Query Status: " << info.queryStatus << L"\n";
    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

void PrintLogonEventWithFrequency(const LogonEventInfo& info) {
    std::wcout << L"--- Logon Event ---\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";
    std::wcout << L"Account Name: " << info.accountName << L"\n";
    std::wcout << L"Account Domain: " << info.accountDomain << L"\n";
    std::wcout << L"Logon Type: " << info.logonType << L"\n";
    std::wcout << L"IP Address: " << info.ipAddress << L"\n";
    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

void PrintProcessEventWithFrequency(const ProcessEventInfo& info) {
    std::wcout << L"--- Process Event ---\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";
    std::wcout << L"Process Name: " << info.processName << L"\n";
    std::wcout << L"Process ID: " << info.processId << L"\n";
    std::wcout << L"Parent Process ID: " << info.parentProcessId << L"\n";
    if (!info.commandLine.empty())
        std::wcout << L"Command Line: " << info.commandLine << L"\n";
    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

void PrintSecurityChangeEventWithFrequency(const SecurityChangeEventInfo& info) {
    std::wcout << L"--- Security Change Event ---\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";
    std::wcout << L"Policy Change Description: " << info.policyChangeDescription << L"\n";
    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

void PrintSystemErrorEventWithFrequency(const SystemErrorEventInfo& info) {
    std::wcout << L"--- System/Application Error Event ---\n";
    std::wcout << L"Event ID: " << info.eventID << L"\n";
    std::wcout << L"Time Created (UTC): " << info.timeCreated << L"\n";
    std::wcout << L"Computer: " << info.computer << L"\n";
    std::wcout << L"Message: " << info.message << L"\n";
    std::wcout << L"Frequency: " << info.frequency << L"\n\n";
}

//  You need to add the main function

int main() {
    // Containers for event aggregation
    std::unordered_map<std::wstring, NetworkEventInfo> networkEventsMap;
    std::unordered_map<std::wstring, DnsQueryEventInfo> dnsEventsMap;
    std::unordered_map<std::wstring, LogonEventInfo> logonEventsMap;
    std::unordered_map<std::wstring, ProcessEventInfo> processEventsMap;
    std::unordered_map<std::wstring, SecurityChangeEventInfo> securityChangeEventsMap;
    std::unordered_map<std::wstring, SystemErrorEventInfo> systemErrorEventsMap;

    // 1. Network and Firewall Events (System and Security logs)
    LPCWSTR networkQuery =
        L"*[System[EventID=10010 or EventID=4001 or EventID=4004 or EventID=4946 or EventID=4947 or EventID=4948 or EventID=5156 or EventID=5157]]";

    EVT_HANDLE hNetworkResults = EvtQuery(NULL, L"System", networkQuery, EvtQueryChannelPath);
    if (!hNetworkResults) {
        std::wcerr << L"EvtQuery for network events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hNetworkResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                NetworkEventInfo info;
                if (ParseNetworkEvent(events[i], info)) {
                    std::wstring key = MakeNetworkEventKey(info);
                    auto it = networkEventsMap.find(key);
                    if (it != networkEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        networkEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hNetworkResults);
    }

    // 2. DNS Queries (Microsoft-Windows-DNS-Client/Operational)
    LPCWSTR dnsQuery =
        L"*[System[EventID=3008 or EventID=3009]]";

    EVT_HANDLE hDnsResults = EvtQuery(NULL, L"Microsoft-Windows-DNS-Client/Operational", dnsQuery, EvtQueryChannelPath);
    if (!hDnsResults) {
        std::wcerr << L"EvtQuery for DNS events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hDnsResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                DnsQueryEventInfo info;
                if (ParseDnsQueryEvent(events[i], info)) {
                    std::wstring key = MakeDnsQueryEventKey(info);
                    auto it = dnsEventsMap.find(key);
                    if (it != dnsEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        dnsEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hDnsResults);
    }

    // 3. User Logon/Logoff Events (Security log)
    LPCWSTR logonQuery =
        L"*[System[EventID=4624 or EventID=4625 or EventID=4634 or EventID=4648 or EventID=4672]]";

    EVT_HANDLE hLogonResults = EvtQuery(NULL, L"Security", logonQuery, EvtQueryChannelPath);
    if (!hLogonResults) {
        std::wcerr << L"EvtQuery for logon events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hLogonResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                LogonEventInfo info;
                if (ParseLogonEvent(events[i], info)) {
                    std::wstring key = MakeLogonEventKey(info);
                    auto it = logonEventsMap.find(key);
                    if (it != logonEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        logonEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hLogonResults);
    }

    // 4. Process Creation/Termination Events (Security log)
    LPCWSTR processQuery =
        L"*[System[EventID=4688 or EventID=4689]]";

    EVT_HANDLE hProcessResults = EvtQuery(NULL, L"Security", processQuery, EvtQueryChannelPath);
    if (!hProcessResults) {
        std::wcerr << L"EvtQuery for process events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hProcessResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                ProcessEventInfo info;
                if (ParseProcessEvent(events[i], info)) {
                    std::wstring key = MakeProcessEventKey(info);
                    auto it = processEventsMap.find(key);
                    if (it != processEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        processEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hProcessResults);
    }

    // 5. Security Changes (Security log)
    LPCWSTR securityChangeQuery =
        L"*[System[EventID=4719 or (EventID>=4720 and EventID<=4726)]]";

    EVT_HANDLE hSecurityChangeResults = EvtQuery(NULL, L"Security", securityChangeQuery, EvtQueryChannelPath);
    if (!hSecurityChangeResults) {
        std::wcerr << L"EvtQuery for security change events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hSecurityChangeResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                SecurityChangeEventInfo info;
                if (ParseSecurityChangeEvent(events[i], info)) {
                    std::wstring key = MakeSecurityChangeEventKey(info);
                    auto it = securityChangeEventsMap.find(key);
                    if (it != securityChangeEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        securityChangeEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hSecurityChangeResults);
    }

    // 6. System/Application Errors (System and Application logs)
    LPCWSTR systemErrorQuery = L"*[(System/EventID=1001)]"; // You can expand this query as needed

    EVT_HANDLE hSystemErrorResults = EvtQuery(NULL, L"System", systemErrorQuery, EvtQueryChannelPath);
    if (!hSystemErrorResults) {
        std::wcerr << L"EvtQuery for system error events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;
        while (true) {
            if (!EvtNext(hSystemErrorResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) break;
                std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                break;
            }
            for (DWORD i = 0; i < returned; i++) {
                SystemErrorEventInfo info;
                if (ParseSystemErrorEvent(events[i], info)) {
                    std::wstring key = MakeSystemErrorEventKey(info);
                    auto it = systemErrorEventsMap.find(key);
                    if (it != systemErrorEventsMap.end()) {
                        it->second.frequency++;
                    }
                    else {
                        systemErrorEventsMap[key] = info;
                    }
                }
                EvtClose(events[i]);
            }
        }
        EvtClose(hSystemErrorResults);
    }

    // Print all aggregated events

    std::wcout << L"=== NETWORK EVENTS ===\n\n";
    for (const auto& pair : networkEventsMap) {
        PrintNetworkEventWithFrequency(pair.second);
    }

    std::wcout << L"=== DNS QUERY EVENTS ===\n\n";
    for (const auto& pair : dnsEventsMap) {
        PrintDnsQueryEventWithFrequency(pair.second);
    }

    std::wcout << L"=== USER LOGON/LOGOFF EVENTS ===\n\n";
    for (const auto& pair : logonEventsMap) {
        PrintLogonEventWithFrequency(pair.second);
    }

    std::wcout << L"=== PROCESS CREATION/TERMINATION EVENTS ===\n\n";
    for (const auto& pair : processEventsMap) {
        PrintProcessEventWithFrequency(pair.second);
    }

    std::wcout << L"=== SECURITY CHANGE EVENTS ===\n\n";
    for (const auto& pair : securityChangeEventsMap) {
        PrintSecurityChangeEventWithFrequency(pair.second);
    }

    std::wcout << L"=== SYSTEM/APPLICATION ERROR EVENTS ===\n\n";
    for (const auto& pair : systemErrorEventsMap) {
        PrintSystemErrorEventWithFrequency(pair.second);
    }

    return 0;
}
