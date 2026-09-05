<!-- nl -->
# Codex Credit Monitor 2.1

## Nieuw in 2.1

- Twee compacte dashboardacties maken creditbeheer direct bereikbaar: **Credits toevoegen** en **Credits gebruiken**. De plus- en gebruikspictogrammen staan naast de bestaande headeracties en hebben elk een duidelijke tooltip.
- Beide acties openen **Usage & Billing** in ChatGPT. De monitor koopt geen credits en wijzigt geen instelling zelfstandig.

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

- Two compact dashboard actions make credit management directly available: **Add credits** and **Use credits**. The plus and usage icons sit alongside the existing header actions and each have a clear tooltip.
- Both actions open **Usage & Billing** in ChatGPT. The monitor never purchases credits or changes a setting on its own.

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
