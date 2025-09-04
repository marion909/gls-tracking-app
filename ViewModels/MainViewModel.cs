using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using GlsTrackingApp.Models;
using GlsTrackingApp.Services;

namespace GlsTrackingApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _trackingNumber;
        private TrackingInfo _selectedTrackingInfo;
        private bool _isLoading;
        private string _statusMessage;

        public MainViewModel()
        {
            TrackingResults = new ObservableCollection<TrackingInfo>();
            TrackCommand = new RelayCommand(async () => await TrackPackageAsync(), CanTrack);
            ClearCommand = new RelayCommand(ClearResults);
            RefreshCommand = new RelayCommand(async () => await RefreshSelectedAsync(), CanRefresh);
        }

        public ObservableCollection<TrackingInfo> TrackingResults { get; }

        public string TrackingNumber
        {
            get => _trackingNumber;
            set 
            { 
                _trackingNumber = value; 
                OnPropertyChanged();
                ((RelayCommand)TrackCommand).RaiseCanExecuteChanged();
            }
        }

        public TrackingInfo SelectedTrackingInfo
        {
            get => _selectedTrackingInfo;
            set 
            { 
                _selectedTrackingInfo = value; 
                OnPropertyChanged();
                ((RelayCommand)RefreshCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand TrackCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RefreshCommand { get; }

        private bool CanTrack()
        {
            return !string.IsNullOrWhiteSpace(TrackingNumber) && !IsLoading;
        }

        private bool CanRefresh()
        {
            return SelectedTrackingInfo != null && !IsLoading;
        }

        private async Task TrackPackageAsync()
        {
            if (string.IsNullOrWhiteSpace(TrackingNumber))
                return;

            IsLoading = true;
            StatusMessage = "Paket wird verfolgt...";

            try
            {
                using var seleniumService = new SeleniumTrackingService(headless: true); // Headless für UI
                var trackingInfo = await seleniumService.TrackPackageAsync(TrackingNumber);
                
                if (trackingInfo != null)
                {
                    // Prüfe, ob das Paket bereits in der Liste ist
                    var existingIndex = -1;
                    for (int i = 0; i < TrackingResults.Count; i++)
                    {
                        if (TrackingResults[i].TrackingNumber == trackingInfo.TrackingNumber)
                        {
                            existingIndex = i;
                            break;
                        }
                    }

                    if (existingIndex >= 0)
                    {
                        // Aktualisiere existierenden Eintrag
                        TrackingResults[existingIndex] = trackingInfo;
                    }
                    else
                    {
                        // Füge neuen Eintrag hinzu
                        TrackingResults.Insert(0, trackingInfo);
                    }

                    SelectedTrackingInfo = trackingInfo;
                    StatusMessage = $"Paket {TrackingNumber} erfolgreich abgerufen - Status: {trackingInfo.Status}";
                    TrackingNumber = string.Empty; // Eingabefeld leeren
                }
                else
                {
                    StatusMessage = $"Fehler: Keine Tracking-Daten für {TrackingNumber} gefunden.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshSelectedAsync()
        {
            if (SelectedTrackingInfo == null)
                return;

            IsLoading = true;
            StatusMessage = "Aktualisiere Tracking-Informationen...";

            try
            {
                using var seleniumService = new SeleniumTrackingService(headless: true);
                var refreshedInfo = await seleniumService.TrackPackageAsync(SelectedTrackingInfo.TrackingNumber);
                
                if (refreshedInfo != null)
                {
                    // Finde und aktualisiere den Eintrag in der Liste
                    for (int i = 0; i < TrackingResults.Count; i++)
                    {
                        if (TrackingResults[i].TrackingNumber == refreshedInfo.TrackingNumber)
                        {
                            TrackingResults[i] = refreshedInfo;
                            SelectedTrackingInfo = refreshedInfo;
                            break;
                        }
                    }

                    StatusMessage = $"Paket {SelectedTrackingInfo.TrackingNumber} aktualisiert - Status: {refreshedInfo.Status}";
                }
                else
                {
                    StatusMessage = $"Fehler beim Aktualisieren von {SelectedTrackingInfo.TrackingNumber}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Aktualisieren: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearResults()
        {
            TrackingResults.Clear();
            SelectedTrackingInfo = null;
            StatusMessage = "Ergebnisse gelöscht.";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
            else
            {
                _execute?.Invoke();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
