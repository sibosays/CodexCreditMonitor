# Codex Credit Monitor

![Codex Credit Monitor v2.2 dashboard](assets/dashboard-v2.2.png)

Codex Credit Monitor is a lightweight Windows system-tray companion for understanding local Codex usage at a glance. It runs quietly in the background, reads only local Codex session records for the signed-in Windows user, and never sends data anywhere.

## What it shows

- Available credit balance and your configured automatic-recharge context.
- Current 5-hour and weekly allowance usage.
- The real reset time for the current 5-hour window and the allowance still remaining.
- Today's model events, processed tokens, and recent sessions.
- Local credit-use pace when credits are being consumed.

## Contextual actions

The dashboard keeps its layout compact. When relevant, it shows small contextual actions beside the fixed header controls:

- **Add credits** appears only when the available credit balance is 50 credits or lower.
- **Use credits** appears when the included weekly allowance is exhausted while a positive credit balance is available.

Both actions open ChatGPT Usage & Billing. The monitor never purchases credits or changes ChatGPT settings by itself.

## Alerts and refresh

Optional local warnings can appear for 75% and 90% 5-hour-window usage, low credit balances, and unusually rapid credit use. The app watches local session activity and refreshes on demand or at a configurable interval. When the dashboard is closed, a manual refresh can show a concise result near the system tray.

## Privacy

This is an independent community utility, not an official OpenAI billing meter. It reads local Codex session data only. Cloud tasks, other devices, and delayed account updates can differ from what is observed locally.

## Download and run

Download the current **Portable ZIP** from the [latest release](https://github.com/sibosays/CodexCreditMonitor/releases/latest), extract it completely, and run `CodexCreditMonitorPortable.exe`.

The v2.2 Windows and PortableApps packages are self-contained 64-bit builds: no separate .NET Desktop Runtime is required. The PortableApps edition stores its settings in its own `Data` folder and deliberately does not offer Windows startup.

## Verify the download

Check the ZIP against `SHA256SUMS.txt` before running it. The release always contains the current Portable ZIP, its checksum, and the dashboard screenshot.

## v2.2

- Adds the 5-hour reset time and remaining allowance to the dashboard status area.
- Replaces the Info history view with a professional, current product description that gently scrolls and pauses while you read it.
- Delivers the application without a .NET runtime dependency.

© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance