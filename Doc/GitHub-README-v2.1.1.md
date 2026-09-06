# Codex Credit Monitor

**A portable Windows system-tray companion for understanding local Codex usage at a glance.**

Created and maintained by C.S.K. (Simon) Bouwens.

Codex Credit Monitor turns local Codex session records into a clear dashboard for credit balance, rate-limit windows, model activity, and recent sessions. It runs quietly in the Windows system tray and never changes your ChatGPT or Codex account.

> This is an independent community utility. It is not an official OpenAI product and is not an official billing meter.

## At a glance

![Codex Credit Monitor v2.1.1 dashboard with balanced card margins.](assets/dashboard-v2.1.1.png)

*The v2.1.1 dashboard uses a consistent card grid with equal outer margins.*

## Download

**[Download Codex Credit Monitor Portable v2.1.1 (ZIP)](https://github.com/sibosays/CodexCreditMonitor/raw/refs/heads/main/downloads/CodexCreditMonitorPortable_2.1.1.zip)**

This repository contains the current product information and portable package only. The application source code is not published here.

## Install and start

1. Download `CodexCreditMonitorPortable_2.1.1.zip`.
2. Right-click the ZIP file and choose **Extract All**.
3. Open the extracted `CodexCreditMonitorPortable` folder.
4. Run `CodexCreditMonitorPortable.exe`.
5. Keep the folder intact. The app stores its portable data in its own `Data` folder.

Verify the downloaded ZIP against [SHA256SUMS.txt](downloads/SHA256SUMS.txt) before running it. Because this is an independent Windows app, SmartScreen may initially identify it as an unfamiliar publisher; only proceed after confirming the download source and checksum.

## What it shows

- Available credit balance, current five-hour window, and weekly usage.
- Model activity, processed tokens, and recent sessions from today.
- On-demand or scheduled refreshes with a configurable interval.
- Optional visible and audible warnings for high usage, low credit balance, and unusually rapid credit spending.
- Clear usage, balance, and rapid-credit-use warning toasts with the relevant value, status, and guidance.
- A clear in-app status when the included weekly allowance is exhausted and credits are in use.
- Compact **Add credits** and **Use credits** shortcuts when they are relevant. Both open ChatGPT's Usage & Billing page; the app never purchases credits or changes account settings.
- English and Dutch application interfaces, selectable in **Settings**.

## Privacy and scope

Codex Credit Monitor reads the signed-in Windows user's local Codex session records. It does not send that data elsewhere. Cloud tasks, activity from other devices, and delayed balance updates may not appear immediately.

## Feedback

For a problem or improvement idea, please open a GitHub issue with your Windows version, app version, and steps to reproduce the behaviour.

## Copyright

Copyright © 2026 C.S.K. (Simon) Bouwens. All rights reserved.
