using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GlsTrackingApp.Models;

namespace GlsTrackingApp.Services
{
    public class XmlImportService
    {
        public GLSList ImportFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Die Datei {filePath} wurde nicht gefunden.");

            try
            {
                var xmlContent = File.ReadAllText(filePath);
                
                // Debug: Zeige ersten Teil des XML-Inhalts
                System.Diagnostics.Debug.WriteLine($"XML Content (first 500 chars): {xmlContent.Substring(0, Math.Min(500, xmlContent.Length))}");
                
                // Remove DOCTYPE declaration if present (can cause issues)
                if (xmlContent.Contains("<!DOCTYPE"))
                {
                    var doctypeStart = xmlContent.IndexOf("<!DOCTYPE");
                    var doctypeEnd = xmlContent.IndexOf(">", doctypeStart) + 1;
                    xmlContent = xmlContent.Remove(doctypeStart, doctypeEnd - doctypeStart);
                    System.Diagnostics.Debug.WriteLine("DOCTYPE declaration removed");
                }

                var serializer = new XmlSerializer(typeof(GLSList));
                using var stringReader = new StringReader(xmlContent);
                var glsList = (GLSList?)serializer.Deserialize(stringReader);

                if (glsList == null)
                    throw new InvalidOperationException("Die XML-Datei konnte nicht deserialisiert werden.");

                System.Diagnostics.Debug.WriteLine($"Loaded {glsList.Elements.Count} elements from XML");

                // Filtere Header-Zeilen heraus
                var originalCount = glsList.Elements.Count;
                glsList.Elements.RemoveAll(e => e.IsHeaderRow || string.IsNullOrWhiteSpace(e.ParcelNumber));
                System.Diagnostics.Debug.WriteLine($"Filtered out {originalCount - glsList.Elements.Count} header/empty rows");

                return glsList;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fehler beim Lesen der XML-Datei: {ex.Message}", ex);
            }
        }

        public List<StoredTrackingInfo> ConvertToTrackingInfos(IEnumerable<GLSElement> selectedElements, DateTime? targetDate = null)
        {
            var trackingInfos = new List<StoredTrackingInfo>();

            foreach (var element in selectedElements)
            {
                if (!element.IsSelected) continue;

                var trackingInfo = new StoredTrackingInfo
                {
                    TrackingNumber = element.ParcelNumber,
                    CustomerName = element.Consignee,
                    CreatedDate = DateTime.Now,
                    LastStatus = element.Status,
                    LastLocation = $"{element.City}, {element.Country}",
                    LastUpdate = element.ParsedInitialDate
                };

                trackingInfos.Add(trackingInfo);
            }

            return trackingInfos;
        }
    }
}
