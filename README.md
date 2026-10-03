# Codex Credit Monitor

A lightweight, privacy-focused Windows system-tray companion that turns local Codex session data into a clear and practical usage dashboard.

![Codex Credit Monitor v2.5 dashboard showing the reset status and contextual credit actions](https://github.com/sibosays/CodexCreditMonitor/releases/download/v2.5/dashboard-v2.5.png)

## Why use it?

Codex Credit Monitor keeps the signals that matter during a working session in one compact view. It helps you decide whether to continue, slow down, wait for the next allowance reset, add credits, or start using an available credit balance.

At a glance, you can see:

- Your available credit balance.
- Usage in the active 5-hour window and the weekly window.
- The live 5-hour reset countdown and remaining allowance inside the credit card.
- Today's model activity and recent sessions, plus today's processed tokens for your whole account.
- Locally observed credit-use pace and optional alerts, with timely active-window analysis and a quiet-period snapshot for a useful dashboard.
- The exhausted-allowance status and the credit-use pace remain visible together when credits are in use.

## How it works

The application runs quietly in the Windows system tray. It asks the locally installed Codex for the current credit balance and allowance windows, so the dashboard stays current even when no Codex work thread is active. Today's activity and recent sessions come from the signed-in Windows user's local Codex session records. The header shows whether the figures are live or come from the last local data. You can also refresh manually or choose a refresh interval.

No account credentials are requested, read, or stored: Codex fetches the limits with its own sign-in. No usage data is uploaded. Codex Credit Monitor is an independent local monitor, not an official OpenAI billing meter.

## Dashboard and contextual actions

The dashboard combines credits, allowances, today's activity, and recent sessions without crowding the layout.

- **Add credits** appears only when the available balance is 50 credits or lower.
- **Use credits** appears only when the included weekly allowance is exhausted and a positive credit balance is available.
- Newly relevant actions briefly pulse to attract attention without permanently animating the interface.
- Both actions open ChatGPT Usage & Billing. The application never purchases credits or changes ChatGPT settings by itself.

## Alerts

Optional local notifications cover 75% and 90% 5-hour-window usage, unusually rapid credit use, and a low credit balance at the warning and critical levels you choose in Settings. The tray icon shows an amber or red dot while the balance is low. Notifications use the same usage and balance values shown on the dashboard.

## Download v2.5

Current build: **2.5.0 · build 10 · 3 October 2026**. Bug fixes keep version 2.5; the build number identifies the exact package and is shown in the application's Info window.

Choose the package that matches how you use Windows:

### Windows Installer (MSI)

[Download CodexCreditMonitorSetup_2.5_x64.msi](https://github.com/sibosays/CodexCreditMonitor/releases/latest/download/CodexCreditMonitorSetup_2.5_x64.msi)

Use the MSI for a conventional 64-bit Windows installation. Windows requests administrator approval, then installs the self-contained desktop application in Program Files and adds a Codex Credit Monitor shortcut to the Start menu.

### PortableApps package (ZIP)

[Download CodexCreditMonitorPortable_2.5.zip](https://github.com/sibosays/CodexCreditMonitor/releases/latest/download/CodexCreditMonitorPortable_2.5.zip)

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
AA599CECCA1A9BC5C889CB101A665AF1E7937CA4C2D898461AC22B46846EA78F  CodexCreditMonitorSetup_2.5_x64.msi
ADFAD42840D447DA8E7F2A8EE16ED6B963674767770B11E5E5B72A0C47A97934  CodexCreditMonitorPortable_2.5.zip
```

## What is new in v2.5?

- Live credit balance, 5-hour limit, and weekly limit through the locally installed Codex, also when no Codex work thread is active. Failed reads retry automatically.
- A header status that shows whether the figures are live, or come from the last local data because Codex is signed out, not found, or unreachable.
- Today's processed tokens for your whole account, including other devices and cloud tasks.
- An unused 5-hour window shows that it starts with your next request instead of a fictitious countdown; weekly usage shows its reset time.
- Today's totals count all of today's local sessions, and a custom Codex folder (`CODEX_HOME`) is respected.
- Adjustable balance warnings (by default at 50 and 15 credits) that alert once per crossing and re-arm after a top-up; the tray icon shows an amber or red dot.
- A History window with 14 or 30 days of daily usage, balance, and weekly usage, plus CSV export.
- The build number appears in the Info window.
- Smaller downloads, and every build is checked by automated tests.

## Earlier in v2.2

- Live 5-hour reset countdown and remaining allowance inside the credit card.
- Timely local alerts from a bounded active observation window, while the dashboard retains the latest known quiet-period limit snapshot.
- Reliable limit refreshes when the newest snapshot is in an older-looking session file or before a large local log record.
- A pre-recharge credit-use pace clears when the local balance recovers, preventing an old alarming rate from being shown as current.
- Credit-use pace remains visible alongside the exhausted-allowance status.
- Professional product-focused Info view instead of change history.
- Gentle automatic Info scrolling with pause-on-hover behavior.
- Self-contained MSI and PortableApps delivery without a separate .NET dependency.
- Rebuilt and CRC-verified PortableApps launcher for reliable platform startup.

## Privacy and scope

Codex Credit Monitor requests the current balance and limits through the locally installed Codex and reads local Codex session records for today's activity. It never reads or stores credentials and uploads no usage data. Today's model events and recent sessions cover this computer; today's token total covers your whole account.

---

Concept and development: **C.S.K. (Simon) Bouwens**  
© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance

