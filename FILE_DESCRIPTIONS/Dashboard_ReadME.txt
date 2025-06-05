# HEAT Dashboard README

Welcome to the HEAT Dashboard! This README provides a comprehensive guide to the structure, usage, and customization of the dashboard, as defined by the `Dashboard.xaml` and `Dashboard.xaml.cs` files.

---

## Table of Contents

1. [Overview](#overview)
2. [File Structure & Purpose](#file-structure--purpose)
3. [UI Layout & Features](#ui-layout--features)
4. [Data Model & CSV Parsing](#data-model--csv-parsing)
5. [Tab Navigation & Table Rendering](#tab-navigation--table-rendering)
6. [Column Visibility](#column-visibility)
7. [Export & Delete Actions](#export--delete-actions)
8. [Customization](#customization)
9. [Troubleshooting](#troubleshooting)
10. [Credits](#credits)

---

## Overview

The HEAT Dashboard is a WinUI-based user interface for visualizing and managing network configuration data parsed from a CSV file (`STATE_SAVE.csv`). It features tabbed navigation, dynamic data tables, column selection, and actions for exporting or deleting the analysis state.

---

## File Structure & Purpose

### Dashboard.xaml

- **Purpose:** Declares the UI layout, styles, and templates for the dashboard.
- **Key Components:**
  - Tab navigation bar for switching between data sections.
  - Data table area with customizable columns.
  - "All Tables" view for displaying all sections.
  - Action buttons for exporting and deleting configuration.

### Dashboard.xaml.cs

- **Purpose:** Implements the logic for tab switching, CSV parsing, data binding, export, and user actions.
- **Key Components:**
  - Data model (`FlatRow`) for table rows.
  - CSV parsing and sectioning logic.
  - Event handlers for tab clicks, column selection, export, and delete actions.
  - UI update and dialog display logic.

---

## UI Layout & Features

### Tabs

- **Tabs:** Each tab corresponds to a network configuration section (e.g., Total Network, LAN, WAN, NAT, DHCP, etc.).
- **All Tables Tab:** Displays all parsed sections in a scrollable view.

### Table Area

- **Single Table View:** Shows data for the selected section, with a header and rows.
- **All Tables View:** Lists all sections and their data in sequence.

### Action Buttons

- **Export VyOS Configuration:** Copies the `STATE_SAVE.csv` to `STATE_SAVE_EXPORT.csv` on the desktop.
- **Delete Analysis Save State:** Deletes the `STATE_SAVE.csv` from the desktop and clears the dashboard.

### Styles

- **PrimaryButtonStyle:** Blue accent, rounded, animated on hover/press.
- **TabButtonStyle:** Flat, blue-bordered, animated for selection.
- **SelectedTabButtonStyle:** Filled blue background for selected tab.
- **Table Row Styling:** Alternating row backgrounds for readability.

---

## Data Model & CSV Parsing

### CSV Structure

- The dashboard expects a CSV file (`STATE_SAVE.csv`) on the user's desktop.
- The CSV is sectioned by lines ending with a colon (`:`), which act as headers for each data section.
- Each section contains:
  - **Header Row:** List of column names.
  - **Data Rows:** Values corresponding to headers.

### Data Model

- **FlatRow:** Represents a single row in the table, with up to 10 columns (`Col0` to `Col9`).
- **Section Storage:** Parsed as a dictionary mapping section names to tuples of (headers, rows).

### Parsing Logic

- **Section Detection:** Lines ending with `:` are treated as new sections.
- **Header Parsing:** The first non-empty line after a section header is used as column headers.
- **Row Parsing:** Subsequent lines are parsed as data rows until the next section or end of file.
- **CSV Line Parsing:** Handles quoted values and commas within quotes.

---

## Tab Navigation & Table Rendering

### Tab-to-Section Mapping

- Each tab button is mapped to a section title in the CSV.
- Clicking a tab updates the table area to show the corresponding section's data.

### Table Rendering

- **Headers:** Displayed using the `FlatRow.FromHeaders()` method.
- **Rows:** Displayed using the `FlatRow.FromDictionary()` method, mapping CSV values to columns.
- **All Tables View:** Iterates through all sections, rendering each with headers and rows.

---

## Column Visibility

- **Default:** All columns in a section are visible.
- **Customization:** Users can select which columns to show via a flyout with toggle switches.
- **Constraints:** At least one column must remain visible at all times.
- **Persistence:** Column visibility is reset when switching sections.

---

## Export & Delete Actions

### Export VyOS Configuration

- **Action:** Copies `STATE_SAVE.csv` to `STATE_SAVE_EXPORT.csv` on the desktop.
- **Feedback:** Shows a dialog on success or error.

### Delete Analysis Save State

- **Action:** Deletes `STATE_SAVE.csv` from the desktop.
- **UI Update:** Clears all data from the dashboard.
- **Feedback:** Shows a dialog on success or error.

---

## Customization

### Adding New Tabs/Sections

1. **Update XAML:** Add a new tab button in the tab grid.
2. **Update Mapping:** Add the tab name and section title to the `TabToSectionTitle` dictionary in code-behind.
3. **CSV:** Ensure the corresponding section exists in the CSV.

### Changing Styles

- Edit the relevant `` resources in `Dashboard.xaml` for buttons, tabs, or table rows.

### Changing Column Count

- The UI and data model currently support up to 10 columns per section.
- To support more columns, update:
  - The `FlatRow` class (add more `ColN` properties).
  - The XAML templates for rows and headers (add more `` and `` elements).

---

## Troubleshooting

### "STATE_SAVE.csv not found on Desktop."

- Ensure the file exists on the desktop.
- The dashboard will not display data without this file.

### Table appears empty

- The selected section in the CSV may have no data.
- Check the CSV file for correct section headers and data rows.

### Export/Delete actions fail

- Check file permissions on the desktop.
- Ensure the file is not open in another application.

### UI/Rendering Issues

- Ensure all required columns are present in each section.
- If adding new columns or tabs, update both XAML and code-behind accordingly.

---

## Credits

- **Author:** Anthony Samen
- **Created:** June 2, 2025
- **Last Modified:** June 5, 2025

---

## Appendix: Quick Reference

### Key Classes & Methods

- **FlatRow:** Data model for table rows.
- **ParseStateSaveCsvWithColonHeaders:** Parses the CSV into sections.
- **ShowSectionForTab:** Updates table based on selected tab.
- **RenderAllTablesTab:** Renders all sections in the "All Tables" view.
- **ExportVyOS_Click:** Handles export action.
- **DeleteSaveState_Click:** Handles delete action.

### Main UI Controls

- **TabGrid:** Contains tab navigation buttons.
- **MainTableListView:** Displays the current section's table.
- **AllTablesStackPanel:** Displays all sections when "All Tables" is selected.

---