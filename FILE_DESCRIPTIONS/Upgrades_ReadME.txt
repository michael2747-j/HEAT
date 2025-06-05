# HEAT App Upgrades Module

Welcome to the **HEAT App Upgrades** module! This module provides users with a visually rich interface for exploring, learning about, and selecting advanced network security upgrades from ADAM Networks. It also includes an interactive quiz that recommends the best upgrade based on user needs.

---

## Table of Contents

- [Overview](#overview)
- [Files and Structure](#files-and-structure)
  - [Upgrades.xaml](#upgradesxaml)
  - [Upgrades.xaml.cs](#upgradesxamlcs)
- [Features](#features)
- [Products Displayed](#products-displayed)
- [Quiz Logic](#quiz-logic)
- [Benefits & Protection](#benefits--protection)
- [Customization](#customization)
- [How to Use](#how-to-use)
- [Development & Authors](#development--authors)
- [Troubleshooting](#troubleshooting)
- [License](#license)

---

## Overview

The **Upgrades** module is a user control within the HEAT app that introduces, describes, and helps users select from three advanced network protection products:

- **adam:ONE®**
- **DNSharmony®**
- **adam:GO™**

It combines clear product information, a benefits summary, privacy/protection details, and an interactive quiz for tailored recommendations.

---

## Files and Structure

### Upgrades.xaml

- **Purpose:** UI definition for the upgrades page.
- **Main Sections:**
  - Product cards with images, links, and descriptions.
  - Upgrade recommendation quiz launcher.
  - Benefits summary.
  - Privacy/protection information.

### Upgrades.xaml.cs

- **Purpose:** Implements the interactive quiz logic.
- **Key Responsibilities:**
  - Defines quiz questions and options.
  - Handles quiz navigation and user input.
  - Tallies responses and recommends the best product.
  - Displays results in a dialog.

---

## Features

- **Modern, scrollable UI** with visually distinct product cards.
- **Hyperlinks** to product pages.
- **Interactive quiz** with multi-question dialog flow.
- **Benefits and privacy highlights** for user confidence.
- **Responsive design** for different window sizes.

---

## Products Displayed

### 1. adam:ONE®

- **Description:**  
  Zero Trust connectivity (ZTc) solution for enterprise-grade security. Employs AI-driven allowlisting, patented egress controls (DTTS®), and threat intelligence aggregation (DNSharmony®). Hybrid Muscle-Brain design for decentralized performance and centralized management.
- **Ideal for:** Enterprise, Managed Networks, Compliance  
- **More info:** [adam:ONE®](https://adamnet.works/products/adam_one/)

### 2. DNSharmony®

- **Description:**  
  DNS intelligence aggregation and filtering. Enforces Safe Search, blocks harmful content, and provides real-time visibility. Self-installable on pfSense, ASUS Merlin WRT, and more.
- **Ideal for:** Families, Schools, SMBs  
- **More info:** [DNSharmony®](https://adamnet.works/products/dnsharmony/)

### 3. adam:GO™

- **Description:**  
  Extends Zero Trust to mobile devices via always-on VPN to a dedicated adam:ONE® cloud instance. Integrates with MDM, supports iOS, iPadOS, Windows 10, and macOS.
- **Ideal for:** Mobile Professionals, Travelers  
- **More info:** [adam:GO™](https://adamnet.works/products/adam_go/)

---

## Quiz Logic

- **Purpose:** Guide users to the most suitable upgrade via a series of questions.
- **How it works:**
  - 12 questions covering usage, environment, device types, security needs, and management preferences.
  - Each answer is mapped to one of the three products.
  - At the end, the product with the most selections is recommended.
- **User Experience:**
  - Launched via "Take the Quiz" button.
  - Each question is a dialog with option buttons.
  - User can cancel at any time.

---

## Benefits & Protection

**Benefits of Upgrading:**

- Enhanced malware and phishing protection.
- Advanced firewall and intrusion detection.
- Parental controls and content filtering.
- Real-time threat intelligence updates.
- Priority support and regular feature upgrades.

**Added Protection Privacy:**

- Advanced DNS-layer security.
- Smart routing policies.
- Decentralized control options.
- Compliance readiness.
- Minimal management overhead.

---

## Customization

- **Products:**  
  Add or remove product cards in `Upgrades.xaml` as needed.
- **Quiz:**  
  Update questions and options in `Upgrades.xaml.cs` (`LoadQuizQuestions()` method).
- **Styling:**  
  Modify colors, icons, and layout in `Upgrades.xaml` for branding or accessibility.

---

## How to Use

1. **Browse Upgrades:**  
   Scroll through the available upgrades and read descriptions.

2. **Learn More:**  
   Click product names to visit official product pages.

3. **Take the Quiz:**  
   Click "Take the Quiz" and answer all questions.  
   Receive a tailored recommendation at the end.

4. **Review Benefits:**  
   Explore the benefits and privacy/protection features summarized below the quiz.

---

## Development & Authors

- **Created:** June 2025
- **Authors:**  
  - Anthony Samen (UI/XAML)  
  - Michael Melles (Quiz Logic/C#)

---

## Troubleshooting

- **Quiz not launching:**  
  Ensure event handler `LaunchQuiz_Click` is correctly wired in XAML.
- **Product images missing:**  
  Confirm images are present in `Assets/` and paths are correct.
- **Dialog issues:**  
  The quiz uses `ContentDialog`—ensure your app supports it and is running in a compatible environment (WinUI 3).

---