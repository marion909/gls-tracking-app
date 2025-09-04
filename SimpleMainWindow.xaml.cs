using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using GlsTrackingApp.Services;
using GlsTrackingApp.Models;
using GlsTrackingApp.Config;
using GlsTrackingApp.Security;

namespace GlsTrackingApp;

public partial class SimpleMainWindow : Window, IDisposable
{
    private readonly TrackingStorageService _storageService;
    private readonly TrackingService _trackingService;
    private readonly Dictionary<string, List<TrackingEvent>> _cachedEvents;
    private List<StoredTrackingInfo> _storedTrackings;
    private bool _disposed = false;

    public SimpleMainWindow()
    {
        InitializeComponent();
        _storageService = new TrackingStorageService();
        _trackingService = new TrackingService();
        _storedTrackings = new List<StoredTrackingInfo>();
        _cachedEvents = new Dictionary<string, List<TrackingEvent>>();
        
        // Fenster zwangsweise in den Vordergrund
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        
        // Authentifizierung beim Start
        if (!AuthenticateUser())
        {
            Application.Current.Shutdown();
            return;
        }
        
        // Einstellungen aus Config laden
        LoadConfigSettings();
        
        // Globale Tastenkürzel
        this.KeyDown += MainWindow_KeyDown;

        // GLS Portal-Zugangsdaten beim Start prüfen
        CheckGlsPortalCredentials();

        // Beim Start alle gespeicherten Trackings laden
        _ = LoadAndRefreshStoredTrackingsAsync();
    }
    
    private bool AuthenticateUser()
    {
        var config = AppConfig.Instance;
        
        try
        {
            if (config.IsFirstRun || string.IsNullOrEmpty(config.MasterPasswordHash))
            {
                // Erstes Mal - Master-Passwort festlegen
                if (LoginDialog.ShowLoginDialog(out string masterPassword, isFirstTime: true))
                {
                    config.SetMasterPassword(masterPassword);
                    config.SaveConfig();
                    
                    MessageBox.Show("Master-Passwort erfolgreich erstellt!\n\nAlle Ihre Daten werden verschlüsselt gespeichert.", 
                                  "Sicherheit aktiviert", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show("Die Anwendung kann ohne Master-Passwort nicht verwendet werden.", 
                                  "Anwendung wird beendet", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            else
            {
                // Bestehendes Passwort eingeben
                if (LoginDialog.ShowLoginDialog(out string masterPassword, isFirstTime: false))
                {
                    config.CurrentMasterPassword = masterPassword;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler bei der Authentifizierung:\n\n{ex.Message}", 
                          "Authentifizierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void LoadConfigSettings()
    {
        var config = AppConfig.Instance;
        
        // Filter immer automatisch aktiviert (unabhängig von gespeicherten Einstellungen)
        HideDeliveredCheckBox.IsChecked = true;  // Zugestellte immer ausblenden
        HideCancelledCheckBox.IsChecked = true;  // Gecancellte immer ausblenden
        
        // Fenster-Einstellungen anwenden
        this.Topmost = config.TopMostWindow;
    }

    private void CheckGlsPortalCredentials()
    {
        var config = AppConfig.Instance;
        var (username, password) = config.GetDecryptedGlsCredentials();
        
        // Prüfe ob GLS Portal-Zugangsdaten vorhanden sind
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            // Zeige Warnung und öffne automatisch die GLS Portal-Einstellungen
            var result = MessageBox.Show(
                "Es sind keine GLS Portal-Zugangsdaten konfiguriert.\n\n" +
                "Das GLS Portal ist für die Sendungsverfolgung erforderlich.\n" +
                "Möchten Sie jetzt Ihre Zugangsdaten eingeben?",
                "GLS Portal-Zugangsdaten erforderlich",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // GLS Portal-Einstellungen direkt öffnen
                ShowGlsPortalSettings(isRequired: true);
            }
            else
            {
                // App schließen wenn Benutzer ablehnt
                MessageBox.Show(
                    "Die Anwendung kann ohne GLS Portal-Zugangsdaten nicht verwendet werden.",
                    "Anwendung wird beendet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
        }
    }

    private async Task LoadAndRefreshStoredTrackingsAsync()
    {
        try
        {
            var config = AppConfig.Instance;
            
            // Prüfe ob GLS Portal Tracking aktiviert und konfiguriert ist
            if (config.UseGlsPortalTracking && 
                !string.IsNullOrEmpty(config.GlsUsername) && 
                !string.IsNullOrEmpty(config.GlsPassword))
            {
                StatusText.Text = "GLS Portal Tracking aktiviert - lade Daten vom GLS Portal...";
                
                // Zuerst gespeicherte Sendungen laden
                _storedTrackings = await _storageService.LoadStoredTrackingsAsync();
                
                if (_storedTrackings.Any())
                {
                    StatusText.Text = "Aktualisiere alle Sendungen vom GLS Portal...";
                    await RefreshAllStoredTrackingsAsync();
                }
                else
                {
                    StatusText.Text = "Keine gespeicherten Sendungen gefunden. GLS Portal Tracking ist bereit.";
                    UpdateStoredTrackingsDisplay();
                }
            }
            else
            {
                StatusText.Text = "Lade gespeicherte Sendungen...";
                _storedTrackings = await _storageService.LoadStoredTrackingsAsync();
                
                // Automatisch alle Sendungen von der GLS-Seite beim Start aktualisieren
                if (_storedTrackings.Any())
                {
                    StatusText.Text = "Aktualisiere Sendungen von GLS-Seite...";
                    await RefreshAllStoredTrackingsAsync();
                }
                else
                {
                    UpdateStoredTrackingsDisplay();
                    StatusText.Text = "Keine gespeicherten Sendungen gefunden. Fügen Sie neue Sendungen hinzu.";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Laden: {ex.Message}";
        }
    }

    private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
    {
        // Button deaktivieren während der Verarbeitung
        RefreshAllButton.IsEnabled = false;
        
        // UI sofort aktualisieren lassen
        await Task.Delay(1);
        
        try
        {
            var config = AppConfig.Instance;
            
            // Prüfe ob GLS Portal Tracking aktiviert und konfiguriert ist
            if (config.UseGlsPortalTracking && 
                !string.IsNullOrEmpty(config.GlsUsername) && 
                !string.IsNullOrEmpty(config.GlsPassword))
            {
                StatusText.Text = "Lade alle Sendungen aus GLS Portal...";
                
                // Entschlüsselte Zugangsdaten abrufen
                var (username, password) = config.GetDecryptedGlsCredentials();
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    StatusText.Text = "Fehler: Keine gültigen GLS-Zugangsdaten verfügbar";
                    return;
                }
                
                // Alle Sendungen direkt aus dem GLS Portal laden
                using var glsService = new Services.GlsAuthenticationService();
                var shipments = await glsService.LoadAllShipmentsAsync(username, password);
                
                if (shipments.Any())
                {
                    StatusText.Text = $"Verarbeite {shipments.Count} Sendungen aus GLS Portal...";
                    
                    // Sendungen in die Datenbank speichern/aktualisieren
                    var updatedCount = 0;
                    var newCount = 0;
                    
                    foreach (var shipment in shipments)
                    {
                        try
                        {
                            // Prüfen ob Sendung bereits existiert
                            var existingTrackings = await _storageService.LoadStoredTrackingsAsync();
                            var existing = existingTrackings.FirstOrDefault(t => t.TrackingNumber == shipment.TrackingNumber);
                            
                            if (existing != null)
                            {
                                // Vorhandene Sendung aktualisieren
                                existing.LastStatus = shipment.Status;
                                existing.CustomerName = shipment.Recipient; // Empfänger als Kundenname verwenden
                                existing.GlsDate = shipment.Date; // GLS Datum hinzufügen
                                existing.LastUpdate = DateTime.Now;
                                existing.LastLocation = "GLS Portal";
                                
                                await _storageService.SaveStoredTrackingAsync(existing);
                                updatedCount++;
                            }
                            else
                            {
                                // Neue Sendung hinzufügen
                                var newTracking = new StoredTrackingInfo
                                {
                                    TrackingNumber = shipment.TrackingNumber,
                                    LastStatus = shipment.Status,
                                    CustomerName = shipment.Recipient,
                                    GlsDate = shipment.Date, // GLS Datum hinzufügen
                                    CreatedDate = DateTime.Now,
                                    LastUpdate = DateTime.Now,
                                    LastLocation = "GLS Portal"
                                };
                                
                                await _storageService.SaveStoredTrackingAsync(newTracking);
                                newCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fehler bei Sendung {shipment.TrackingNumber}: {ex.Message}");
                        }
                    }
                    
                    // Liste neu laden und anzeigen
                    _storedTrackings = await _storageService.LoadStoredTrackingsAsync();
                    UpdateStoredTrackingsDisplay();
                    
                    StatusText.Text = $"✅ GLS Portal Sync abgeschlossen: {newCount} neue, {updatedCount} aktualisierte Sendungen";
                }
                else
                {
                    StatusText.Text = "Keine Sendungen im GLS Portal gefunden.";
                }
            }
            else
            {
                // Fallback: Standard-Aktualisierung
                StatusText.Text = "Lade alle Sendungen aus der Datenbank...";
                _storedTrackings = await _storageService.LoadStoredTrackingsAsync();
                UpdateStoredTrackingsDisplay();
                
                // Dann alle aktualisieren
                await RefreshAllStoredTrackingsAsync();
            }
        }
        finally
        {
            // Buttons wieder aktivieren
            RefreshAllButton.IsEnabled = true;
        }
    }

    private async Task RefreshAllStoredTrackingsAsync()
    {
        var config = AppConfig.Instance;
        
        // Wenn keine gespeicherten Sendungen vorhanden sind
        if (!_storedTrackings.Any())
        {
            StatusText.Text = "Keine gespeicherten Sendungen zum Aktualisieren.";
            return;
        }

        // Filtere Sendungen: Nur die aktualisieren, die NICHT "Zugestellt" oder "cancelled" sind
        var trackingsToUpdate = _storedTrackings.Where(t => 
            !t.IsDelivered && 
            !t.IsCancelled
        ).ToList();

        if (!trackingsToUpdate.Any())
        {
            StatusText.Text = "Alle Sendungen sind bereits zugestellt oder storniert - keine Aktualisierung erforderlich.";
            return;
        }

        var totalTrackings = trackingsToUpdate.Count;
        var skippedCount = _storedTrackings.Count - totalTrackings;
        
        // Buttons deaktivieren
        RefreshAllButton.IsEnabled = false;
        
        // Prüfe ob GLS Portal Tracking aktiviert ist
        if (config.UseGlsPortalTracking && 
            !string.IsNullOrEmpty(config.GlsUsername) && 
            !string.IsNullOrEmpty(config.GlsPassword))
        {
            // **NEUE OPTIMIERTE METHODE**: Alle Sendungen auf einmal vom GLS Portal laden
            await RefreshAllFromGlsPortalAsync(totalTrackings, skippedCount);
        }
        else
        {
            // Fallback: Standard-Tracking (einzeln verarbeiten)
            await RefreshAllTrackingsIndividuallyAsync(trackingsToUpdate, totalTrackings, skippedCount);
        }
    }

    /// <summary>
    /// NEUE OPTIMIERTE METHODE: Lädt alle Sendungen einmalig vom GLS Portal
    /// </summary>
    private async Task RefreshAllFromGlsPortalAsync(int totalTrackings, int skippedCount)
    {
        var config = AppConfig.Instance;
        
        StatusText.Text = "🚀 Lade alle Sendungen einmalig vom GLS Portal...";
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressBar.Maximum = 10; // 10 Schritte für detaillierten Fortschritt

        try
        {
            // GLS Portal Service mit Progress-Callback initialisieren
            using var glsService = new Services.GlsAuthenticationService();
            
            // Progress-Callback für Echtzeit-Updates
            glsService.ProgressCallback = (message, current, total) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = message;
                    ProgressBar.Value = current;
                    ProgressBar.Maximum = total;
                });
            };
            
            // Alle Sendungen auf einmal laden
            var (username, password) = config.GetDecryptedGlsCredentials();
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Keine gültigen GLS-Zugangsdaten verfügbar");
            }
            
            var glsShipments = await glsService.LoadAllShipmentsAsync(username, password);
            
            // Lokale Daten mit GLS Daten synchronisieren
            StatusText.Text = "🔄 Synchronisiere lokale Daten...";
            await Task.Delay(50); // UI Update

            int updatedCount = 0;
            int newCount = 0;
            
            // Lokale Sendungen mit GLS Daten aktualisieren
            foreach (var glsShipment in glsShipments)
            {
                var existingTracking = _storedTrackings.FirstOrDefault(t => 
                    t.TrackingNumber.Equals(glsShipment.TrackingNumber, StringComparison.OrdinalIgnoreCase));
                
                if (existingTracking != null)
                {
                    // Bestehende Sendung aktualisieren
                    existingTracking.LastStatus = glsShipment.Status;
                    existingTracking.LastLocation = glsShipment.Recipient; // Empfänger als Location
                    existingTracking.GlsDate = glsShipment.Date; // GLS Datum hinzufügen
                    existingTracking.LastUpdate = DateTime.Now;
                    
                    await _storageService.SaveStoredTrackingAsync(existingTracking);
                    updatedCount++;
                }
                else
                {
                    // Neue Sendung hinzufügen (falls im GLS Portal, aber nicht lokal vorhanden)
                    var newTracking = new StoredTrackingInfo
                    {
                        TrackingNumber = glsShipment.TrackingNumber,
                        LastStatus = glsShipment.Status,
                        LastLocation = glsShipment.Recipient,
                        GlsDate = glsShipment.Date, // GLS Datum hinzufügen
                        LastUpdate = DateTime.Now
                    };
                    
                    await _storageService.SaveStoredTrackingAsync(newTracking);
                    _storedTrackings.Add(newTracking);
                    newCount++;
                }
            }

            // Finale UI-Updates
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
            RefreshAllButton.IsEnabled = true;
            UpdateStoredTrackingsDisplay();
            
            StatusText.Text = $"✅ GLS Portal Sync abgeschlossen: {updatedCount} aktualisiert, {newCount} neu hinzugefügt, {skippedCount} übersprungen";
        }
        catch (Exception ex)
        {
            // Fehlerbehandlung
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
            RefreshAllButton.IsEnabled = true;
            
            StatusText.Text = $"❌ Fehler beim GLS Portal Sync: {ex.Message}";
            Console.WriteLine($"[ERROR] GLS Portal Sync Fehler: {ex}");
        }
    }

    /// <summary>
    /// Fallback-Methode: Einzelne Tracking-Verarbeitung (wie vorher)
    /// </summary>
    private async Task RefreshAllTrackingsIndividuallyAsync(List<StoredTrackingInfo> trackingsToUpdate, int totalTrackings, int skippedCount)
    {
        StatusText.Text = $"Aktualisiere {totalTrackings} Sendungen einzeln mit Standard-Tracking ({skippedCount} übersprungen)...";
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressBar.Maximum = totalTrackings;

        // UI sofort aktualisieren lassen, bevor die Abfragen starten
        await Task.Delay(50);

        // Parallele Verarbeitung mit weniger Browsern für bessere UI-Responsivität
        using var semaphore = new System.Threading.SemaphoreSlim(3, 3); // Reduziert von 5 auf 3
        var processedCount = 0;
        var lockObject = new object();
        var successCount = 0;
        var errorCount = 0;

        try
        {
            var tasks = trackingsToUpdate.Select(async tracking =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    using var service = new Services.TrackingService();
                    var result = await service.TrackPackageAsync(tracking.TrackingNumber).ConfigureAwait(false);
                    if (result != null)
                    {
                        // Daten-Updates ohne UI-Thread
                        tracking.LastStatus = result.Status;
                        tracking.LastLocation = result.Location;
                        tracking.LastUpdate = DateTime.Now;

                        await _storageService.SaveStoredTrackingAsync(tracking).ConfigureAwait(false);
                        
                        // Events zu Cache hinzufügen
                        if (result.Events != null && result.Events.Any())
                        {
                            _cachedEvents[tracking.TrackingNumber] = result.Events.ToList();
                        }

                        lock (lockObject)
                        {
                            successCount++;
                        }
                    }

                    // Thread-safe Progress Update
                    lock (lockObject)
                    {
                        processedCount++;
                    }

                    // UI Update im Main Thread
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = processedCount;
                        StatusText.Text = $"⚡ Verarbeitet: {processedCount}/{totalTrackings} (✓{successCount} ✗{errorCount})";
                    });

                    // Kleine Pause für bessere System-Performance
                    await Task.Delay(200).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        errorCount++;
                        processedCount++;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Fehler bei {tracking.TrackingNumber}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            // Finale UI-Updates
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0;
                RefreshAllButton.IsEnabled = true;
                UpdateStoredTrackingsDisplay();
            });
        }
    }

    private void UpdateStoredTrackingsDisplay()
    {
        try
        {
            if (_disposed || _storedTrackings == null) return;
            
            var totalCount = _storedTrackings.Count;
            var filteredTrackings = _storedTrackings.AsEnumerable();
            
            // Filter für zugestellte Sendungen anwenden
            if (HideDeliveredCheckBox?.IsChecked == true)
            {
                filteredTrackings = filteredTrackings.Where(t => !t.IsDelivered);
            }
            
            // Filter für gecancelled Sendungen anwenden
            if (HideCancelledCheckBox?.IsChecked == true)
            {
                filteredTrackings = filteredTrackings.Where(t => !t.IsCancelled);
            }
            
            // Suchfilter anwenden
            var searchText = SearchTextBox?.Text?.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredTrackings = filteredTrackings.Where(t => 
                    (!string.IsNullOrEmpty(t.CustomerName) && t.CustomerName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(t.TrackingNumber) && t.TrackingNumber.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }
            
            // Sortiere nach Kundenname - alle Einträge einzeln, aber gruppiert nach Name
            var sortedTrackings = filteredTrackings
                .OrderBy(t => string.IsNullOrWhiteSpace(t.CustomerName) ? "ZZZ_Unbekannt" : t.CustomerName.Trim().ToUpper())
                .ThenBy(t => t.TrackingNumber)
                .ToList();
            
            // Nummerierung für jeden Kunden berechnen
            var customerGroups = sortedTrackings
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CustomerName) ? "ZZZ_Unbekannt" : t.CustomerName.Trim().ToUpper())
                .ToList();
                
            foreach (var group in customerGroups)
            {
                var customerTrackings = group.OrderBy(t => t.TrackingNumber).ToList();
                var count = customerTrackings.Count;
                
                for (int i = 0; i < customerTrackings.Count; i++)
                {
                    customerTrackings[i].DisplayIndex = i + 1;
                    customerTrackings[i].TotalCount = count;
                }
            }
            
            var filteredCount = sortedTrackings.Count;
            
            if (StoredTrackingsListView != null)
            {
                StoredTrackingsListView.ItemsSource = null;
                StoredTrackingsListView.ItemsSource = sortedTrackings;
            }
            
            // Anzahl-Anzeige aktualisieren
            if (TrackingCountLabel != null)
            {
                var searchInfo = !string.IsNullOrEmpty(searchText) ? $" (gefiltert nach: \"{searchText}\")" : "";
                TrackingCountLabel.Text = $"{filteredCount} / {totalCount} Sendungen{searchInfo}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateStoredTrackingsDisplay Fehler: {ex.Message}");
        }
    }
    
    // Menu Event Handlers




    private void DatabaseMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Database Viewer ist in dieser Version noch nicht verfügbar.", "Database", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = StoredTrackingsListView.SelectedItem as StoredTrackingInfo;
        if (selectedItem == null)
        {
            StatusText.Text = "Bitte wählen Sie eine Sendung zum Löschen aus.";
            return;
        }

        var result = MessageBox.Show(
            $"Möchten Sie die Sendung {selectedItem.TrackingNumber} wirklich löschen?",
            "Sendung löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _storageService.DeleteStoredTrackingAsync(selectedItem.TrackingNumber);
                
                // Aus Cache entfernen
                if (_cachedEvents.ContainsKey(selectedItem.TrackingNumber))
                {
                    _cachedEvents.Remove(selectedItem.TrackingNumber);
                }
                
                await LoadAndRefreshStoredTrackingsAsync();
                StatusText.Text = $"Sendung {selectedItem.TrackingNumber} wurde gelöscht.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fehler beim Löschen: {ex.Message}";
            }
        }
    }

    private void DatabaseSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            
            // Öffne direkt den Datenbank-Tab (Index 2)
            settingsWindow.SetSelectedTab(2);
            
            var result = settingsWindow.ShowDialog();
            
            if (result == true)
            {
                LoadConfigSettings();
                StatusText.Text = "Datenbank-Einstellungen wurden aktualisiert.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Öffnen der Datenbank-Einstellungen:\n{ex.Message}", "Fehler", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GlsPortalSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowGlsPortalSettings(isRequired: false);
    }

    private void ShowGlsPortalSettings(bool isRequired = false)
    {
        try
        {
            var config = AppConfig.Instance;
            
            // Erstelle ein einfaches Eingabefenster für GLS-Einstellungen
            var settingsDialog = new Window
            {
                Title = "GLS Portal-Einstellungen",
                Width = 500,
                Height = 450, // Erhöht für ZScaler-Einstellungen
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            
            // Setze Owner nur wenn das Hauptfenster bereits geladen ist
            if (this.IsLoaded)
            {
                settingsDialog.Owner = this;
                settingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Titel
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Info
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Username Label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Username TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Password Label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: Password TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6: ZScaler Settings
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 7: All Buttons
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 8: Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 9: Bottom
            
            grid.Margin = new Thickness(20);
            
            // Überschrift
            var title = new TextBlock
            {
                Text = isRequired ? "GLS Portal-Zugangsdaten eingeben (Pflicht)" : "GLS Portal-Einstellungen",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);
            
            // Info-Text (GLS Portal ist jetzt Pflicht)
            var infoText = new TextBlock
            {
                Text = "Das GLS Portal wird für die Sendungsverfolgung benötigt.\nAlle Zugangsdaten werden verschlüsselt gespeichert.",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(infoText, 1);
            grid.Children.Add(infoText);
            
            // Benutzername
            var usernameLabel = new TextBlock
            {
                Text = "GLS Benutzername:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(usernameLabel, 2);
            grid.Children.Add(usernameLabel);
            
            // Verschlüsselte Zugangsdaten laden
            var (currentUsername, currentPassword) = config.GetDecryptedGlsCredentials();
            
            var usernameBox = new TextBox
            {
                Text = currentUsername,
                Margin = new Thickness(0, 0, 0, 15),
                Height = 25,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(usernameBox, 3);
            grid.Children.Add(usernameBox);
            
            // Passwort
            var passwordLabel = new TextBlock
            {
                Text = "GLS Passwort:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(passwordLabel, 4);
            grid.Children.Add(passwordLabel);
            
            var passwordBox = new PasswordBox
            {
                Password = currentPassword,
                Margin = new Thickness(0, 0, 0, 15),
                Height = 25,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(passwordBox, 5);
            grid.Children.Add(passwordBox);
            
            // ZScaler/Proxy-Einstellungen
            var zscalerGroupBox = new GroupBox
            {
                Header = "VPN/Proxy-Einstellungen (für ZScaler/Corporate Networks)",
                Margin = new Thickness(0, 15, 0, 15)
            };
            
            var zscalerPanel = new StackPanel { Margin = new Thickness(10) };
            
            var zscalerCheckBox = new CheckBox
            {
                Content = "ZScaler-Kompatibilitätsmodus aktivieren",
                IsChecked = config.ZScalerMode,
                Margin = new Thickness(0, 0, 0, 8)
            };
            zscalerPanel.Children.Add(zscalerCheckBox);
            
            var disableProxyCheckBox = new CheckBox
            {
                Content = "Proxy-Erkennung deaktivieren (nur bei Problemen)",
                IsChecked = config.DisableProxyDetection,
                Margin = new Thickness(0, 0, 0, 8)
            };
            zscalerPanel.Children.Add(disableProxyCheckBox);
            
            var proxyInfoText = new TextBlock
            {
                Text = "💡 Bei Problemen mit ZScaler/VPN: Kompatibilitätsmodus aktivieren",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            };
            zscalerPanel.Children.Add(proxyInfoText);
            
            zscalerGroupBox.Content = zscalerPanel;
            Grid.SetRow(zscalerGroupBox, 6);
            grid.Children.Add(zscalerGroupBox);
            
            // Button-Panel für Test-Button und OK/Abbrechen
            var buttonRowPanel = new Grid();
            buttonRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Test-Button
            buttonRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            buttonRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action-Buttons
            
            // Test-Button
            var testButton = new Button
            {
                Content = "Verbindung testen",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetColumn(testButton, 0);
            buttonRowPanel.Children.Add(testButton);
            
            Grid.SetRow(buttonRowPanel, 7);
            grid.Children.Add(buttonRowPanel);
            
            testButton.Click += async (s, args) =>
            {
                if (string.IsNullOrEmpty(usernameBox.Text) || string.IsNullOrEmpty(passwordBox.Password))
                {
                    MessageBox.Show("Bitte geben Sie Benutzername und Passwort ein.", "Eingabe erforderlich", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                testButton.IsEnabled = false;
                testButton.Content = "Teste...";
                
                try
                {
                    using var glsService = new GlsAuthenticationService();
                    var success = await glsService.TestLoginAsync(usernameBox.Text, passwordBox.Password);
                    
                    if (success)
                    {
                        MessageBox.Show("✅ Verbindung erfolgreich!\n\nDie GLS Portal-Anmeldung funktioniert korrekt.", 
                                      "Test erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("❌ Verbindung fehlgeschlagen!\n\nBitte überprüfen Sie Ihre Anmeldedaten.", 
                                      "Test fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Fehler beim Testen der Verbindung:\n\n{ex.Message}", 
                                  "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    testButton.IsEnabled = true;
                    testButton.Content = "Verbindung testen";
                }
            };
            
            // Buttons rechts
            var actionButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };
            
            var cancelButton = new Button
            {
                Content = isRequired ? "App beenden" : "Abbrechen",
                Width = isRequired ? 100 : 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, args) => 
            {
                settingsDialog.DialogResult = false;
                if (isRequired)
                {
                    Application.Current.Shutdown();
                }
            };
            
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                IsDefault = true
            };
            okButton.Click += (s, args) =>
            {
                if (string.IsNullOrEmpty(usernameBox.Text) || string.IsNullOrEmpty(passwordBox.Password))
                {
                    MessageBox.Show("Bitte geben Sie Benutzername und Passwort ein.", "Eingabe erforderlich", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                try
                {
                    // GLS Portal ist jetzt immer aktiviert
                    config.UseGlsPortalTracking = true;
                    
                    // Verschlüsselt speichern
                    config.SetEncryptedGlsCredentials(usernameBox.Text, passwordBox.Password);
                    
                    // ZScaler-Einstellungen speichern
                    config.ZScalerMode = zscalerCheckBox.IsChecked == true;
                    config.DisableProxyDetection = disableProxyCheckBox.IsChecked == true;
                    
                    config.SaveConfig();
                    
                    settingsDialog.DialogResult = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern der Einstellungen:\n\n{ex.Message}", 
                                  "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            actionButtonPanel.Children.Add(cancelButton);
            actionButtonPanel.Children.Add(okButton);
            
            Grid.SetColumn(actionButtonPanel, 2);
            buttonRowPanel.Children.Add(actionButtonPanel);
            
            settingsDialog.Content = grid;
            
            var result = settingsDialog.ShowDialog();
            
            if (result == true)
            {
                MessageBox.Show("GLS Portal-Einstellungen wurden gespeichert.", "Einstellungen gespeichert", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Öffnen der GLS Portal-Einstellungen:\n{ex.Message}", "Fehler", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void HideDeliveredCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Filterlogik für zugestellte Sendungen
        UpdateStoredTrackingsDisplay();
    }

    private void HideCancelledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Filterlogik für gecancelled Sendungen
        UpdateStoredTrackingsDisplay();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Filterlogik für Suchtext
        UpdateStoredTrackingsDisplay();
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        // Suchfeld leeren
        if (SearchTextBox != null)
        {
            SearchTextBox.Text = "";
            SearchTextBox.Focus();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Cleanup-Operationen
        CleanupResources();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        // Zusätzliche Cleanup-Operationen falls nötig
        CleanupResources();
        Application.Current.Shutdown();
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Prüfen auf Modifikator-Tasten
        bool ctrlPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control;
        
        try
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.F5:
                    // Alle Sendungen aktualisieren
                    RefreshAllButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                    
                case System.Windows.Input.Key.F when ctrlPressed:
                    // Suchfeld fokussieren
                    if (SearchTextBox != null)
                    {
                        SearchTextBox.Focus();
                        SearchTextBox.SelectAll();
                    }
                    e.Handled = true;
                    break;
                    
                case System.Windows.Input.Key.Escape:
                    // Suchfeld leeren falls fokussiert
                    if (SearchTextBox?.IsFocused == true && !string.IsNullOrEmpty(SearchTextBox.Text))
                    {
                        SearchTextBox.Text = "";
                        e.Handled = true;
                    }
                    break;
                    
                case System.Windows.Input.Key.D1 when ctrlPressed:
                    // Zugestellte Filter umschalten
                    HideDeliveredCheckBox.IsChecked = !HideDeliveredCheckBox.IsChecked;
                    e.Handled = true;
                    break;
                    
                case System.Windows.Input.Key.D2 when ctrlPressed:
                    // Gecancellte Filter umschalten
                    HideCancelledCheckBox.IsChecked = !HideCancelledCheckBox.IsChecked;
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler bei Tastenkürzel: {ex.Message}";
        }
    }

    private void CleanupResources()
    {
        if (_disposed) return;
        
        try
        {
            Console.WriteLine("[Cleanup] 🧹 Starte Ressourcen-Bereinigung...");

            // Tracking Service ordnungsgemäß beenden
            if (_trackingService != null)
            {
                Console.WriteLine("[Cleanup] 🔄 Beende Tracking Service...");
                _trackingService.Dispose();
            }

            // Alle Chrome-Prozesse beenden, die möglicherweise hängen geblieben sind
            try
            {
                var chromeProcesses = Process.GetProcessesByName("chrome");
                var chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
                
                Console.WriteLine($"[Cleanup] 🔍 Gefunden: {chromeProcesses.Length} Chrome-Prozesse, {chromeDriverProcesses.Length} ChromeDriver-Prozesse");

                foreach (var process in chromeProcesses.Concat(chromeDriverProcesses))
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            Console.WriteLine($"[Cleanup] ❌ Beende Prozess: {process.ProcessName} (PID: {process.Id})");
                            process.Kill();
                            process.WaitForExit(2000); // 2 Sekunden warten
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Cleanup] ⚠️ Fehler beim Beenden von {process.ProcessName}: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] ⚠️ Fehler bei der Prozess-Bereinigung: {ex.Message}");
            }

            Console.WriteLine("[Cleanup] ✅ Ressourcen-Bereinigung abgeschlossen");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cleanup] ❌ Unerwarteter Fehler bei der Bereinigung: {ex.Message}");
        }
        finally
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        CleanupResources();
        GC.SuppressFinalize(this);
    }

    ~SimpleMainWindow()
    {
        CleanupResources();
    }
}
