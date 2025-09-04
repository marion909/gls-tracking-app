using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Windows.Media;

namespace GlsTrackingApp.Models
{
    public class TrackingInfo : INotifyPropertyChanged
    {
        private string _trackingNumber;
        private string _status;
        private DateTime? _lastUpdate;
        private string _location;
        private bool _isTracking;

        public string TrackingNumber
        {
            get => _trackingNumber;
            set { _trackingNumber = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime? LastUpdate
        {
            get => _lastUpdate;
            set { _lastUpdate = value; OnPropertyChanged(); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        public bool IsTracking
        {
            get => _isTracking;
            set { _isTracking = value; OnPropertyChanged(); }
        }

        // Neue Eigenschaften für Speicherung
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsDelivered => Status?.Contains("Zugestellt") == true || Status?.Contains("✅") == true;
        public string StatusColor => "Black";

        public List<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();

        // Zusätzliche Eigenschaften für erweiterte Informationen
        public string Description { get; set; } = string.Empty;
        public DeliveryAddress? Address { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TrackingEvent
    {
        public DateTime DateTime { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
    }

    public class DeliveryAddress
    {
        public string CountryCode { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class GlsTrackingResponse
    {
        [JsonProperty("tuNumber")]
        public string TrackingNumber { get; set; }

        [JsonProperty("history")]
        public List<GlsHistoryEvent> History { get; set; } = new List<GlsHistoryEvent>();

        [JsonProperty("address")]
        public GlsAddress Address { get; set; }

        [JsonProperty("references")]
        public List<string> References { get; set; } = new List<string>();
    }

    public class GlsHistoryEvent
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("evtDscr")]
        public string Description { get; set; }

        [JsonProperty("address")]
        public GlsAddress Address { get; set; }
    }

    public class GlsAddress
    {
        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }
    }

    public class StoredTrackingInfo
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public TrackingInfo? LastResult { get; set; }
        public string LastStatus { get; set; } = string.Empty;
        public string LastLocation { get; set; } = string.Empty;
        public DateTime? LastUpdate { get; set; }
        public string GlsDate { get; set; } = string.Empty; // Datum vom GLS Portal
        public int DisplayIndex { get; set; } = 0; // Index für die Anzeige (1/3)
        public int TotalCount { get; set; } = 0; // Gesamtanzahl für den Kunden
        
        public bool IsDelivered => LastStatus?.Contains("Zugestellt") == true || 
                                  LastStatus?.Contains("✅") == true ||
                                  LastStatus?.Contains("Delivered") == true;
        
        public bool IsCancelled 
        {
            get
            {
                if (string.IsNullOrEmpty(LastStatus))
                    return false;
                    
                var status = LastStatus.ToLower();
                return status.Contains("cancelled") || 
                       status.Contains("canceled") || 
                       status.Contains("storniert") ||
                       status.Contains("cancel");
            }
        }
        
        public bool IsOverdue 
        {
            get
            {
                // Wenn bereits zugestellt oder gecancellt, dann nicht überfällig
                if (IsDelivered || IsCancelled)
                    return false;
                    
                // Versuche das GLS Datum zu parsen
                if (string.IsNullOrWhiteSpace(GlsDate))
                    return false;
                    
                // Verschiedene Datumsformate versuchen
                DateTime glsCreationDate = DateTime.MinValue;
                var formats = new[] { 
                    "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy",
                    "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy"
                };
                
                bool parsed = false;
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(GlsDate.Trim(), format, null, System.Globalization.DateTimeStyles.None, out glsCreationDate))
                    {
                        parsed = true;
                        break;
                    }
                }
                
                if (!parsed && !DateTime.TryParse(GlsDate.Trim(), out glsCreationDate))
                    return false;
                
                // Prüfe ob mehr als 5 Tage seit Erstellung vergangen sind
                var daysSinceCreation = (DateTime.Now - glsCreationDate).TotalDays;
                return daysSinceCreation > 5;
            }
        }
        
        public Brush StatusColor => IsOverdue ? Brushes.Red : Brushes.Black;
        
        public string DisplayText => string.IsNullOrWhiteSpace(CustomerName) 
            ? TrackingNumber 
            : $"{CustomerName} ({TrackingNumber})";
        
        // Erweiterte Anzeige mit GLS Datum und Nummerierung
        public string DisplayTextWithDate 
        {
            get
            {
                string baseText = DisplayText;
                
                // Nummerierung hinzufügen wenn mehrere Sendungen für denselben Kunden
                if (TotalCount > 1 && DisplayIndex > 0)
                {
                    if (string.IsNullOrWhiteSpace(CustomerName))
                    {
                        baseText = $"({DisplayIndex}/{TotalCount}) {TrackingNumber}";
                    }
                    else
                    {
                        baseText = $"({DisplayIndex}/{TotalCount}) {CustomerName} ({TrackingNumber})";
                    }
                }
                
                // GLS Datum hinzufügen
                return string.IsNullOrWhiteSpace(GlsDate) 
                    ? baseText 
                    : $"{baseText} - {GlsDate}";
            }
        }
    }
}
