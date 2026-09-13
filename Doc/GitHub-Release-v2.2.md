# Codex Credit Monitor v2.2

Codex Credit Monitor is a lightweight, privacy-focused Windows system-tray application for understanding local Codex usage, credit balance, allowance windows, and recent model activity at a glance.

![Codex Credit Monitor v2.2 dashboard](https://github.com/sibosays/CodexCreditMonitor/releases/download/v2.2/dashboard-v2.2.png)

## Choose your download

- **Windows Installer:** `CodexCreditMonitorSetup_2.2_x64.msi` — requests administrator approval, installs the self-contained 64-bit application in Program Files, and adds a Start menu shortcut.
- **PortableApps package:** `CodexCreditMonitorPortable_2.2.zip` — a complete portable package tested successfully inside the PortableApps.com Platform. It keeps settings in its own Data folder and creates no permanent startup entry.

Neither edition requires a separate .NET Desktop Runtime.

## What the application gives you

- Available credit balance in a compact dashboard.
- Current 5-hour-window usage, weekly usage, and remaining allowance.
- A live countdown to the next 5-hour reset inside the credit card.
- Today's model activity, processed tokens, and recent sessions.
- Contextual **Add credits** and **Use credits** actions that appear only when relevant.
- Optional local alerts for usage thresholds, low credits, and unusually rapid credit use.
- Persistence and recovery of the last reliable local credit-use pace during quiet periods and after restarts.

## New in v2.2

- Restored timely local alerts by limiting background usage analysis to the active observation window, while retaining the latest known limit snapshot for a quiet dashboard.
- Makes limit refreshes reliable when the newest local snapshot is in an older-looking session file or precedes a large log record, including a queued follow-up for overlapping refresh requests.
- Clears a pre-recharge credit-use pace when the local balance recovers, so an old alarming rate is not presented as current.
- Added the real 5-hour reset countdown and remaining allowance inside the credit card.
- Reworked Info into a professional product description with gentle automatic scrolling and pause on hover.
- Added a conventional MSI alongside the PortableApps-tested ZIP.
- Kept both deliveries fully self-contained to prevent .NET runtime dependency errors.
- Completed eight localization, adaptive-layout, scrolling, limit-status, usage-pace, and process-lifecycle corrections without changing the v2.2 version number.
- Kept the credit-use pace visible beside the exhausted-allowance status, so active credit-backed usage remains understandable at a glance.
- Rebuilt the PortableApps launcher with the official generator and verified its CRC and clean startup path.

## Privacy

The application reads the signed-in Windows user's local Codex session records only. It requests no account credentials, uploads no usage data, and never purchases credits or changes ChatGPT settings. It is an independent local monitor, not an official OpenAI billing meter.

## Integrity

Use `SHA256SUMS.txt` to verify either download. The release screenshot is included as `dashboard-v2.2.png`.

Concept and development: **C.S.K. (Simon) Bouwens**  
© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance
