# Codex Credit Monitor

**A portable Windows system-tray companion for understanding local Codex usage at a glance.**

Codex Credit Monitor turns local Codex session records into a clear dashboard for credit balance, rate-limit windows, model activity, and recent sessions. It runs quietly in the Windows system tray and never changes your ChatGPT or Codex account.

> This is an independent community utility. It is not an official OpenAI product and is not an official billing meter.

## Download

Download the current portable package from the [latest release](https://github.com/sibosays/CodexCreditMonitor/releases/latest):

**[Download Codex Credit Monitor Portable v2.0 (ZIP)](https://github.com/sibosays/CodexCreditMonitor/releases/download/v2.0/CodexCreditMonitorPortable_2.0.zip)**

The repository deliberately contains product information only. The application source code is not published here.

## Install and start

1. Download `CodexCreditMonitorPortable_2.0.zip` from the release above.
2. Right-click the ZIP file and choose **Extract All**.
3. Open the extracted `CodexCreditMonitorPortable` folder.
4. Run `CodexCreditMonitorPortable.exe`.
5. Keep the folder intact. The app stores its portable data in its own `Data` folder.

For an integrity check, compare the SHA-256 value in `SHA256SUMS.txt` with the downloaded ZIP file. Because this is an independent Windows app, SmartScreen may initially identify it as an unfamiliar publisher; only proceed after confirming that you downloaded it from this GitHub release and, if needed, verifying the checksum.

## What it shows

- Available credit balance, current five-hour window, and weekly usage.
- Model activity, processed tokens, and recent sessions from today.
- On-demand or scheduled refreshes with a configurable interval.
- Optional visual and audio warnings at high five-hour usage.
- Your configured automatic-recharge threshold and target as context only; the app never changes that setting for you.
- English and Dutch application interfaces, selectable in **Settings**.

## Privacy and scope

Codex Credit Monitor reads the signed-in Windows user's local Codex session records. It does not send that data elsewhere. Cloud tasks, activity from other devices, and delayed balance updates may not appear immediately.

## Feedback

For a problem or improvement idea, please open a GitHub issue with your Windows version, app version, and steps to reproduce the behaviour.
