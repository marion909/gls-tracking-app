# GLS Tracking App

Eine sichere, passwort-geschützte Desktop-Anwendung zur Verfolgung von GLS-Sendungen mit automatischer Portal-Authentifizierung.

## Features

### 🔒 Sicherheit
- **Master-Passwort-Schutz**: Beim ersten Start wird ein selbst wählbares Master-Passwort festgelegt
- **AES-256 Verschlüsselung**: Alle sensiblen Daten werden verschlüsselt gespeichert
- **PBKDF2 Password Hashing**: 10.000 Iterationen für maximale Sicherheit
- **Keine lesbaren Credentials**: Alle Zugangsdaten werden verschlüsselt abgelegt

### 📦 Tracking-Funktionen
- **Automatische GLS-Portal-Anmeldung**: Nahtlose Integration in das GLS-Portal
- **Batch-Tracking**: Mehrere Sendungen gleichzeitig verfolgen
- **Nummerierte Anzeige**: Übersichtliche Darstellung mit (1/3) Format
- **Überfällige Markierung**: Sendungen älter als 5 Tage werden rot markiert
- **Such-Funktion**: Filtern nach Kundennamen oder Sendungsnummern
- **Detaillierte Status-Anzeige**: Vollständige Tracking-Informationen

### 🌐 Netzwerk-Kompatibilität
- **ZScaler/VPN-Unterstützung**: Funktioniert in Unternehmensumgebungen
- **Proxy-Kompatibilität**: Automatische Proxy-Erkennung
- **Headless Chrome**: Optimierte Performance ohne sichtbaren Browser

### 💾 Datenmanagement
- **Lokale SQLite-Datenbank**: Sichere lokale Speicherung
- **Automatische Backups**: Schutz vor Datenverlust
- **Einfache Migration**: Portierbare Datenbankstruktur

## Technische Details

### Systemanforderungen
- Windows 10/11 (x64)
- .NET 9.0 Runtime (wird automatisch installiert)
- Internetverbindung für GLS-Portal-Zugriff

### Sicherheitsarchitektur
- **Verschlüsselung**: AES-256-CBC mit zufälligen Initialization Vectors
- **Key Derivation**: PBKDF2 mit SHA-256 und 10.000 Iterationen
- **Salt-basierte Sicherheit**: Einzigartige Salts für jede Verschlüsselung
- **Session Management**: Master-Passwort nur im Speicher gehalten

### Verwendete Technologien
- **.NET 9.0**: Moderne Framework-Basis
- **WPF**: Native Windows-Benutzeroberfläche
- **SQLite**: Lokale Datenbank
- **Selenium WebDriver**: Automatisierte Browser-Interaktion
- **System.Security.Cryptography**: Enterprise-grade Verschlüsselung

## Installation & Start

### Voraussetzungen
- Windows 10/11
- .NET 9.0 Runtime

### Anwendung starten
```bash
# Repository klonen (falls von Git)
cd gls-tracking-app

# Abhängigkeiten installieren
dotnet restore

# Anwendung bauen
dotnet build

# Anwendung starten
dotnet run
```

## Nutzung

1. **Sendungsnummer eingeben**: Geben Sie eine GLS Sendungsnummer in das Textfeld ein
2. **Verfolgen klicken**: Klicken Sie auf "Verfolgen" oder drücken Sie Enter
3. **Details anzeigen**: Wählen Sie ein Paket aus der Liste, um detaillierte Informationen zu sehen
4. **Aktualisieren**: Klicken Sie auf "Aktualisieren", um den Status zu erneuern
5. **Löschen**: Entfernen Sie alle verfolgten Pakete mit "Löschen"

## Tracking-Funktionalität

Die App verwendet mehrere Ansätze zum Tracking:

1. **GLS REST API**: Primärer Versuch über die offizielle GLS Tracking API
2. **Web-Scraping Fallback**: Falls die API nicht verfügbar ist, wird die GLS Website gescraped
3. **Fehlerbehandlung**: Ausführliche Fehlerbehandlung und Benutzer-Feedback

### Unterstützte Sendungsnummern
- Standard GLS Sendungsnummern (8-15 Zeichen, alphanumerisch)
- Automatische Bereinigung von Leerzeichen und Sonderzeichen

## Projektstruktur

```
```
GlsTrackingApp/
├── Security/                      # Verschlüsselung und Authentifizierung
│   ├── EncryptionService.cs       # AES-256 Verschlüsselungslogik
│   └── LoginDialog.cs             # Master-Passwort Authentifizierung
├── Services/                      # Geschäftslogik-Services
│   ├── GlsAuthenticationService.cs # GLS Portal Authentifizierung
│   ├── SeleniumTrackingService.cs  # Browser-Automation für Tracking
│   ├── SqliteDatabaseService.cs    # Lokale Datenbank-Operationen
│   ├── TrackingService.cs          # Haupt-Tracking-Service
│   └── TrackingStorageService.cs   # Daten-Persistierung
├── Models/                        # Datenmodelle
│   └── TrackingModels.cs          # Tracking-Datenstrukturen
├── Config/                        # Konfiguration
│   └── AppConfig.cs               # Verschlüsselte App-Konfiguration
├── Resources/                     # Anwendungsressourcen
│   └── icon.ico                   # Anwendungs-Icon
├── SimpleMainWindow.xaml          # Haupt-UI Definition
├── SimpleMainWindow.xaml.cs       # Code-Behind für Hauptfenster
├── SettingsWindow.xaml            # Einstellungen-UI
├── SettingsWindow.xaml.cs         # Code-Behind für Einstellungen
├── App.xaml.cs                    # App-Startup und Initialisierung
└── GlsTrackingApp.csproj          # Projekt-Konfiguration
```

## Dependencies

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.8" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.8" />
```

## Erweiterte Features (Zukünftig)

- 🔄 **Automatische Aktualisierung**: Periodisches Abrufen von Updates
- 📧 **Benachrichtigungen**: E-Mail/Push-Notifications bei Statusänderungen
- 💾 **Persistierung**: Speichern von verfolgten Paketen zwischen Sitzungen
- 🏢 **GLS ShipIT Integration**: Vollständige Integration mit GLS ShipIT API für Versand
- 📊 **Statistiken**: Auswertungen und Berichte über Lieferzeiten
- 🌐 **Multi-Language**: Unterstützung für mehrere Sprachen

## Troubleshooting

### Häufige Probleme

**Problem**: Sendung wird nicht gefunden
- **Lösung**: Überprüfen Sie die Sendungsnummer auf Tippfehler
- **Hinweis**: Neue Sendungen können bis zu 2 Stunden dauern, bis sie im System erscheinen

**Problem**: Netzwerkfehler
- **Lösung**: Überprüfen Sie Ihre Internetverbindung
- **Hinweis**: Die App versucht automatisch verschiedene Tracking-Methoden

**Problem**: App startet nicht
- **Lösung**: Stellen Sie sicher, dass .NET 9.0 installiert ist
- **Tipp**: Führen Sie `dotnet --version` aus, um die installierte Version zu überprüfen

## API Information

Die App nutzt die GLS Austria Tracking-Services:
- **Primary**: `https://gls-group.eu/app/service/open/rest/AT/de/rstt001`
- **Fallback**: Web-Scraping der GLS Tracking-Seite

## Entwicklung

### Eigene Builds erstellen
```bash
# Debug Build
dotnet build --configuration Debug

# Release Build für Distribution
dotnet publish --configuration Release --framework net9.0-windows --self-contained true
```

### Code-Stil
- Verwendung des MVVM-Patterns
- Async/Await für alle HTTP-Operationen
- Comprehensive Exception Handling
- Clean Code Prinzipien

## Lizenz & Haftungsausschluss

Diese Anwendung ist für den privaten und geschäftlichen Gebrauch gedacht. 
Sie nutzt öffentlich verfügbare GLS-Services und ist nicht offiziell von GLS unterstützt.

**Haftungsausschluss**: Die Tracking-Informationen werden von GLS bereitgestellt. 
Für die Genauigkeit und Aktualität der Daten kann keine Gewähr übernommen werden.

---

**Entwickelt für effizientes GLS Paket-Tracking** 📦✨
