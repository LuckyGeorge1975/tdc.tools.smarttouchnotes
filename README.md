# SmartTouchNotes

Ein schlankes, offline nutzbares Notizbuch für Stiftgeräte und Desktop. Beim Start erscheint die zuletzt geöffnete Notiz; beim ersten Start entsteht eine leere Schreibfläche. **Neue Notiz**, **Notizen** und **Rückgängig** sind direkt erreichbar. Die Liste ist nach letzter Bearbeitung sortiert. Titel sind optional; eine Titelbearbeitung ist noch nicht implementiert.

Die gemeinsame Avalonia-12-Oberfläche verwendet auf dem Desktop die Maus, auf Android den Stift und ignoriert Touch-Eingaben auf der Schreibfläche. Die Fläche zeichnet ohne Animation; sie ist als `InkSurface` gekapselt und kann später durch eine native Android-Fläche ersetzt werden. Aktuell gibt es noch kein Scrollen oder Zoomen. Stiftstriche werden beim Loslassen gespeichert. Desktop zeigt an, dass dort keine Handschrifterkennung verfügbar ist.

## Erkennung und aktueller Android-Blocker

Der unabhängige Erkennungskoordinator startet nach einer konfigurierbaren Schreibpause (Standard: 500 ms), kopiert die abgeschlossenen Striche und verwirft veraltete Ergebnisse bei neuen Strichen, Undo und Notizwechsel. Originalstriche und erkannter Text bleiben getrennt. Der Android-Adapter verwendet das deutsche ML-Kit-Digital-Ink-Modell (`de-DE`); der Modelldownload läuft asynchron, Fehler werden angezeigt, **Erkennen** ermöglicht einen erneuten Versuch. Währenddessen bleibt das Schreiben möglich. Das heruntergeladene Modell soll anschließend offline nutzbar sein.

**Der Adapter ist noch nicht vollständig gebaut oder auf einem Gerät getestet.** Der Standard-Android-Build enthält deshalb derzeit keine Erkennung, sondern zeigt deren Nichtverfügbarkeit an. Optional aktiviert `-p:EnableDigitalInk=true` das Binding `Xamarin.Google.MLKit.DigitalInk.Recognition` 119.0.0.4. Der Restore mit .NET 10 war erfolgreich; der Build scheiterte in dieser Umgebung vor der C#-Kompilierung mit `XBD001`, weil `dl.google.com` nicht erreichbar war. Betroffen waren u. a. `digital-ink-recognition-19.0.0.aar`, `digital-ink-common-16.0.0.aar`, ML Kit Common, Play Services und Firebase. Daher sind Binding-API, APK mit Erkennung, Modelldownload und Laufzeitverhalten noch zu prüfen. Für den optionalen Build ist Zugriff auf Googles Maven-Repository erforderlich.

## Bauen und prüfen

Voraussetzungen: .NET 10 SDK, für Android `dotnet workload install android`, Android SDK und JDK. Desktop läuft unter Windows; der Linux-Build ist ebenfalls möglich. Tests und Desktop benötigen keine Android-Workload oder Modelldownload.

```sh
dotnet build SmartTouchNotes.Desktop/SmartTouchNotes.Desktop.csproj
dotnet test SmartTouchNotes.Tests/SmartTouchNotes.Tests.csproj
dotnet run --project SmartTouchNotes.Desktop/SmartTouchNotes.Desktop.csproj

# Android ohne Erkennung (Debug-APK)
dotnet build SmartTouchNotes.Android/SmartTouchNotes.Android.csproj -c Debug
# Optionaler, noch nicht vollständig verifizierter ML-Kit-Build:
dotnet build SmartTouchNotes.Android/SmartTouchNotes.Android.csproj -c Debug -p:EnableDigitalInk=true

# USB-Debugging auf dem Bigme aktivieren und RSA-Abfrage bestätigen
adb devices
adb install -r SmartTouchNotes.Android/bin/Debug/net10.0-android/tools.tdc.smarttouchnotes-Signed.apk
adb shell monkey -p tools.tdc.smarttouchnotes 1
```

In dieser Umgebung: Desktop-Build erfolgreich; Tests erfolgreich; Android-Debug-Build **ohne** ML Kit erfolgreich. Ein Desktop-Smoke-Test unter Xvfb erzeugte zwei Notizen mit Mausstrichen und speicherte sie lokal. Der ML-Kit-Build ist wie oben beschrieben blockiert. Ein physisches Gerät stand nicht zur Verfügung.

## Speicherung und späterer Ausbau

Die Datei `SmartTouchNotes/notes.json` liegt im lokalen Anwendungsdatenverzeichnis des jeweiligen Benutzers bzw. im privaten App-Speicher auf Android. Sie enthält eine Formatversion, stabile Notiz-IDs, die zuletzt geöffnete Notiz, Bearbeitungszeit, optionale Titel, geordnete Striche und getrennten Erkennungstext. Jeder Punkt enthält Dokumentkoordinaten, Unix-Zeit in Millisekunden und optionalen Druck. Gespeichert wird über eine temporäre Datei und atomaren Ersatz; unbekannte Formatversionen werden nicht überschrieben. Die Daten sind lokal und nicht verschlüsselt: Geräte-Backups und Zugriffsschutz liegen derzeit beim Betriebssystem.

Für spätere Versionen sollten Vektorobjekte eigene stabile IDs, Typen, Geometrie, Stil und Dokumentkoordinaten erhalten; die ursprünglichen Striche bleiben für Vorschau, Annahme und Undo erhalten. Manuell eingegebener oder korrigierter Text muss getrennt vom automatisch erkannten Text gespeichert werden, ebenso editierbare Zusammenfassungen und Schlagworte mit Herkunft und Änderungsstand. Diese Funktionen sind noch nicht implementiert.

Eine Browser-App benötigt dieselben versionierten Notizen, Strichpunkte und Vektorobjekte zur Anzeige sowie editierbare Text-, Zusammenfassungs- und Schlagwortfelder. Für Synchronisierung benötigt sie stabile Objekt-IDs, Änderungsrevisionen, Löschmarkierungen und einen Austausch seit einem bekannten Stand. Offline-Änderungen und gleichzeitig bearbeitete Versionen müssen erhalten bleiben und Konflikte explizit zusammengeführt oder zur Auswahl angeboten werden, nicht durch „letzter Schreibvorgang gewinnt“ verloren gehen. Authentifizierung, Transport, Backend, Cloud-Anbieter und Browser-Framework bleiben offen; Strich- und Vektorbearbeitung im Browser folgt später. Es gibt noch keine Cloud-Schnittstelle oder Synchronisierung.

## Ausstehende Geräteprüfung: Bigme B7 Pro

- [ ] Mit Stift schnell und langsam schreiben und zeichnen; sichtbare Latenz, fehlende Punkte und Verhalten beim Drehen/Neustart prüfen.
- [ ] Handballen auflegen, mit Stift schreiben und versehentliche Touch-Striche bzw. Aussetzer prüfen.
- [ ] Bei längerer Nutzung und nach Undo/Notizwechsel Ghosting und nötige Bildschirmaktualisierungen beurteilen.
- [ ] Mit verifiziertem ML-Kit-Build online das deutsche Modell laden; währenddessen schreiben, Downloadfehler und erneuten Versuch prüfen.
- [ ] Nach erfolgreichem Download Flugmodus aktivieren und weitere deutsche Handschrift erkennen; bei Schreiben, Undo und Notizwechsel während einer laufenden Erkennung veraltete Vorschläge ausschließen.
