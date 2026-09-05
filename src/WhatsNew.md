<!-- nl -->
# Codex Credit Monitor 2.1

## Nieuw in 2.1

- **Credits toevoegen** verschijnt alleen bij een saldo van 50 credits of lager. De plusactie opent Usage & Billing in ChatGPT.
- **Credits gebruiken** verschijnt alleen wanneer de inbegrepen weekbundel op is én lokaal positief credittegoed beschikbaar is. Het creditkaartje met bliksem opent Usage & Billing voor extra Codex-gebruik.
- Beide acties hebben tweetalige, meerregelige tooltips. Gebruik `--preview-low-credits`, `--preview-use-credits` of `--preview-credit-actions` om de drie staten lokaal te bekijken.
- Beide acties openen **Usage & Billing** in ChatGPT. De monitor koopt geen credits en wijzigt geen instelling zelfstandig.
- Herstelt het vaste Windows-appicoon in de v2.1-uitvoer. De actuele verbruiksuitlezer accepteert zowel de klassieke als de nieuwe per-limietstructuur uit lokale Codex-logs, zodat saldo en beide verbruiksvensters ook in de vernieuwde toast blijven staan.
- Herstelt ook het zichtbaar openen van het dashboard vanuit de lokale appuitvoer.

## Functionele beschrijving

Codex Credit Monitor is een lichte Windows-systeemvakapp die je lokale Codex-sessies samenvat. De app werkt op de achtergrond, leest geen externe gegevens en stuurt niets door.

- Toont je beschikbare credittegoed, het 5-uursvenster en weekverbruik.
- Herstelt het uitlezen van saldo en verbruiksvensters wanneer Codex aanvullende limietrecords schrijft.
- Geeft inzicht in modelmomenten, verwerkte tokens en recente sessies van vandaag.
- Vernieuwt op aanvraag of periodiek met een rustig, instelbaar interval.
- Geeft optioneel een zichtbare en hoorbare waarschuwing bij 75%/90% verbruik, een laag credittegoed (25 en 10 credits) of uitzonderlijk snel creditverbruik (12 credits in 30 minuten of 30 credits in twee uur).
- Maakt zichtbaar wanneer je inbegrepen weekbundel op is en Codex credits gebruikt; toont daarnaast het lokale creditverbruikstempo.
- Toont een compacte samenvatting bij het systeemvak als je vernieuwt terwijl het dashboard gesloten is.
- Ondersteunt automatische opwaardering als context: de app toont je ingestelde drempel en doel, maar wijzigt ChatGPT nooit zelf.
- Vertaalt het label bij deze context consequent mee met de gekozen app-taal.
- Werkt als gewone Windows-app of als zelfstandige PortableApps-editie; de draagbare editie bewaart instellingen in zijn eigen `Data`-map.

Het dashboard open je vanuit het systeemvak. Instellingen, handmatig vernieuwen en afsluiten zijn daar eveneens beschikbaar.

<!-- en -->
# Codex Credit Monitor 2.1

## New in 2.1

- **Add credits** appears only at a balance of 50 credits or below. The plus action opens Usage & Billing in ChatGPT.
- **Use credits** appears only when the included weekly allowance is exhausted and a positive local credit balance is available. The credit-card-with-lightning action opens Usage & Billing for additional Codex usage.
- Both actions have bilingual, multiline tooltips. Use `--preview-low-credits`, `--preview-use-credits`, or `--preview-credit-actions` to view the three states locally.
- Both actions open **Usage & Billing** in ChatGPT. The monitor never purchases credits or changes a setting on its own.
- Restores the fixed Windows application icon in the v2.1 output. The usage reader accepts both the classic and the new per-limit structure in local Codex logs, keeping the balance and both usage windows in the refreshed toast.
- Also restores visibly opening the dashboard from the local app output.

## Functional overview

Codex Credit Monitor is a lightweight Windows system-tray app that summarises your local Codex sessions. It works in the background, does not read external data, and does not send data anywhere.

- Shows your available credit balance, 5-hour window, and weekly usage.
- Restores balance and usage-window reading when Codex writes additional rate-limit records.
- Provides insight into model events, processed tokens, and today's recent sessions.
- Refreshes on demand or periodically with a quiet, configurable interval.
- Can show a visible and audible warning at 75%/90% usage, when the available credit balance is low (25 and 10 credits), or when credits are being used unusually quickly (12 credits in 30 minutes or 30 credits in two hours).
- Makes it clear when your included weekly allowance is exhausted and Codex is using credits; also shows the locally observed credit-use pace.
- Shows a compact summary near the system tray when you refresh while the dashboard is closed.
- Supports automatic recharge as context: it shows your configured threshold and target, but never changes ChatGPT itself.
- Localizes that label consistently with the selected app language.
- Works as a standard Windows app or a self-contained PortableApps edition; the portable edition stores settings in its own `Data` folder.

Open the dashboard from the system tray. Settings, manual refresh, and exit are available there too.
