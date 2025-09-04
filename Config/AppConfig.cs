using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using GlsTrackingApp.Security;

namespace GlsTrackingApp.Config
{
    public class AppConfig
    {
        public string DatabasePath { get; set; } = string.Empty;
        public string DatabaseType { get; set; } = "SQLite"; // "Access" oder "SQLite"
        
        // Tracking-Einstellungen
        public int BrowserCount { get; set; } = 5;
        public int PageLoadTimeout { get; set; } = 30;
        public int ElementWaitTime { get; set; } = 10;
        public bool HeadlessMode { get; set; } = false;
        
        // Allgemeine Einstellungen
        public bool HideDeliveredByDefault { get; set; } = true;  // Standardmäßig aktiviert
        public bool HideCancelledByDefault { get; set; } = true;  // Standardmäßig aktiviert
        public bool TopMostWindow { get; set; } = true;
        
        // GLS Portal-Einstellungen
        public string GlsUsername { get; set; } = string.Empty;
        public string GlsPassword { get; set; } = string.Empty;
        public bool UseGlsPortalTracking { get; set; } = true;
        
        // ZScaler/Proxy-Einstellungen
        public bool ZScalerMode { get; set; } = false;
        public bool DisableProxyDetection { get; set; } = false;
        public string CustomProxyServer { get; set; } = string.Empty;
        
        // Security-Einstellungen
        public string MasterPasswordHash { get; set; } = string.Empty;
        public bool IsFirstRun { get; set; } = true;
        
        // Aktuelle Session-Daten (werden nicht gespeichert)
        [System.Text.Json.Serialization.JsonIgnore]
        public string CurrentMasterPassword { get; set; } = string.Empty;
        
        private static AppConfig? _instance;
        private static readonly object _lock = new object();
        
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfig();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private static AppConfig LoadConfig()
        {
            var config = new AppConfig();
            
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                             "GlsTrackingApp", "appsettings.json");
                
                if (File.Exists(configPath))
                {
                    var builder = new ConfigurationBuilder()
                        .AddJsonFile(configPath, optional: true);
                    
                    var configuration = builder.Build();
                    configuration.Bind(config);
                }
                
                // Standard-Pfad setzen falls nicht konfiguriert
                if (string.IsNullOrEmpty(config.DatabasePath))
                {
                    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var appFolder = Path.Combine(appDataPath, "GlsTrackingApp");
                    // SQLite als Standard
                    config.DatabasePath = Path.Combine(appFolder, "tracking.db");
                }
            }
            catch (Exception)
            {
                // Bei Fehlern Standard-Konfiguration verwenden
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appDataPath, "GlsTrackingApp");
                config.DatabasePath = Path.Combine(appFolder, "tracking.mdb");
            }
            
            return config;
        }
        
        public void SaveConfig()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appDataPath, "GlsTrackingApp");
                
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);
                
                var configPath = Path.Combine(appFolder, "appsettings.json");
                
                var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(configPath, json);
            }
            catch (Exception)
            {
                // Fehler beim Speichern ignorieren
            }
        }
        
        // Verschlüsselte GLS-Zugangsdaten
        public void SetEncryptedGlsCredentials(string username, string password)
        {
            if (string.IsNullOrEmpty(CurrentMasterPassword))
                throw new InvalidOperationException("Master-Passwort nicht gesetzt!");
                
            try
            {
                GlsUsername = EncryptionService.Encrypt(username, CurrentMasterPassword);
                GlsPassword = EncryptionService.Encrypt(password, CurrentMasterPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fehler beim Verschlüsseln der GLS-Zugangsdaten: {ex.Message}", ex);
            }
        }
        
        public (string username, string password) GetDecryptedGlsCredentials()
        {
            if (string.IsNullOrEmpty(CurrentMasterPassword))
                return (string.Empty, string.Empty);
                
            try
            {
                var username = string.IsNullOrEmpty(GlsUsername) ? string.Empty : 
                              EncryptionService.Decrypt(GlsUsername, CurrentMasterPassword);
                var password = string.IsNullOrEmpty(GlsPassword) ? string.Empty : 
                              EncryptionService.Decrypt(GlsPassword, CurrentMasterPassword);
                return (username, password);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }
        
        public void SetMasterPassword(string masterPassword)
        {
            CurrentMasterPassword = masterPassword;
            MasterPasswordHash = EncryptionService.HashPassword(masterPassword);
            IsFirstRun = false;
        }
        
        public bool VerifyMasterPassword(string password)
        {
            return EncryptionService.VerifyPassword(password, MasterPasswordHash);
        }
    }
}
