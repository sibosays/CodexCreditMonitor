# Codex Credit Monitor

**A portable Windows system-tray companion for understanding local Codex usage at a glance.**

Codex Credit Monitor keeps a lightweight view of the Codex activity available on the current Windows device. It runs quietly in the system tray and turns local session records into a clear dashboard, optional usage alerts, and a compact refresh summary.

> This is an independent community utility. It is not an official OpenAI product and it does not change your ChatGPT or Codex account.

## Download

| Current version | Package | Platform |
| --- | --- | --- |
| 2.0 | [CodexCreditMonitorPortable_2.0.paf.exe](downloads/CodexCreditMonitorPortable_2.0.paf.exe) | 64-bit Windows / PortableApps |

The installer checksum is available in [SHA256SUMS.txt](SHA256SUMS.txt).

## What it shows

- Available credit balance, current five-hour window, and weekly usage.
- Improved reading of credit balance and usage windows when Codex writes additional rate-limit records.
- Model activity, processed tokens, and recent sessions from today.
- On-demand or scheduled refreshes with a configurable interval.
- Optional visual and audio warnings at high five-hour usage.
- Your configured automatic-recharge threshold and target as context only; the app never changes that setting for you.

## Install and start

1. Download the `.paf.exe` package above.
2. Open it with the [PortableApps Platform](https://portableapps.com/).
3. Choose an install location and launch **Codex Credit Monitor** from the platform.
4. Find the app in the Windows system tray; use its menu to open the dashboard, refresh, configure settings, or exit.

The portable edition keeps its settings inside its own `Data` folder. It deliberately does not offer Windows startup, so it does not make a persistent change to the host computer.

## Privacy and scope

Codex Credit Monitor reads the signed-in Windows user's local Codex session records. It does not send that data elsewhere. It is a convenience view rather than an official billing meter: cloud tasks, activity from other devices, and delayed balance updates may not appear immediately.

## Languages

The application supports English and Dutch. Select the preferred language in **Settings**.

## Source availability

This repository is intentionally **installer-only** for now. It contains the public PortableApps package and product information, not the application's source code.

## Feedback

If you encounter an issue or have an idea, please open a GitHub issue with your Windows version, the app version, and steps to reproduce the behaviour.
