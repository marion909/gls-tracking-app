# Release Notes - GLS Tracking App v1.3.0

## 🔒 Complete Password Protection System

### 🌟 Major Security Update
Diese Version führt ein vollständiges Passwort-Schutz-System ein, das alle sensiblen Daten mit militärgrader Verschlüsselung schützt.

### ✨ Neue Sicherheitsfeatures

#### Master-Passwort-System
- **Erste Einrichtung**: Beim ersten Start wird ein selbst wählbares Master-Passwort abgefragt
- **Sichere Authentifizierung**: Alle nachfolgenden Starts erfordern das Master-Passwort
- **Passwort-Bestätigung**: Schutz vor Tippfehlern bei der Erstellung

#### Enterprise-Grade Verschlüsselung
- **AES-256-CBC**: Militärgrade Verschlüsselung für alle sensiblen Daten
- **PBKDF2 Key Derivation**: 10.000 Iterationen mit SHA-256 für maximale Sicherheit
- **Salt-basierte Sicherheit**: Einzigartige Salts für jede Verschlüsselung
- **Zufällige IV**: Unique Initialization Vectors für jede Verschlüsselungsoperation

#### Sichere Datenspeicherung
- **Verschlüsselte Credentials**: Alle GLS-Portal-Zugangsdaten werden verschlüsselt gespeichert
- **Keine lesbaren Passwörter**: Konfigurationsdateien enthalten keine lesbaren Zugangsdaten
- **Session Management**: Master-Passwort wird nur im Speicher gehalten

### 🚀 Erhaltene Features
Alle bisherigen Funktionen wurden beibehalten und funktionieren nahtlos mit dem neuen Sicherheitssystem:

- **Nummerierte Sendungsanzeige**: Übersichtliches (1/3) Format
- **Überfällige Markierung**: Sendungen älter als 5 Tage in Rot
- **Such-Funktionalität**: Filter nach Kundennamen und Sendungsnummern
- **ZScaler/VPN-Support**: Kompatibilität mit Unternehmensumgebungen
- **Vereinfachte UI**: Nur wesentliche Einstellungen (Datenbank + GLS Portal)
- **Detaillierte Fortschrittsanzeige**: Echtzeit-Status der Website-Interaktion
- **Single-File Deployment**: Alle Abhängigkeiten in einer Datei

### 🛡️ Sicherheitsarchitektur

#### Technische Implementation
- **EncryptionService.cs**: Zentrale Verschlüsselungslogik mit AES-256
- **LoginDialog.cs**: Benutzerfreundliche Passwort-Authentifizierung
- **AppConfig.cs**: Sichere Verwaltung verschlüsselter Credentials
- **Session-basiert**: Master-Passwort nur zur Laufzeit im Speicher

#### Kryptographische Standards
- **Verschlüsselung**: AES-256-CBC mit 256-Bit Schlüsseln
- **Key Derivation**: PBKDF2 mit SHA-256 und 10.000 Iterationen
- **Salt Generation**: Kryptographisch sichere Zufallszahlen
- **IV Generation**: Einzigartige Initialization Vectors pro Verschlüsselung

### 📋 Systemanforderungen
- **Betriebssystem**: Windows 10/11 (x64)
- **Framework**: .NET 9.0 Runtime (wird automatisch installiert)
- **Netzwerk**: Internetverbindung für GLS-Portal-Zugriff
- **Speicher**: ~50 MB Festplattenspeicher

### 🔧 Installation & Upgrade

#### Neue Installation
1. `GlsTrackingApp-v1.3.0.exe` herunterladen
2. Ausführbare Datei starten
3. Master-Passwort bei der ersten Einrichtung festlegen
4. GLS-Portal-Zugangsdaten in den Einstellungen eingeben

#### Upgrade von vorherigen Versionen
1. Alte Version schließen
2. Neue `GlsTrackingApp-v1.3.0.exe` verwenden
3. Beim ersten Start Master-Passwort festlegen
4. GLS-Zugangsdaten erneut eingeben (werden automatisch verschlüsselt)

### ⚠️ Wichtige Sicherheitshinweise
- **Master-Passwort nicht vergessen**: Kann nicht wiederhergestellt werden
- **Sichere Passwörter verwenden**: Empfehlung für starke Master-Passwörter
- **Regelmäßige Backups**: Sicherung der Konfigurationsdaten empfohlen

### 🔗 Download
- **Datei**: `GlsTrackingApp-v1.3.0.exe`
- **Größe**: ~75 MB (alle Abhängigkeiten enthalten)
- **Hash**: SHA-256 wird bei Download angezeigt

### 🐛 Bekannte Probleme
- Keine bekannten Probleme in dieser Version

### 🚀 Nächste Schritte
- Benutzer-Feedback sammeln
- Performance-Optimierungen
- Zusätzliche Sicherheitsfeatures basierend auf Anforderungen

---

**Vollständige Änderungsliste**: Vergleich mit [v1.2.0...v1.3.0](../../compare/v1.2.0...v1.3.0)  
**Binärdatei**: [GlsTrackingApp-v1.3.0.exe](../../releases/download/v1.3.0/GlsTrackingApp-v1.3.0.exe)
