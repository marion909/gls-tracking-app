using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GlsTrackingApp.Models;
using GlsTrackingApp.Services;

namespace GlsTrackingApp
{
    public partial class XmlImportWindow : Window
    {
        private readonly XmlImportService _xmlImportService;
        private readonly TrackingStorageService _storageService;
        private GLSList? _currentGlsList;

        public XmlImportWindow()
        {
            InitializeComponent();
            _xmlImportService = new XmlImportService();
            _storageService = new TrackingStorageService();
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "XML-Datei auswählen",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
                LoadXmlFile(dialog.FileName);
            }
        }

        private void LoadXmlFile(string filePath)
        {
            try
            {
                StatusText.Text = "Lade XML-Datei...";
                
                _currentGlsList = _xmlImportService.ImportFromFile(filePath);
                
                if (_currentGlsList.Elements.Count == 0)
                {
                    StatusText.Text = "Keine gültigen Sendungen in der XML-Datei gefunden";
                    SendungenListView.ItemsSource = null;
                    _currentGlsList = null;
                    MessageBox.Show("Die XML-Datei enthält keine gültigen Sendungen.", 
                                   "Keine Daten", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    SendungenListView.ItemsSource = _currentGlsList.Elements;
                    StatusText.Text = $"{_currentGlsList.Elements.Count} Sendungen geladen";
                }
                
                UpdateImportButtonState();
            }
            catch (System.Xml.XmlException xmlEx)
            {
                MessageBox.Show($"XML-Format-Fehler:\n\n{xmlEx.Message}\n\nBitte überprüfen Sie, ob die Datei eine gültige XML-Datei ist.", 
                               "XML-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                StatusText.Text = "XML-Format-Fehler";
                SendungenListView.ItemsSource = null;
                _currentGlsList = null;
                UpdateImportButtonState();
            }
            catch (System.InvalidOperationException ioEx)
            {
                MessageBox.Show($"Fehler beim Verarbeiten der XML-Datei:\n\n{ioEx.Message}\n\nMöglicherweise ist das XML-Format nicht kompatibel.", 
                               "Verarbeitungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                StatusText.Text = "Verarbeitungsfehler";
                SendungenListView.ItemsSource = null;
                _currentGlsList = null;
                UpdateImportButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unerwarteter Fehler beim Laden der XML-Datei:\n\n{ex.Message}\n\nDetails: {ex.GetType().Name}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                StatusText.Text = "Fehler beim Laden der Datei";
                SendungenListView.ItemsSource = null;
                _currentGlsList = null;
                UpdateImportButtonState();
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGlsList?.Elements != null)
            {
                foreach (var element in _currentGlsList.Elements)
                {
                    element.IsSelected = true;
                }
                SendungenListView.Items.Refresh();
                HeaderCheckBox.IsChecked = true;
                UpdateImportButtonState();
            }
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGlsList?.Elements != null)
            {
                foreach (var element in _currentGlsList.Elements)
                {
                    element.IsSelected = false;
                }
                SendungenListView.Items.Refresh();
                HeaderCheckBox.IsChecked = false;
                UpdateImportButtonState();
            }
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectAllButton_Click(sender, e);
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectNoneButton_Click(sender, e);
        }

        private void ItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateHeaderCheckBox();
            UpdateImportButtonState();
        }

        private void UpdateHeaderCheckBox()
        {
            if (_currentGlsList?.Elements == null) return;

            var selectedCount = _currentGlsList.Elements.Count(e => e.IsSelected);
            var totalCount = _currentGlsList.Elements.Count;

            if (selectedCount == 0)
            {
                HeaderCheckBox.IsChecked = false;
            }
            else if (selectedCount == totalCount)
            {
                HeaderCheckBox.IsChecked = true;
            }
            else
            {
                HeaderCheckBox.IsChecked = null; // Indeterminate state
            }
        }

        private void UpdateImportButtonState()
        {
            var hasSelectedItems = _currentGlsList?.Elements?.Any(e => e.IsSelected) == true;
            ImportButton.IsEnabled = hasSelectedItems;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGlsList?.Elements == null) return;

            var selectedElements = _currentGlsList.Elements.Where(e => e.IsSelected).ToList();
            
            if (!selectedElements.Any())
            {
                MessageBox.Show("Bitte wählen Sie mindestens eine Sendung aus.", 
                               "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ImportButton.IsEnabled = false;
                ImportButton.Content = "Importiere...";

                var targetDate = TargetDatePicker.SelectedDate;
                var trackingInfos = _xmlImportService.ConvertToTrackingInfos(selectedElements, targetDate);

                var importedCount = 0;
                var skippedCount = 0;

                foreach (var trackingInfo in trackingInfos)
                {
                    // Prüfe ob bereits vorhanden
                    var existingTrackings = await _storageService.LoadStoredTrackingsAsync();
                    var exists = existingTrackings.Any(t => t.TrackingNumber == trackingInfo.TrackingNumber);

                    if (!exists)
                    {
                        await _storageService.SaveSingleTrackingAsync(trackingInfo);
                        importedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                var message = $"Import abgeschlossen!\n\n" +
                              $"Importiert: {importedCount} Sendungen\n";
                
                if (skippedCount > 0)
                {
                    message += $"Übersprungen (bereits vorhanden): {skippedCount} Sendungen";
                }

                MessageBox.Show(message, "Import erfolgreich", 
                               MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Importieren:\n\n{ex.Message}", 
                               "Import-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ImportButton.IsEnabled = true;
                ImportButton.Content = "Importieren";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
