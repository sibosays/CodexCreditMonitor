# Codex Credit Monitor

**A portable Windows system-tray companion for understanding local Codex usage at a glance.**

Codex Credit Monitor turns local Codex session records into a clear dashboard for credit balance, rate-limit windows, model activity, and recent sessions. It runs quietly in the Windows system tray and never changes your ChatGPT or Codex account.

> This is an independent community utility. It is not an official OpenAI product and is not an official billing meter.

## At a glance

![Codex Credit Monitor dashboard showing the available credit balance, five-hour and weekly usage, activity totals, and recent sessions.](https://raw.githubusercontent.com/sibosays/CodexCreditMonitor/57d14f52c0f454fe03fe0026510bb139ed70935c/assets/dashboard-overview.png)

*The dashboard presents local Codex usage in one compact Windows system-tray companion.*

## Download

Download the current portable package directly from this repository:

**[Download Codex Credit Monitor Portable v2.0 (ZIP)](https://github.com/sibosays/CodexCreditMonitor/raw/refs/heads/main/downloads/CodexCreditMonitorPortable_2.0.zip)**

The repository deliberately contains product information only. The application source code is not published here.

## Install and start

1. Download `CodexCreditMonitorPortable_2.0.zip` using the link above.
2. Right-click the ZIP file and choose **Extract All**.
3. Open the extracted `CodexCreditMonitorPortable` folder.
4. Run `CodexCreditMonitorPortable.exe`.
5. Keep the folder intact. The app stores its portable data in its own `Data` folder.

For an integrity check, compare the SHA-256 value in [SHA256SUMS.txt](downloads/SHA256SUMS.txt) with the downloaded ZIP file. Because this is an independent Windows app, SmartScreen may initially identify it as an unfamiliar publisher; only proceed after confirming that you downloaded it from this repository and, if needed, verifying the checksum.

## What it shows

- Available credit balance, current five-hour window, and weekly usage.
- Model activity, processed tokens, and recent sessions from today.
- On-demand or scheduled refreshes with a configurable interval.
- Optional visible and audible warnings at 75% and 90% of the five-hour window, when the remaining balance reaches 25 or 10 credits, and when credits are consumed unusually quickly (12 credits in 30 minutes or 30 credits in two hours).
- A clear in-app status when the included weekly allowance is exhausted and credits are in use, plus the locally observed credit-use pace.
- Your configured automatic-recharge threshold and target as context only; the app never changes that setting for you.
- English and Dutch application interfaces, selectable in **Settings**.

## Privacy and scope

Codex Credit Monitor reads the signed-in Windows user's local Codex session records. It does not send that data elsewhere. Cloud tasks, activity from other devices, and delayed balance updates may not appear immediately.

## Feedback

For a problem or improvement idea, please open a GitHub issue with your Windows version, app version, and steps to reproduce the behaviour.
