#include <windows.h>
#include <winevt.h>
#include <iostream>
#include <string>
#include <regex>

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

// Print network-related events from System log
void PrintNetworkEvent(EVT_HANDLE hEvent) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;

    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                std::wstring providerName = ExtractXmlAttribute(xml, L"Provider", L"Name");
                std::wstring eventID = ExtractXmlTag(xml, L"EventID");
                std::wstring level = ExtractXmlTag(xml, L"Level");
                std::wstring timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                std::wstring computer = ExtractXmlTag(xml, L"Computer");

                std::wcout << L"--- Network Event ---\n";
                std::wcout << L"Provider: " << providerName << L"\n";
                std::wcout << L"Event ID: " << eventID << L"\n";
                std::wcout << L"Level: " << LevelToString(level) << L" (" << level << L")\n";
                std::wcout << L"Time Created (UTC): " << timeCreated << L"\n";
                std::wcout << L"Computer: " << computer << L"\n";

                // Extract network protocol related fields if present
                std::wstring protocol = ExtractEventDataField(xml, L"Protocol");
                if (!protocol.empty()) {
                    std::wcout << L"Protocol: " << protocol << L"\n";
                    std::wcout << L"Source IP: " << ExtractEventDataField(xml, L"SourceIp") << L"\n";
                    std::wcout << L"Source Port: " << ExtractEventDataField(xml, L"SourcePort") << L"\n";
                    std::wcout << L"Destination IP: " << ExtractEventDataField(xml, L"DestinationIp") << L"\n";
                    std::wcout << L"Destination Port: " << ExtractEventDataField(xml, L"DestinationPort") << L"\n";
                }

                std::wcout << L"\n";
            }
            else {
                std::wcerr << L"EvtRender failed with " << GetLastError() << L"\n";
                delete[] buffer;
            }
        }
        else {
            std::wcerr << L"EvtRender failed with " << GetLastError() << L"\n";
        }
    }
}

// Print firewall rule change events from Security log (4946, 4947, 4948)
void PrintFirewallRuleChangeEvent(EVT_HANDLE hEvent) {
    DWORD bufferSize = 0, bufferUsed = 0, propertyCount = 0;

    if (!EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, NULL, &bufferUsed, &propertyCount)) {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            bufferSize = bufferUsed;
            wchar_t* buffer = new wchar_t[bufferSize / sizeof(wchar_t)];
            if (EvtRender(NULL, hEvent, EvtRenderEventXml, bufferSize, buffer, &bufferUsed, &propertyCount)) {
                std::wstring xml(buffer);
                delete[] buffer;

                std::wstring eventID = ExtractXmlTag(xml, L"EventID");
                std::wstring timeCreated = ExtractXmlAttribute(xml, L"TimeCreated", L"SystemTime");
                std::wstring computer = ExtractXmlTag(xml, L"Computer");

                std::wcout << L"--- Firewall Rule Change Event ---\n";
                std::wcout << L"Event ID: " << eventID << L"\n";
                std::wcout << L"Time Created (UTC): " << timeCreated << L"\n";
                std::wcout << L"Computer: " << computer << L"\n";

                // Common fields for firewall rule changes
                std::wstring profileChanged = ExtractEventDataField(xml, L"ProfileChanged");
                std::wstring ruleId = ExtractEventDataField(xml, L"RuleId");
                std::wstring ruleName = ExtractEventDataField(xml, L"RuleName");
                std::wstring origin = ExtractEventDataField(xml, L"Origin");
                std::wstring active = ExtractEventDataField(xml, L"Active");
                std::wstring direction = ExtractEventDataField(xml, L"Direction");
                std::wstring profiles = ExtractEventDataField(xml, L"Profiles");
                std::wstring action = ExtractEventDataField(xml, L"Action");
                std::wstring applicationPath = ExtractEventDataField(xml, L"ApplicationPath");
                std::wstring serviceName = ExtractEventDataField(xml, L"ServiceName");
                std::wstring protocol = ExtractEventDataField(xml, L"Protocol");
                std::wstring securityOptions = ExtractEventDataField(xml, L"SecurityOptions");
                std::wstring edgeTraversal = ExtractEventDataField(xml, L"EdgeTraversal");
                std::wstring modifyingUser = ExtractEventDataField(xml, L"ModifyingUser");
                std::wstring modifyingApplication = ExtractEventDataField(xml, L"ModifyingApplication");
                std::wstring policyAppId = ExtractEventDataField(xml, L"PolicyAppId");
                std::wstring errorCode = ExtractEventDataField(xml, L"ErrorCode");

                std::wcout << L"Profile Changed: " << profileChanged << L"\n";
                std::wcout << L"Rule ID: " << ruleId << L"\n";
                std::wcout << L"Rule Name: " << ruleName << L"\n";
                if (!origin.empty()) std::wcout << L"Origin: " << origin << L"\n";
                if (!active.empty()) std::wcout << L"Active: " << active << L"\n";
                if (!direction.empty()) std::wcout << L"Direction: " << direction << L"\n";
                if (!profiles.empty()) std::wcout << L"Profiles: " << profiles << L"\n";
                if (!action.empty()) std::wcout << L"Action: " << action << L"\n";
                if (!applicationPath.empty()) std::wcout << L"Application Path: " << applicationPath << L"\n";
                if (!serviceName.empty()) std::wcout << L"Service Name: " << serviceName << L"\n";
                if (!protocol.empty()) std::wcout << L"Protocol: " << protocol << L"\n";
                if (!securityOptions.empty()) std::wcout << L"Security Options: " << securityOptions << L"\n";
                if (!edgeTraversal.empty()) std::wcout << L"Edge Traversal: " << edgeTraversal << L"\n";
                if (!modifyingUser.empty()) std::wcout << L"Modifying User: " << modifyingUser << L"\n";
                if (!modifyingApplication.empty()) std::wcout << L"Modifying Application: " << modifyingApplication << L"\n";
                if (!policyAppId.empty()) std::wcout << L"PolicyAppId: " << policyAppId << L"\n";
                if (!errorCode.empty()) std::wcout << L"Error Code: " << errorCode << L"\n";

                std::wcout << L"\n";
            }
            else {
                std::wcerr << L"EvtRender failed with " << GetLastError() << L"\n";
                delete[] buffer;
            }
        }
        else {
            std::wcerr << L"EvtRender failed with " << GetLastError() << L"\n";
        }
    }
}

int wmain() {
    // Query network-related events from System log
    LPCWSTR networkQuery =
        L"*[System[EventID=10010 or EventID=4001 or EventID=4004 or EventID=3 or EventID=4000 or EventID=4005 or EventID=4006]]";

    EVT_HANDLE hNetworkResults = EvtQuery(NULL, L"System", networkQuery, EvtQueryChannelPath);
    if (!hNetworkResults) {
        std::wcerr << L"EvtQuery for network events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;

        while (true) {
            if (!EvtNext(hNetworkResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) {
                    break;
                }
                else {
                    std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                    break;
                }
            }

            for (DWORD i = 0; i < returned; i++) {
                PrintNetworkEvent(events[i]);
                EvtClose(events[i]);
            }
        }
        EvtClose(hNetworkResults);
    }

    // Query firewall rule change events from Security log (4946, 4947, 4948)
    LPCWSTR firewallQuery =
        L"*[System[EventID=4946 or EventID=4947 or EventID=4948 or EventID=2010 or EventID=2097]]";

    EVT_HANDLE hFirewallResults = EvtQuery(NULL, L"Security", firewallQuery, EvtQueryChannelPath);
    if (!hFirewallResults) {
        std::wcerr << L"EvtQuery for firewall events failed with " << GetLastError() << L"\n";
    }
    else {
        EVT_HANDLE events[10];
        DWORD returned = 0;

        while (true) {
            if (!EvtNext(hFirewallResults, 10, events, INFINITE, 0, &returned)) {
                if (GetLastError() == ERROR_NO_MORE_ITEMS) {
                    break;
                }
                else {
                    std::wcerr << L"EvtNext failed with " << GetLastError() << L"\n";
                    break;
                }
            }

            for (DWORD i = 0; i < returned; i++) {
                PrintFirewallRuleChangeEvent(events[i]);
                EvtClose(events[i]);
            }
        }
        EvtClose(hFirewallResults);
    }

    return 0;
}
