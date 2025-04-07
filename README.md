
# Autovermietung Webanwendung

Dieses Projekt ist eine einfache und verständliche Webanwendung zur Verwaltung einer Autovermietung. Die Anwendung bietet Administratoren Möglichkeiten zur Fahrzeug- und Benutzerverwaltung, Erstellung von Mietverträgen sowie Rechnungen. Benutzer können Fahrzeuge reservieren und den Status ihrer Vermietungen einsehen.

## Funktionen

- **Fahrzeugverwaltung:** Fahrzeuge hinzufügen, bearbeiten und entfernen.
- **Benutzerverwaltung:** Benutzerregistrierung, Login und Verwaltung von Benutzerrollen (Benutzer/Admin).
- **Reservierungen:** Erstellung und Verwaltung von Fahrzeugreservierungen.
- **Rechnungserstellung:** Automatische Erstellung von Rechnungen zu abgeschlossenen Mietverträgen.
- **Passwort- und Sicherheitsfragen:** Passwort-Zurücksetzung mithilfe von Sicherheitsfragen.

## Technologie-Stack

- C# (.NET Framework)
- HTML/CSS/JavaScript für die Web-Oberfläche
- CSV-Dateien für einfache datenbasierte Speicherung (keine externe Datenbank erforderlich)
- Passwort-Hashing für sichere Benutzerauthentifizierung

## Einrichtung & Nutzung

1. Klone das Repository:
```bash
git clone https://github.com/OPuder/Autovermietung.git
```

2. Öffne das Projekt mit einer IDE deiner Wahl (SharpDevelop).

3. Lege die CSV-Dateien in den Ordner `Assets/` im Projektverzeichnis.

4. Starte die Anwendung entweder direkt über die IDE oder kompiliere und starte das Projekt.

5. Greife über deinen Webbrowser auf die Web-Oberfläche zu:
```
http://localhost:PORT
```

## Projektstruktur

- `Controllers/`: Verwaltung der Anwendungslogik (z.B. AdminController, AuthController)
- `Models/`: Datenmodelle für Benutzer, Fahrzeuge, Vermietungen
- `Views/`: Web-Oberfläche (HTML, CSS, JavaScript)
- `Assets/`: CSV-Dateien zur Speicherung von Daten

## Lizenz

Dieses Projekt ist Open Source und kann frei verwendet und angepasst werden.
