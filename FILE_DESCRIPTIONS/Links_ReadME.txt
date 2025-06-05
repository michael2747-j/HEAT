# HEAT App Links Page  
**Files Covered:**  
- `Links.xaml`  
- `Links.xaml.cs`  

---

## Overview

This component provides a visually engaging, easy-to-navigate page for the HEAT app, displaying:
- **Product links** (with images, names, prices, and external URLs)
- **Media & Social links** (YouTube, Twitter, LinkedIn, Facebook)
- **Cybersecurity awareness resources** (OWASP, CISA, and more)

It is implemented as a **UserControl** in WinUI 3, supporting data binding and a modern, responsive layout.

---

## Table of Contents

1. [Purpose](#purpose)
2. [Features](#features)
3. [File Structure](#file-structure)
4. [How It Works](#how-it-works)
5. [Customization](#customization)
6. [Extending Functionality](#extending-functionality)
7. [Design Notes](#design-notes)
8. [Authors & History](#authors--history)

---

## Purpose

The Links page is designed to:
- **Showcase products** relevant to HEAT users, with up-to-date pricing and links.
- **Promote official media and social channels** for user engagement.
- **Encourage cybersecurity best practices** by linking to trusted resources.

---

## Features

- **Modern UI:** Clean, accessible, and visually appealing layout.
- **Data Binding:** Product links are data-driven for easy updates and future integration with APIs.
- **Responsive Layout:** Uses `VariableSizedWrapGrid` for product links, adapting to different window sizes.
- **External Links:** All hyperlinks open in the default browser.
- **Separation of Concerns:** UI (XAML) and logic/data (C#) are cleanly separated.

---

## File Structure

```
Links.xaml        // UI layout and styling
Links.xaml.cs     // Data models and logic for populating the UI
```

---

## How It Works

### 1. UI Layout (`Links.xaml`)

- **Grid Layout:** Organizes the page into three rows: product section, media/social links, and cybersecurity resources.
- **Product Links:**  
  - Displayed in a horizontal wrap grid (max 3 rows).
  - Each product shows an image, name (as a clickable hyperlink), and price.
- **Media & Social Links:**  
  - Prominent links to YouTube, Twitter, LinkedIn, and Facebook.
- **Cybersecurity Awareness:**  
  - Trusted resource links for user education.

**Example UI Hierarchy:**
```
Grid
 ├─ Product Links Title
 ├─ ItemsControl (Product Links)
 └─ StackPanel
     ├─ Media & Social Title
     ├─ Social Links
     ├─ Cybersecurity Awareness Title
     └─ Cybersecurity Resource Links
```

### 2. Data & Logic (`Links.xaml.cs`)

- **ProductLink Model:**  
  - `Name` (string): Product name
  - `Link` (string): URL to product
  - `Price` (string): Product price (can be updated dynamically)
  - `Image` (string): Image URL
- **ObservableCollection:**  
  - Holds all product entries for data binding.
- **Initialization:**  
  - Sample products are added in the constructor.
  - `DataContext` is set for XAML binding.

---

## Customization

### Adding/Editing Product Links

To add or modify products:
```csharp
ProductLinks.Add(new ProductLink {
    Name = "New Product",
    Link = "https://supplier.com/product",
    Price = "Price: $99.99",
    Image = "https://image.url/product.png"
});
```
Or, replace the sample data with dynamic data loading as needed.

### Updating Media/Social Links

Edit the `` elements in `Links.xaml` under the "Media & Social" section.

### Updating Cybersecurity Links

Edit the `` elements in `Links.xaml` under the "Cybersecurity Awareness" section.

---

## Extending Functionality

- **Dynamic Product Data:**  
  Replace hardcoded sample data with API calls or database queries for real-time updates.
- **Click Analytics:**  
  Track hyperlink clicks for engagement metrics.
- **Localization:**  
  Use resource files for multi-language support.
- **Theming:**  
  Adjust colors and styles for dark/light mode compatibility.

---

## Design Notes

- **Accessibility:**  
  - All text uses sufficient contrast and appropriate font sizes.
  - Hyperlinks are underlined and clearly distinguishable.
- **Performance:**  
  - Uses lightweight controls and data binding for efficient UI updates.
- **Maintainability:**  
  - Clear separation between UI and logic.
  - Well-commented code for easy onboarding.

---

## Authors & History

- **Anthony Samen** (UI Design, XAML)
- **Michael Melles** (Data Model, Logic, C#)
- **Created:** June 3, 2025
- **Last Modified:** June 4, 2025

---

## Quick Start

1. **Add both files to your WinUI 3 project.**
2. **Reference the `Links` UserControl in your main window or page:**
   ```xml
   
   ```
3. **Customize product, social, and resource links as needed.**
4. **Build and run!**

---