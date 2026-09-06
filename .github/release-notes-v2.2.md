## Codex Credit Monitor v2.2

![Codex Credit Monitor v2.2 dashboard](https://github.com/sibosays/CodexCreditMonitor/releases/download/v2.2/dashboard-v2.2.png)

Codex Credit Monitor is a lightweight Windows system-tray companion for understanding local Codex usage at a glance. It works quietly in the background, reads only local Codex session records for the signed-in Windows user, and sends no data anywhere.

### What v2.2 adds

- The dashboard status area shows the real reset time for the current 5-hour window and the allowance still remaining.
- The Info window now contains a professional, current product description instead of change history. It scrolls gently and pauses while you read it.
- The standard Windows app and PortableApps package are self-contained 64-bit builds. No separate .NET Desktop Runtime is required.

### Dashboard and actions

The dashboard shows available credit balance, automatic-recharge context, current 5-hour and weekly usage, model events, processed tokens, recent sessions, and locally observed credit-use pace.

**Add credits** appears only when the available balance reaches 50 credits or lower. **Use credits** appears when the included weekly allowance is exhausted while a positive credit balance is available. Both actions open ChatGPT Usage & Billing; the monitor never purchases credits or changes settings itself.

### Alerts and privacy

Optional local warnings cover 75% and 90% 5-hour-window usage, low credit balances, and unusually rapid credit use. This is an independent community utility, not an official OpenAI billing meter. Cloud tasks, other devices, and delayed account updates can differ from locally observed data.

### Download

Download `CodexCreditMonitorPortable_2.2.zip`, extract it completely, and run `CodexCreditMonitorPortable.exe`. The PortableApps edition keeps its settings in its own `Data` folder.

Verify the ZIP with `SHA256SUMS.txt` before running it.

© 2026 C.S.K. (Simon) Bouwens · Built with AI assistance