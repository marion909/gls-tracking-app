using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GlsTrackingApp.Models
{
    [XmlRoot("GLSList")]
    public class GLSList
    {
        [XmlElement("GLSElement")]
        public List<GLSElement> Elements { get; set; } = new List<GLSElement>();
    }

    public class GLSElement
    {
        [XmlElement("ParcelNumber")]
        public string ParcelNumber { get; set; } = string.Empty;

        [XmlElement("InitialDate")]
        public string InitialDate { get; set; } = string.Empty;

        [XmlElement("Status")]
        public string Status { get; set; } = string.Empty;

        [XmlElement("ZipCode")]
        public string ZipCode { get; set; } = string.Empty;

        [XmlElement("Country")]
        public string Country { get; set; } = string.Empty;

        [XmlElement("City")]
        public string City { get; set; } = string.Empty;

        [XmlElement("Consignee")]
        public string Consignee { get; set; } = string.Empty;

        // Hilfseigenschaft für die Anzeige
        public string DisplayText => $"{ParcelNumber} - {Consignee}";

        // Parsed Date für bessere Handhabung
        public DateTime? ParsedInitialDate
        {
            get
            {
                // Versuche verschiedene Datumsformate
                var formats = new[]
                {
                    "dd/MM/yyyy HH:mm:ss:fff",
                    "dd/MM/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm:ss:fff",
                    "MM/dd/yyyy HH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(InitialDate, format, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var date))
                    {
                        return date;
                    }
                }

                // Fallback: Versuche normales Parsing
                if (DateTime.TryParse(InitialDate, out var fallbackDate))
                {
                    return fallbackDate;
                }

                return null;
            }
        }

        // Für UI-Binding
        public bool IsSelected { get; set; } = false;

        // Prüfe ob es sich um eine Header-Zeile handelt
        public bool IsHeaderRow => ParcelNumber == "PaketNr." || ParcelNumber == "ParcelNumber";
    }
}
