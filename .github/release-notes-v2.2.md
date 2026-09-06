# Codex Credit Monitor v2.2

Codex Credit Monitor is a lightweight, privacy-focused Windows system-tray application for understanding local Codex usage, credit balance, allowance windows, and recent model activity at a glance.

![Codex Credit Monitor v2.2 dashboard](https://github.com/sibosays/CodexCreditMonitor/releases/download/v2.2/dashboard-v2.2.png)

## Choose your download

- **Windows Installer:** `CodexCreditMonitorSetup_2.2_x64.msi` — installs the self-contained 64-bit application in Program Files and adds a Start menu shortcut.
- **PortableApps package:** `CodexCreditMonitorPortable_2.2.zip` — a complete portable package tested successfully inside the PortableApps.com Platform. It keeps settings in its own Data folder and creates no permanent startup entry.

Neither edition requires a separate .NET Desktop Runtime.

## What the application gives you

- Available credit balance in a compact dashboard.
- Current 5-hour-window usage, weekly usage, and remaining allowance.
- A live countdown to the next 5-hour reset.
- Today's model activity, processed tokens, and recent sessions.
- Contextual **Add credits** and **Use credits** actions that appear only when relevant.
- Optional local alerts for usage thresholds, low credits, and unusually rapid credit use.

## New in v2.2

- Added the real 5-hour reset countdown and remaining allowance to the dashboard status area.
- Reworked Info into a professional product description with gentle automatic scrolling and pause on hover.
- Added a conventional MSI alongside the PortableApps-tested ZIP.
- Kept both deliveries fully self-contained to prevent .NET runtime dependency errors.

## Privacy

The application reads the signed-in Windows user's local Codex session records only. It requests no account credentials, uploads no usage data, and never purchases credits or changes ChatGPT settings. It is an independent local monitor, not an official OpenAI billing meter.

## Integrity

Use `SHA256SUMS.txt` to verify either download. The release screenshot is included as `dashboard-v2.2.png`.

Concept and development: **C.S.K. (Simon) Bouwens**  
© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance
