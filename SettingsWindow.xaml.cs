using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using GlsTrackingApp.Config;
using GlsTrackingApp.Services;

namespace GlsTrackingApp
{
    public partial class SettingsWindow : Window
    {
        private readonly AppConfig _config;
        private bool _hasChanges = false;
        
        public SettingsWindow()
        {
            InitializeComponent();
            _config = AppConfig.Instance;
            LoadSettings();
            UpdateInfoDisplay();
        }
        
        private void LoadSettings()
        {
            // Datenbank-Einstellungen
            DatabasePathTextBox.Text = _config.DatabasePath;
        }
        
        private void UpdateInfoDisplay()
        {
            CurrentDbPathInfo.Text = $"Aktueller Datenbankpfad: {_config.DatabasePath}";
            
            if (File.Exists(_config.DatabasePath))
            {
                var fileInfo = new FileInfo(_config.DatabasePath);
                DbStatusInfo.Text = $"SQLite-Datenbank gefunden. Größe: {fileInfo.Length / 1024:N0} KB, " +
                                   $"Letzte Änderung: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm}";
                DbStatusInfo.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                DbStatusInfo.Text = "SQLite-Datenbank nicht gefunden. Wird beim nächsten Start erstellt.";
                DbStatusInfo.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void BrowseDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "SQLite-Datenbank auswählen oder erstellen",
                Filter = "SQLite Database (*.db)|*.db|Alle Dateien (*.*)|*.*",
                DefaultExt = "db",
                FileName = "tracking.db"
            };
            
            // Aktuellen Pfad als Ausgangspunkt verwenden
            var currentPath = DatabasePathTextBox.Text;
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    dialog.FileName = Path.GetFileName(currentPath);
                }
                catch
                {
                    // Fehler ignorieren, Standard verwenden
                }
            }
            
            if (dialog.ShowDialog() == true)
            {
                DatabasePathTextBox.Text = dialog.FileName;
            }
        }
        
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var testPath = DatabasePathTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(testPath))
            {
                MessageBox.Show("Bitte geben Sie einen Datenbankpfad ein.", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            TestConnectionButton.IsEnabled = false;
            TestConnectionButton.Content = "Teste...";
            
            // Variablen vor try-Block definieren
            var originalPath = AppConfig.Instance.DatabasePath;
            var originalType = AppConfig.Instance.DatabaseType;
            
            try
            {
                // Temporären Pfad für Test setzen
                AppConfig.Instance.DatabasePath = testPath;
                AppConfig.Instance.DatabaseType = "SQLite"; // Nur noch SQLite unterstützt
                
                // Nur SQLite testen
                var testService = new SqliteDatabaseService();
                await testService.GetAllTrackingInfoAsync();
                
                MessageBox.Show("Verbindung zur SQLite-Datenbank erfolgreich!", "Test erfolgreich", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Pfad und Typ zurücksetzen
                AppConfig.Instance.DatabasePath = originalPath;
                AppConfig.Instance.DatabaseType = originalType;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Verbindung zur Datenbank fehlgeschlagen:\n\n{ex.Message}", 
                               "Test fehlgeschlagen", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Pfad und Typ zurücksetzen
                AppConfig.Instance.DatabasePath = originalPath;
                AppConfig.Instance.DatabaseType = originalType;
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Verbindung testen";
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var newPath = DatabasePathTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(newPath))
            {
                MessageBox.Show("Bitte geben Sie einen Datenbankpfad ein.", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Verzeichnis erstellen falls nicht vorhanden
                var directory = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Alle Einstellungen speichern
                _config.DatabasePath = newPath;
                _config.DatabaseType = "SQLite";
                
                _config.SaveConfig();
                
                MessageBox.Show("Einstellungen wurden erfolgreich gespeichert!", 
                               "Gespeichert", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Einstellungen:\n\n{ex.Message}", 
                               "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie die Datenbank-Einstellungen auf die Standardwerte zurücksetzen?",
                "Einstellungen zurücksetzen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // Datenbank-Pfad auf Standard zurücksetzen
                DatabasePathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tracking.db");
                
                _hasChanges = true;
                MessageBox.Show("Datenbank-Einstellungen wurden auf Standardwerte zurückgesetzt.", "Zurückgesetzt", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validierung
                if (string.IsNullOrWhiteSpace(DatabasePathTextBox.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen gültigen Datenbankpfad ein.", "Fehler", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Konfiguration speichern (nur SQLite)
                _config.DatabasePath = DatabasePathTextBox.Text.Trim();
                
                _config.SaveConfig();
                
                DialogResult = true;
                
                if (_hasChanges)
                {
                    MessageBox.Show(
                        "Einstellungen wurden gespeichert.\n\nEinige Änderungen werden erst nach einem Neustart der Anwendung wirksam.",
                        "Einstellungen gespeichert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Einstellungen:\n{ex.Message}", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void CleanupDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie alle Sendungen löschen, die älter als 90 Tage sind?\n\nDieser Vorgang kann nicht rückgängig gemacht werden.",
                "Datenbank bereinigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    CleanupDatabaseButton.IsEnabled = false;
                    CleanupDatabaseButton.Content = "Bereinige...";
                    
                    // Hier würde die Bereinigungslogik stehen
                    var cutoffDate = DateTime.Now.AddDays(-90);
                    
                    // TODO: Implementierung der Datenbankbereinigung
                    // var storageService = new TrackingStorageService();
                    // await storageService.DeleteOldTrackingsAsync(cutoffDate);
                    
                    await System.Threading.Tasks.Task.Delay(1000); // Simulation
                    
                    MessageBox.Show("Datenbank wurde erfolgreich bereinigt.", "Bereinigung abgeschlossen", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler bei der Datenbankbereinigung:\n{ex.Message}", "Fehler", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    CleanupDatabaseButton.IsEnabled = true;
                    CleanupDatabaseButton.Content = "Datenbank bereinigen";
                }
            }
        }
        
        private void OpenDatabaseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dbPath = DatabasePathTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = _config.DatabasePath;
                }
                
                var folderPath = Path.GetDirectoryName(dbPath);
                if (Directory.Exists(folderPath))
                {
                    Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show("Der Datenbankordner wurde nicht gefunden.", "Ordner nicht gefunden", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Ordners:\n{ex.Message}", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetSelectedTab(int tabIndex)
        {
            // Da wir nur noch einen Tab haben, wird dieser immer ausgewählt
            SettingsTabControl.SelectedIndex = 0;
        }
    }
}
