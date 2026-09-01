# Codex Credit Monitor 1.2

Een lichte Windows-systeemvakmonitor voor lokaal Codex-gebruik. De app leest alleen de sessieregistratie van de aangemelde Windows-gebruiker en verstuurt geen gegevens.

## Gebruik

- Start stil in het systeemvak. Open het dashboard met een linkermuisklik of **Dashboard openen**.
- Rechtsboven staan transparante iconen voor **Instellingen** en **Nu vernieuwen**; de tooltip verschijnt bij hover.
- Het dashboard toont credittegoed, 5-uurs- en weekverbruik, modelmomenten, tokens en recente sessies. Modelgebruik wordt per verbruiksmoment toegewezen, ook bij een modelwissel binnen één sessie.
- **Nu vernieuwen** toont bij een gesloten dashboard alleen het uiteindelijke compacte overzicht bij het systeemvak; er verschijnt geen tussentijds laadvenster.
- Waarschuwingen verschijnen eenmalig bij 75% en 90% verbruik van het 5-uursvenster.
- De ingestelde automatische opwaardeerbuffer wordt als context bij het creditsaldo getoond (standaard 125 → 250 credits); de monitor wijzigt ChatGPT zelf niet.
- De monitor bewaakt logactiviteit gebeurtenisgestuurd en draait met lage Windows-prioriteit.

## Instellingen

Via het systeemvakmenu **Instellingen…** kies je starten met Windows, waarschuwingen, geluid en een verversinterval van 1, 2 of 5 minuten. **Info…** leest de actuele inhoud van [`src/WhatsNew.md`](src/WhatsNew.md) rechtstreeks uit de appmap.

## Distributie

- Lokale app: `app\CodexCreditMonitor.exe` (vereist de .NET Desktop Runtime).
- Draagbare installatie: `Distributie\PortableApps\CodexCreditMonitorPortable_1.2_Dutch.paf.exe`.

De PortableApps-versie bewaart haar instellingen in de pakketmap en biedt daarom geen Windows-opstartoptie. Dit document beschrijft uitsluitend de actuele versie; er is geen aparte versiegeschiedenis.
