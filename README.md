# 🛡️ bralti dns

> **Official Website:** [https://bralti.com](https://bralti.com)  
> **Source Base:** Fork of [GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) by [ValdikSS](https://github.com/ValdikSS)  
> **License:** [Apache License 2.0](LICENSE) (Core & GUI) & [LGPLv3/GPLv3](licenses/LICENSE-windivert.txt) (WinDivert)  
> **Supported Platforms:** Windows 10 & Windows 11 (64-bit and 32-bit)

---

## 📌 Project Overview & Fork Disclaimer

**bralti dns** is a lightweight, open-source Windows application designed to bypass **Deep Packet Inspection (DPI)** censorship and **DNS hijacking** imposed by Internet Service Providers (ISPs).

> [!IMPORTANT]
> **Fork Notice:**  
> **bralti dns** is an enhanced, graphical distribution and fork of **[GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI)**, created by **ValdikSS**.  
> While the underlying engine relies on the core DPI evasion techniques developed in GoodbyeDPI, this fork introduces:
> 1. **Modern Windows GUI (C# .NET):** Clean, dark-mode visual interface replacing complex batch files.
> 2. **Zero-Click Auto-Start:** Protection starts immediately upon launching the application.
> 3. **Windows System Tray Integration (⌃):** Minimizes quietly to the notification area with native Windows toast notifications.
> 4. **1-Click Windows Service Mode:** Run the protection continuously in the background on system boot without any open window.
> 5. **Windows Startup Integration:** Option to launch the GUI minimized into the tray upon user login.
> 6. **Pre-Tuned Turkey Defaults:** Out-of-the-box configuration tailored specifically for Turkish ISPs (Mode `-5`, TTL 5, Yandex DNS via Port `1253`).
> 7. **Unified Single Executable:** Everything packaged cleanly into `BraltiDNS.exe`.

---

## ⚡ Built with "Vibe Coding": Human Vision, AI-Engineered

**bralti dns** is a real-world case study in the modern **"Vibe Coding"** paradigm. 

The developer behind **[bralti.com](https://bralti.com)** served as the **Project Director & Architect**, driving and prompting AI agents to solve the persistent pain points and usability flaws that the original GoodbyeDPI suffered from for years:

### ⚠️ Pain Points of the Original Project:
* **Scattered Batch Scripts:** Required navigating a confusing folder full of `.cmd` files with no guidance.
* **Intimidating Console Window:** Kept an open command prompt in the taskbar that could be accidentally closed.
* **No Visual Feedback:** Installing or removing the background service required running separate scripts with no clear status or error reporting.
* **Manual Setup on Every Boot:** Required manually finding and clicking the script every time the PC restarted.
* **Confusing DNS & Hop Configuration:** Everyday users had to decipher complex command-line arguments to bypass Turkish ISP blocks.

### 💡 The Director's Solutions via Vibe Coding:
By providing clear product requirements, architectural vision, and iterative feedback, the director guided the AI to:
1. **Consolidate into One Executable:** Merged all helper scripts and wrappers into a single `BraltiDNS.exe`.
2. **Design an Overlap-Free Responsive GUI:** Crafted a dark-mode card layout with structured `TableLayoutPanel` spacing that never overlaps at any display scaling.
3. **Automate Zero-Click Protection:** Made evasion activate instantly on app launch without needing to press anything.
4. **Implement System Tray & Native Toasts:** Enable running silently in the background (⌃) with system notifications.
5. **Create a Unified Dynamic Service Button:** A single smart button that automatically detects service state and toggles between Install and Remove.
6. **Add Windows Startup Integration:** Enabled 1-click startup registration directly in the Windows Registry.

---

## 🎯 Who is this for? (Target Audience & Use Cases)

**bralti dns** is specifically designed for users who want unrestricted, uncensored internet access without the severe drawbacks of traditional VPNs:

1. **Internet Users in Turkey (Primary Audience):**
   * Circumvents widespread ISP blocks on platforms such as **Discord**, **Roblox**, **Wikipedia**, Pastebin, and independent news outlets.
   * Works across all major Turkish providers: **Türk Telekom**, **Turkcell Superonline**, **Vodafone Türkiye**, **Türksat Kablonet**, and smaller regional operators.
2. **Gamers and Streamers:**
   * Traditional VPNs reroute all your traffic through remote servers, adding **50ms to 250ms of ping / latency**, causing stutter and lag in competitive games.
   * **bralti dns adds 0ms ping!** Your traffic connects directly to game servers at maximum wire speed.
3. **Users Who Don't Want Speed Throttling:**
   * VPNs often bottleneck high-speed fiber lines down to 20–50 Mbps.
   * **bralti dns preserves 100% of your native download and upload bandwidth.**
4. **Privacy-Conscious Users:**
   * Unlike free VPNs or public proxies, **no third-party server intercepts or logs your data**. All packet processing occurs locally inside your own network card.

---

## 💡 How It Works: The Engineering Behind DPI Circumvention

To understand why **bralti dns** works when other methods fail, it is important to understand how ISP censorship works:

### 1. How ISPs Block Websites

Modern ISP censorship does not simply block IP addresses (since modern cloud providers like Cloudflare, AWS, and Fastly host thousands of sites on a single IP). Instead, ISPs employ two mechanisms:

```
[Your Computer]
       │
       ▼ (1. Plaintext DNS Query on standard Port 53)
[ISP DNS Resolver] ─────────► [DNS Hijacking / Poisoning] ──► ❌ Returns fake IP or NXDOMAIN
       │
       ▼ (2. Encrypted HTTPS connection begins with TLS ClientHello)
[ISP DPI Appliance] ────────► [Reads SNI: "discord.com"] ──► ❌ Injects fake TCP RST packet to kill connection
```

1. **DNS Hijacking & Poisoning (Port 53):**  
   When your browser looks up a domain name (e.g., `discord.com`), it sends a UDP query to Port 53. Turkish ISPs intercept all Port 53 traffic—even if you manually set your DNS to `1.1.1.1` or `8.8.8.8`—and return false addresses.
2. **SNI Inspection via Deep Packet Inspection (DPI):**  
   Even though HTTPS encrypts web traffic, the initial TLS handshake packet (`TLS ClientHello`) sends the target domain name in plaintext within the **Server Name Indication (SNI)** field. ISP DPI middleboxes inspect this field in real time. If the domain is blacklisted, the DPI appliance instantly injects a spoofed **`TCP RST` (Reset)** packet into the stream, forcing your browser to close the connection.

---

### 2. How `bralti dns` Bypasses DPI (Without a VPN or Proxy)

**bralti dns** runs locally at the Windows kernel level using the Microsoft-signed **WinDivert** driver. It manipulates outgoing network packets so that ISP DPI boxes become blind to them, while the destination server reconstructs them perfectly:

```
[Your Computer: bralti dns + WinDivert]
       │
       ├─► 1. DNS Redirection to Port 1253 (Bypasses Port 53 ISP Hijacking entirely)
       ├─► 2. TCP Segmentation: Splits the TLS ClientHello right through the domain name (e.g., "disc" + "ord.com")
       └─► 3. Low-TTL Decoy: Sends a fake packet with TTL=5 that fools the DPI and drops before reaching the server
       │
       ▼
[ISP DPI Middlebox] ────────► Cannot reassemble split packets in real time ──► Traffic passes unobstructed!
       │
       ▼
[Destination Web Server] ───► Standard TCP stack reassembles chunks seamlessly ──► Connection established! ✅
```

1. **TCP Segmentation / SNI Splitting:**  
   The program splits the `TLS ClientHello` packet into two or more TCP segments right in the middle of the domain name. Because ISP DPI appliances process millions of packets per second, they cannot afford the memory or processing cost to buffer and reassemble split TCP streams on the fly. As a result, the DPI middlebox fails to detect the forbidden domain and lets the packets through. The destination server's standard TCP stack reassembles the fragments effortlessly.
2. **Fake Low-TTL Decoy Packets (`--set-ttl 5`):**  
   The software sends a decoy packet with a low Time-To-Live (TTL) value (e.g., 5 hops). This decoy reaches the ISP's DPI middlebox, satisfies its inspection state, and subsequently expires and dies in the network before reaching the target server. The real packet follows immediately behind it and reaches the destination safely.
3. **Alternative Port DNS Redirection (`Port 1253`):**  
   The tool redirects standard DNS requests to secure, fast DNS resolvers (such as Yandex DNS) operating on non-standard port `1253`. Because Turkish ISPs only monitor and hijack port 53, port 1253 traffic passes completely untouched, delivering genuine, unpoisoned IP addresses.

---

## 📊 Technical Comparison

| Feature | 🛡️ bralti dns | 🔒 Traditional VPN | 🌐 Web Proxy | ⚙️ Manual DNS Change |
|---|---|---|---|---|
| **Internet Speed** | 🚀 **100% Native Line Speed** | 🔻 30%–70% speed drop | 🔻 Severe speed drop | 🚀 Full speed |
| **Added Ping / Latency** | ⚡ **0 ms** (Direct connection) | 🔺 +50ms to +250ms | 🔺 +100ms or more | ⚡ 0 ms |
| **Middleman Server** | ❌ **None** (Local processing) | ⚠️ All traffic traverses VPN | ⚠️ Traverses proxy server | ❌ None |
| **Bypasses Turkish DPI** | ✅ **Yes (Guaranteed)** | ✅ Yes | ⚠️ Fragile & easily blocked | ❌ **No** (Port 53 is hijacked) |
| **Subscription Cost** | 🆓 **100% Free & Open Source** | 💳 $5–$15 / month | 💳 Ad-supported or paid | 🆓 Free |
| **Memory / CPU Footprint** | 🪶 **< 15 MB RAM**, < 0.1% CPU | 🐘 200MB+ RAM, high battery | ⚖️ Moderate | 🪶 Negligible |

---

## 🎛️ Presets & Operating Modes

**bralti dns** includes pre-configured modes designed for different network conditions:

| Preset Name | CLI Parameters | Recommended Usage |
|---|---|---|
| **[Default] Turkey Standard** | `-5 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253` | **Recommended.** Best overall stability and speed across all major Turkish ISPs. |
| **Turkey - Superonline Alt 1** | `-5 --set-ttl 3 --dns-addr 77.88.8.8 --dns-port 1253` | Tuned with lower hop count for Turkcell Superonline fiber connections. |
| **Turkey - Superonline Alt 2** | `-5 --dns-addr 77.88.8.8 --dns-port 1253` | Standard Mode `-5` without TTL modification if your router drops low-TTL packets. |
| **Turkey - Aggressive Mode** | `-9 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253` | Maximum evasion mode; blocks QUIC/HTTP3 to force browsers to use fragmented TCP. |
| **Standard Compatibility** | `-1 --dns-addr 77.88.8.8 --dns-port 1253` | Passive mode for standard compatibility on non-aggressive DPI setups. |
| **Custom Arguments** | User-defined parameters | Allows advanced users and developers to pass custom flags directly to the engine. |

---

## 🚀 Quick Start Guide

### Launching the Application
1. Double-click **[BraltiDNS.exe](BraltiDNS.exe)** (or execute **[Start.cmd](Start.cmd)**).
2. Protection starts **automatically** upon opening (zero clicks required).
3. A Windows balloon notification will confirm that protection is active.
4. Minimize or close the window—it will continue running silently in the **Windows System Tray (⌃)**.

### Permanent Background Protection (Windows Service)
If you want protection to run 24/7 without opening the application window:
1. Open **bralti dns**.
2. Click **📦 Install Windows Service**.
3. The GoodbyeDPI engine will be registered as an official Windows Service that starts automatically on system boot, even before you log in.
4. To uninstall, simply click the same button (which dynamically becomes **🗑️ Remove Windows Service**).

### App Startup with Windows
If you prefer the GUI to launch automatically into your system tray when you log in:
* Click **🚀 Enable App Startup with Windows**.

---

## 📁 Repository Structure

```text
├── BraltiDNS.exe              <-- Unified Windows GUI Executable (Ready to run)
├── Start.cmd                  <-- Quick launcher shortcut
├── NOTICE                     <-- Open source attributions & copyright notices
├── LICENSE                    <-- Apache License 2.0
├── README.md                  <-- Official project documentation
│
├── 📁 gui/                    <-- C# .NET Windows Forms Frontend
│   ├── DPIShield.cs           <-- Main application, tray manager, and service wrapper
│   ├── app.manifest           <-- Administrator privilege manifest (UAC requireAdministrator)
│   └── build_gui.cmd          <-- 1-click build script using built-in Windows csc.exe
│
├── 📁 src/                    <-- Native C Engine Source Code (GoodbyeDPI core)
│   ├── goodbyedpi.c           <-- Modified with Turkey defaults
│   ├── icon.ico               <-- Application icon
│   └── Makefile               <-- MinGW compilation makefile
│
├── 📁 x86_64/                 <-- 64-bit Engine & Microsoft-Signed Drivers
│   ├── WinDivert64.sys        <-- WHQL-signed Windows kernel packet driver
│   ├── WinDivert.dll          <-- WinDivert user-space interface library
│   └── goodbyedpi.exe         <-- 64-bit CLI engine
│
├── 📁 x86/                    <-- 32-bit Engine & Drivers
│   ├── WinDivert32.sys        <-- 32-bit kernel driver
│   ├── WinDivert.dll
│   └── goodbyedpi.exe
│
└── 📁 licenses/               <-- Third-party licenses
    ├── LICENSE-goodbyedpi.txt <-- Apache 2.0 (ValdikSS)
    ├── LICENSE-windivert.txt  <-- GNU LGPLv3 / GPLv3 (basil00)
    ├── LICENSE-uthash.txt     <-- BSD 1-Clause (Troy D. Hanson)
    └── LICENSE-getline.txt    <-- BSD License
```

---

## 🛠️ Building from Source

The graphical interface is built using pure C# targeting `.NET Framework 4.5+`. It can be compiled on **any Windows system without installing Visual Studio**:

1. Ensure `BraltiDNS.exe` is closed.
2. Run `gui\build_gui.cmd` from an administrator prompt.
3. The built-in Windows compiler (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`) will compile `gui\DPIShield.cs` into `BraltiDNS.exe` in seconds.

---

## ⚖️ License & Legal Compliance

This project strictly adheres to all open-source licensing requirements:

* **GoodbyeDPI Core Engine:**  
  Copyright (c) 2017–2024 ValdikSS. Licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).  
  *Under Section 4 of the Apache License, derivative works and forks are explicitly permitted, provided copyright notices, original licenses, and state-of-change notices are preserved.*
* **GUI & Customizations:**  
  Copyright (c) 2026 [bralti.com](https://bralti.com). Licensed under the [Apache License, Version 2.0](LICENSE).
* **WinDivert Kernel Packet Driver:**  
  Copyright (c) 2011–2020 basil00. Licensed under [GNU LGPLv3 / GPLv3](licenses/LICENSE-windivert.txt).
* **uthash:**  
  Copyright (c) 2005–2018 Troy D. Hanson. Licensed under the [BSD 1-Clause License](licenses/LICENSE-uthash.txt).

---

## 🌐 Credits & Acknowledgments

* **Project Director, Vibe Coder & Maintainer:** [bralti.com](https://bralti.com) (Vision, Architecture & AI Prompt Direction)
* **Original GoodbyeDPI Engine:** [ValdikSS](https://github.com/ValdikSS/GoodbyeDPI) (Core C DPI evasion logic)
* **WinDivert Packet Filter:** [basil00](https://reqrypt.org/windivert.html) (Kernel-level packet filtering)
