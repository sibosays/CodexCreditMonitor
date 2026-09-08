# Codex Credit Monitor

A lightweight, privacy-focused Windows system-tray companion that turns local Codex session data into a clear and practical usage dashboard.

![Codex Credit Monitor v2.2 dashboard showing the reset status and contextual credit actions](assets/dashboard-v2.2.png)

## Why use it?

Codex Credit Monitor keeps the signals that matter during a working session in one compact view. It helps you decide whether to continue, slow down, wait for the next allowance reset, add credits, or start using an available credit balance.

At a glance, you can see:

- Your available credit balance.
- Usage in the active 5-hour window and the weekly window.
- The live 5-hour reset countdown and remaining allowance inside the credit card.
- Today's model activity, processed tokens, and recent sessions.
- Locally observed credit-use pace and optional alerts, with the last reliable pace retained during quiet periods and after restarts.
- The exhausted-allowance status and the credit-use pace remain visible together when credits are in use.

## How it works

The application runs quietly in the Windows system tray. It reads the signed-in Windows user's local Codex session records and summarizes them in the dashboard. New local activity can trigger an update, and you can also refresh manually or choose a refresh interval.

No account credentials are requested. No usage data is uploaded, and the application does not call a billing service. Codex Credit Monitor is an independent local monitor, not an official OpenAI billing meter.

## Dashboard and contextual actions

The dashboard combines credits, allowances, today's activity, and recent sessions without crowding the layout.

- **Add credits** appears only when the available balance is 50 credits or lower.
- **Use credits** appears only when the included weekly allowance is exhausted and a positive credit balance is available.
- Newly relevant actions briefly pulse to attract attention without permanently animating the interface.
- Both actions open ChatGPT Usage & Billing. The application never purchases credits or changes ChatGPT settings by itself.

## Alerts

Optional local notifications cover 75% and 90% 5-hour-window usage, low credit balances, and unusually rapid credit use. Notifications use the same usage and balance values shown on the dashboard.

## Download v2.2

Choose the package that matches how you use Windows:

### Windows Installer (MSI)

[Download CodexCreditMonitorSetup_2.2_x64.msi](https://github.com/sibosays/CodexCreditMonitor/releases/latest/download/CodexCreditMonitorSetup_2.2_x64.msi)

Use the MSI for a conventional 64-bit Windows installation. Windows requests administrator approval, then installs the self-contained desktop application in Program Files and adds a Codex Credit Monitor shortcut to the Start menu.

### PortableApps package (ZIP)

[Download CodexCreditMonitorPortable_2.2.zip](https://github.com/sibosays/CodexCreditMonitor/releases/latest/download/CodexCreditMonitorPortable_2.2.zip)

The portable edition has been tested successfully inside the PortableApps.com Platform. Extract the complete ZIP archive and either run `CodexCreditMonitorPortable.exe` directly or add its folder to PortableApps.com Platform.

The portable package:

- Follows the PortableApps directory structure.
- Includes its launcher, AppInfo metadata, and application icon.
- Keeps application settings in its own `Data` folder.
- Creates no permanent Windows startup entry.
- Can be moved as one complete folder.

Both downloads are self-contained for 64-bit Windows and require no separate .NET Desktop Runtime.

## Settings and controls

From the system tray you can open the dashboard, refresh immediately, change settings, view product information, or exit. Settings include language, alerts, sound, and refresh interval. The standard Windows edition can also launch at Windows sign-in.

The Info window dynamically reads the bundled Dutch and English product description. It scrolls gently and pauses while you point at the text.

## Verify your download

[Download SHA256SUMS.txt](https://github.com/sibosays/CodexCreditMonitor/releases/latest/download/SHA256SUMS.txt) and compare the SHA-256 value of your chosen package:

```text
9C19B3EACD2D858E7FB5C71E1C2E4BF5B358425FEC915BC7133F639FF7B71E5C  CodexCreditMonitorSetup_2.2_x64.msi
A7B5018F26E24E3A783D98D26F3902C76CE2A7437354CD4B19EDD5DD40D3FE4B  CodexCreditMonitorPortable_2.2.zip
```

## What is new in v2.2?

- Live 5-hour reset countdown and remaining allowance inside the credit card.
- Reliable usage-pace persistence and recovery from recent local history.
- Credit-use pace remains visible alongside the exhausted-allowance status.
- Professional product-focused Info view instead of change history.
- Gentle automatic Info scrolling with pause-on-hover behavior.
- Self-contained MSI and PortableApps delivery without a separate .NET dependency.
- Rebuilt and CRC-verified PortableApps launcher for reliable platform startup.

## Privacy and scope

Codex Credit Monitor reads local Codex session records only and sends no data anywhere. Cloud tasks, activity on other devices, and delayed account updates may differ until those values are written locally.

---

Concept and development: **C.S.K. (Simon) Bouwens**  
© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance

