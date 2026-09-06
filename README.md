# Codex Credit Monitor

A lightweight Windows system-tray companion that turns local Codex session data into a clear, practical usage dashboard.

![Codex Credit Monitor v2.2 dashboard](assets/dashboard-v2.2.png)

## Why use it?

Codex usage information is useful, but it is easy to lose sight of your active allowance while working. Codex Credit Monitor keeps the most relevant signals close at hand so you can decide when to continue, slow down, add credits, or wait for a reset.

At a glance, you can see:

- Your available credit balance.
- Usage in the current 5-hour window and the weekly window.
- How long remains until the 5-hour window resets.
- The allowance still remaining in that window.
- Today's model activity, processed tokens, and recent sessions.
- Whether credits are being consumed unusually quickly.

## How it works

The application runs quietly in the Windows system tray. It reads local Codex session records for the signed-in Windows user and summarizes them in a compact dashboard. New local session activity can trigger an update, and you can also refresh manually or use a configurable refresh interval.

No account credentials are requested. No usage data is uploaded, and the application does not call a billing service. It is an independent local monitor, not an official OpenAI billing meter.

## Dashboard

The dashboard combines four types of information without crowding the layout:

- **Credits** — available balance, automatic-recharge context, and locally observed credit-use pace.
- **Allowances** — current 5-hour and weekly usage, including the real 5-hour reset countdown.
- **Today** — model events and processed tokens for the current day.
- **Sessions** — the most recent local Codex sessions and their activity.

## Contextual credit actions

Two compact actions appear only when they are useful:

- **Add credits** appears when the available balance is 50 credits or lower.
- **Use credits** appears when the included weekly allowance is exhausted and a positive credit balance is available.

Both actions open ChatGPT Usage & Billing. Codex Credit Monitor never purchases credits or changes ChatGPT settings by itself.

## Alerts

Optional local notifications help prevent surprises:

- A usage warning at 75% of the current 5-hour window.
- A stronger warning at 90%.
- Low-credit warnings at the configured critical levels.
- A rapid-use warning when local credit consumption accelerates unusually quickly.

Notifications use the same usage and balance values shown on the dashboard.

## Settings and system-tray controls

From the system tray you can open the dashboard, refresh immediately, change settings, view product information, or exit the monitor. Settings include language, alert behavior, sound, refresh interval, and — for the standard Windows build — launch at Windows sign-in.

The Info window contains the current product description in Dutch or English. It scrolls gently and pauses when you point at the text so it remains easy to read.

## Download

Download the current package from the [latest release](https://github.com/sibosays/CodexCreditMonitor/releases/latest):

1. Download `CodexCreditMonitorPortable_2.2.zip`.
2. Extract the complete ZIP archive.
3. Open the extracted `CodexCreditMonitorPortable` folder.
4. Run `CodexCreditMonitorPortable.exe`.

The application is a self-contained 64-bit Windows build. A separate .NET Desktop Runtime installation is not required.

The PortableApps edition keeps its settings in its own `Data` folder and deliberately does not offer Windows startup, leaving no persistent startup entry on the host computer.

## Verify the download

Use the accompanying `SHA256SUMS.txt` file to verify the Portable ZIP before running it. The current release contains only the current application package, its checksum, and the matching dashboard screenshot, in addition to GitHub's automatic source archives.

## Privacy and scope

Codex Credit Monitor reads local Codex session records only and sends no data anywhere. Because it observes local records, cloud tasks, activity on other devices, and delayed account updates may differ from the dashboard until those values are written locally.

## What is new in v2.2?

- Real 5-hour reset countdown and remaining allowance in the dashboard status area.
- A professional product-focused Info view instead of change history.
- Gentle automatic Info scrolling with pause-on-hover behavior.
- Self-contained Windows and PortableApps delivery without a separate .NET dependency.

---

Concept and development: **C.S.K. (Simon) Bouwens**  
© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance