using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using GlsTrackingApp.Models;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace GlsTrackingApp.Services
{
    public class SeleniumTrackingService : IDisposable
    {
        private IWebDriver? _driver;
        private readonly bool _headless;
        private bool _disposed = false;

        public SeleniumTrackingService(bool headless = true)
        {
            _headless = headless;
        }

        public async Task<TrackingInfo?> TrackPackageAsync(string trackingNumber)
        {
            try
            {
                Console.WriteLine($"[Selenium] 🔍 Starte Tracking für: {trackingNumber}");
                
                await InitializeDriverAsync();
                
                string url = $"https://gls-group.eu/GROUP/en/parcel-tracking?match={trackingNumber}";
                Console.WriteLine($"[Selenium] 🌐 Navigiere zu: {url}");
                
                _driver!.Navigate().GoToUrl(url);
                await WaitForPageLoadAsync();
                
                await EnsureTrackingNumberEnteredAsync(trackingNumber);
                await WaitForTrackingDataAsync();
                
                var status = ExtractTrackingStatus();
                
                if (status.HasValue)
                {
                    Console.WriteLine($"[Selenium] ✅ Status gefunden: {status.Value.Status}");
                    
                    return new TrackingInfo
                    {
                        TrackingNumber = trackingNumber,
                        Status = status.Value.Status,
                        Location = status.Value.Location,
                        LastUpdate = DateTime.Now,
                        Description = "Status über Browser-Automation ermittelt",
                        Address = new DeliveryAddress 
                        { 
                            CountryCode = "AT",
                            ZipCode = "",
                            City = status.Value.Location
                        },
                        Events = ExtractAllStatusEvents()
                    };
                }
                else
                {
                    Console.WriteLine("[Selenium] ❌ Kein Status gefunden");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler: {ex.Message}");
                return null;
            }
            finally
            {
                await Task.Delay(200); // Reduziert von 1000 auf 200ms
                Dispose();
            }
        }

        private async Task InitializeDriverAsync()
        {
            var options = new ChromeOptions();
            
            if (_headless)
                options.AddArgument("--headless");
            
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-gpu-sandbox");
            options.AddArgument("--disable-software-rasterizer");
            options.AddArgument("--disable-background-timer-throttling");
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-features=TranslateUI");
            options.AddArgument("--disable-ipc-flooding-protection");
            options.AddArgument("--silent");
            options.AddArgument("--disable-logging");
            options.AddArgument("--log-level=3");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
            
            // Zusätzliche Performance-Optimierungen
            options.AddArgument("--disable-images"); // Bilder nicht laden
            options.AddArgument("--disable-javascript"); // JavaScript deaktivieren (falls möglich)
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--disable-features=VizDisplayCompositor");
            options.AddArgument("--aggressive-cache-discard");
            
            Console.WriteLine($"[Selenium] 🚀 Chrome starten (headless: {_headless}, optimiert für Geschwindigkeit)...");
            
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            service.SuppressInitialDiagnosticInformation = true;
            
            _driver = new ChromeDriver(service, options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5); // Reduziert für bessere Performance
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15); // Seitenladezeit-Limit
        }

        private async Task WaitForPageLoadAsync()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15)); // Reduziert von 30 auf 15 Sekunden
            
            await Task.Run(() =>
            {
                wait.Until(driver => ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return document.readyState").Equals("complete"));
            });
            
            Console.WriteLine("[Selenium] 📄 Seite geladen");
            
            // Debug: Protokolliere alle Status-SVG-URLs
            await LogStatusSvgUrls();
        }

        private async Task EnsureTrackingNumberEnteredAsync(string trackingNumber)
        {
            try
            {
                var inputField = _driver!.FindElement(By.Id("witt002_search_match_input"));
                
                if (string.IsNullOrEmpty(inputField.GetAttribute("value")))
                {
                    Console.WriteLine("[Selenium] 📝 Gebe Tracking-Nummer ein...");
                    inputField.Clear();
                    inputField.SendKeys(trackingNumber);
                    
                    var searchButton = _driver.FindElement(By.Id("witt002_search_button"));
                    searchButton.Click();
                    
                    await Task.Delay(1000); // Reduziert von 2000 auf 1000ms
                }
                else
                {
                    Console.WriteLine("[Selenium] ✅ Tracking-Nummer bereits eingegeben");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler beim Eingeben der Tracking-Nummer: {ex.Message}");
            }
        }

        private async Task WaitForTrackingDataAsync()
        {
            const int maxAttempts = 10;
            int attempt = 0;
            
            while (attempt < maxAttempts)
            {
                try
                {
                    var statusElements = _driver!.FindElements(By.CssSelector("[id*='witt002_details_status_value_current'], [class*='status'], [class*='progress']"));
                    
                    if (statusElements.Any(element => !string.IsNullOrWhiteSpace(element.Text)))
                    {
                        Console.WriteLine($"[Selenium] ✅ Tracking-Daten gefunden nach {attempt + 1} Versuchen");
                        return;
                    }
                    
                    Console.WriteLine($"[Selenium] ⏳ Warte auf Daten... Versuch {attempt + 1}/{maxAttempts}");
                    await Task.Delay(500); // Reduziert von 1000 auf 500ms
                    attempt++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Selenium] ⚠️ Fehler beim Warten: {ex.Message}");
                    await Task.Delay(500); // Reduziert von 1000 auf 500ms
                    attempt++;
                }
            }
            
            Console.WriteLine("[Selenium] ⏰ Timeout beim Warten auf Tracking-Daten");
        }

        private (string Status, string Location)? ExtractTrackingStatus()
        {
            try
            {
                // Methode 1: Status aus SVG-URLs extrahieren (zuverlässigste Methode)
                var statusFromSvg = ExtractStatusFromSvgUrls();
                if (statusFromSvg.HasValue)
                {
                    return statusFromSvg;
                }

                // Methode 2: Suche nach dem spezifischen Current-Status-Element
                var currentStatusElement = _driver!.FindElement(By.Id("witt002_details_status_value_current"));
                var statusText = currentStatusElement.Text?.Trim();
                
                if (!string.IsNullOrWhiteSpace(statusText))
                {
                    var emoji = GetStatusEmoji(statusText);
                    return ($"{emoji} {statusText}", "GLS Website (Current)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler bei Current-Status-Extraktion: {ex.Message}");
            }
            
            // Methode 3: Extraktion aus der StatusBar
            var statusFromBar = ExtractStatusFromBar();
            if (statusFromBar.HasValue)
            {
                return statusFromBar;
            }
            
            // Methode 4: Fallback-Methoden
            return ExtractFallbackStatus();
        }

        private (string Status, string Location)? ExtractStatusFromSvgUrls()
        {
            try
            {
                Console.WriteLine("[Selenium] 🔍 Analysiere SVG-Status-URLs...");
                
                // Suche nach allen IMG-Elementen mit GLS-Status-SVG-URLs
                var statusImages = _driver!.FindElements(By.XPath("//img[contains(@src, 'gls_group_2021_witt000_status_')]"));
                
                var allStatuses = new List<(string name, string state, string url)>();
                
                foreach (var img in statusImages)
                {
                    var src = img.GetAttribute("src");
                    if (string.IsNullOrEmpty(src)) continue;
                    
                    Console.WriteLine($"[Selenium] 📊 Gefundene Status-SVG: {src}");
                    
                    // Extrahiere den Status aus der URL
                    // Format: gls_group_2021_witt000_status_STATUSNAME_STATE_svg.svg
                    var match = System.Text.RegularExpressions.Regex.Match(src, 
                        @"gls_group_2021_witt000_status_([^_]+)_([^_]+)_svg\.svg");
                    
                    if (match.Success)
                    {
                        var statusName = match.Groups[1].Value;
                        var statusState = match.Groups[2].Value;
                        allStatuses.Add((statusName, statusState, src));
                        
                        Console.WriteLine($"[Selenium] 📋 Status gefunden: {statusName} -> {statusState}");
                    }
                }
                
                // Priorisierung: 1. current, 2. complete (neueste), 3. pending
                var currentStatus = allStatuses.FirstOrDefault(s => s.state.ToLower() == "current");
                if (currentStatus.name != null)
                {
                    var translatedStatus = TranslateGlsStatus(currentStatus.name, currentStatus.state);
                    var emoji = GetGlsStatusEmoji(currentStatus.name, currentStatus.state);
                    
                    Console.WriteLine($"[Selenium] ✅ Aktueller Status aus SVG: {currentStatus.name}_{currentStatus.state} -> {translatedStatus}");
                    
                    return ($"{emoji} {translatedStatus}", "GLS Website (SVG-Current)");
                }
                
                // Falls kein current, nehme den letzten complete-Status
                var completeStatuses = allStatuses.Where(s => s.state.ToLower() == "complete").ToList();
                if (completeStatuses.Any())
                {
                    var latestComplete = completeStatuses.Last(); // Letzter in der Liste = aktuellster
                    var translatedStatus = TranslateGlsStatus(latestComplete.name, latestComplete.state);
                    var emoji = GetGlsStatusEmoji(latestComplete.name, latestComplete.state);
                    
                    Console.WriteLine($"[Selenium] ✅ Letzter abgeschlossener Status: {latestComplete.name}_{latestComplete.state} -> {translatedStatus}");
                    
                    return ($"{emoji} {translatedStatus}", "GLS Website (SVG-Complete)");
                }
                
                // Falls nur pending verfügbar
                var pendingStatus = allStatuses.FirstOrDefault(s => s.state.ToLower() == "pending");
                if (pendingStatus.name != null)
                {
                    var translatedStatus = TranslateGlsStatus(pendingStatus.name, pendingStatus.state);
                    var emoji = GetGlsStatusEmoji(pendingStatus.name, pendingStatus.state);
                    
                    Console.WriteLine($"[Selenium] ⏳ Ausstehender Status: {pendingStatus.name}_{pendingStatus.state} -> {translatedStatus}");
                    
                    return ($"{emoji} {translatedStatus}", "GLS Website (SVG-Pending)");
                }
                
                Console.WriteLine("[Selenium] ❌ Keine verwertbaren Status-SVGs gefunden");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler bei SVG-Status-Extraktion: {ex.Message}");
            }
            
            return null;
        }

        private string TranslateGlsStatus(string statusName, string statusState)
        {
            var baseStatus = statusName.ToLower() switch
            {
                "delivered" => "Zugestellt",
                "indelivery" => "In Zustellung",
                "intransit" => "In Transit",
                "inwarehouse" => "Im Lager",
                "preadvice" => "Voravisiert",
                "collected" => "Abgeholt",
                "sorted" => "Sortiert",
                "loaded" => "Geladen",
                "dispatched" => "Versandt",
                "processed" => "Verarbeitet",
                "received" => "Eingegangen",
                _ => statusName
            };
            
            var stateModifier = statusState.ToLower() switch
            {
                "current" => "",  // Aktueller Status braucht keinen Zusatz
                "pending" => " (Geplant)",
                "complete" => " (Erledigt)",
                _ => $" ({statusState})"
            };
            
            return baseStatus + stateModifier;
        }

        private string GetGlsStatusEmoji(string statusName, string statusState)
        {
            // Für current status (aktueller Status) verwende spezifische Icons
            if (statusState.ToLower() == "current")
            {
                return statusName.ToLower() switch
                {
                    "delivered" => "✅",
                    "indelivery" => "🚚",
                    "intransit" => "🚛",
                    "inwarehouse" => "📦",
                    "preadvice" => "📋",
                    "collected" => "📤",
                    "sorted" => "🔄",
                    "loaded" => "📦",
                    "dispatched" => "🚀",
                    "processed" => "⚙️",
                    "received" => "📥",
                    _ => "🟢"  // Grün für aktuellen Status
                };
            }
            
            // Für complete status (bereits passiert)
            if (statusState.ToLower() == "complete")
            {
                return statusName.ToLower() switch
                {
                    "delivered" => "✅",
                    "indelivery" => "✅",
                    "intransit" => "✅", 
                    "inwarehouse" => "✅",
                    "preadvice" => "✅",
                    "collected" => "✅",
                    "sorted" => "✅",
                    "loaded" => "✅",
                    "dispatched" => "✅",
                    "processed" => "✅",
                    "received" => "✅",
                    _ => "⚪"  // Weiß für erledigte Schritte
                };
            }
            
            // Für pending status (noch nicht passiert)
            if (statusState.ToLower() == "pending")
            {
                return "🟡";  // Gelb für geplante Schritte
            }
            
            return "❓";  // Fallback
        }

        private List<TrackingEvent> ExtractAllStatusEvents()
        {
            var events = new List<TrackingEvent>();
            
            try
            {
                Console.WriteLine("[Selenium] 📋 Sammle alle Status-Events...");
                
                // Suche nach allen Status-SVG-URLs
                var statusImages = _driver!.FindElements(By.XPath("//img[contains(@src, 'gls_group_2021_witt000_status_')]"));
                
                foreach (var img in statusImages)
                {
                    var src = img.GetAttribute("src");
                    if (string.IsNullOrEmpty(src)) continue;
                    
                    var match = System.Text.RegularExpressions.Regex.Match(src, 
                        @"gls_group_2021_witt000_status_([^_]+)_([^_]+)_svg\.svg");
                    
                    if (match.Success)
                    {
                        var statusName = match.Groups[1].Value;
                        var statusState = match.Groups[2].Value;
                        
                        var translatedStatus = TranslateGlsStatus(statusName, statusState);
                        var emoji = GetGlsStatusEmoji(statusName, statusState);
                        
                        // Bestimme Event-Zeit basierend auf Status-Zustand
                        var eventTime = statusState.ToLower() switch
                        {
                            "complete" => DateTime.Now.AddHours(-GetHoursAgoForStatus(statusName)),
                            "current" => DateTime.Now,
                            "pending" => DateTime.Now.AddHours(GetHoursInFutureForStatus(statusName)),
                            _ => DateTime.Now
                        };
                        
                        events.Add(new TrackingEvent
                        {
                            DateTime = eventTime,
                            Status = $"{emoji} {translatedStatus}",
                            Location = "GLS System",
                            Description = GetStatusDescription(statusName, statusState)
                        });
                        
                        Console.WriteLine($"[Selenium] 📝 Event hinzugefügt: {translatedStatus} um {eventTime:dd.MM.yyyy HH:mm}");
                    }
                }
                
                // Sortiere Events chronologisch
                events = events.OrderBy(e => e.DateTime).ToList();
                
                Console.WriteLine($"[Selenium] ✅ {events.Count} Status-Events gesammelt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler beim Sammeln der Events: {ex.Message}");
            }
            
            return events;
        }

        private int GetHoursAgoForStatus(string statusName)
        {
            // Geschätzte Zeiten für bereits passierte Events
            return statusName.ToLower() switch
            {
                "received" => 72,      // 3 Tage
                "processed" => 48,     // 2 Tage  
                "sorted" => 24,        // 1 Tag
                "loaded" => 12,        // 12 Stunden
                "dispatched" => 8,     // 8 Stunden
                "intransit" => 4,      // 4 Stunden
                "inwarehouse" => 2,    // 2 Stunden
                "preadvice" => 96,     // 4 Tage
                _ => 1
            };
        }

        private int GetHoursInFutureForStatus(string statusName)
        {
            // Geschätzte Zeiten für zukünftige Events
            return statusName.ToLower() switch
            {
                "indelivery" => 2,     // 2 Stunden
                "delivered" => 4,      // 4 Stunden
                _ => 24
            };
        }

        private string GetStatusDescription(string statusName, string statusState)
        {
            var baseDescription = statusName.ToLower() switch
            {
                "received" => "Sendung wurde im GLS-System erfasst",
                "processed" => "Sendung wurde bearbeitet",
                "sorted" => "Sendung wurde sortiert",
                "loaded" => "Sendung wurde auf Fahrzeug geladen",
                "dispatched" => "Sendung wurde versandt", 
                "intransit" => "Sendung ist unterwegs",
                "inwarehouse" => "Sendung befindet sich im Lager",
                "indelivery" => "Sendung ist in der Zustellung",
                "delivered" => "Sendung wurde zugestellt",
                "preadvice" => "Sendung wurde voravisiert",
                "collected" => "Sendung wurde abgeholt",
                _ => $"Status: {statusName}"
            };
            
            var stateDescription = statusState.ToLower() switch
            {
                "complete" => " - Abgeschlossen",
                "current" => " - Aktueller Status", 
                "pending" => " - Geplant",
                _ => ""
            };
            
            return baseDescription + stateDescription;
        }

        private async Task LogStatusSvgUrls()
        {
            try
            {
                Console.WriteLine("[Selenium] 🔍 Suche nach Status-SVG-URLs...");
                
                // Suche nach allen SVG-Bildern die Status enthalten könnten
                var allImages = _driver!.FindElements(By.TagName("img"));
                var statusSvgs = new List<string>();
                
                foreach (var img in allImages)
                {
                    var src = img.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src) && src.Contains("gls_group_2021_witt000_status_"))
                    {
                        statusSvgs.Add(src);
                        Console.WriteLine($"[Selenium] 📊 Status-SVG gefunden: {src}");
                    }
                }
                
                if (statusSvgs.Count == 0)
                {
                    Console.WriteLine("[Selenium] ❌ Keine Status-SVG-URLs gefunden");
                    
                    // Debug: Zeige alle SVG-URLs
                    var allSvgs = allImages.Where(img => 
                    {
                        var src = img.GetAttribute("src");
                        return !string.IsNullOrEmpty(src) && src.EndsWith(".svg");
                    }).ToList();
                    
                    Console.WriteLine($"[Selenium] 🔍 Alle SVG-URLs gefunden ({allSvgs.Count}):");
                    foreach (var svg in allSvgs)
                    {
                        Console.WriteLine($"[Selenium] 📄 SVG: {svg.GetAttribute("src")}");
                    }
                }
                else
                {
                    Console.WriteLine($"[Selenium] ✅ {statusSvgs.Count} Status-SVG-URLs gefunden");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler beim Protokollieren der SVG-URLs: {ex.Message}");
            }
        }

        private (string Status, string Location)? ExtractStatusFromBar()
        {
            try
            {
                var statusBarElement = _driver!.FindElement(By.Id("witt002_details_statusBar"));
                var statusText = statusBarElement.Text?.Trim();
                
                if (!string.IsNullOrWhiteSpace(statusText))
                {
                    var emoji = GetStatusEmoji(statusText);
                    return ($"{emoji} {statusText}", "GLS Website (StatusBar)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler bei StatusBar-Extraktion: {ex.Message}");
            }
            
            return null;
        }

        private (string Status, string Location)? ExtractFallbackStatus()
        {
            try
            {
                var statusElements = _driver!.FindElements(By.CssSelector(".status-text, .current-status, [class*='status'][class*='current']"));
                
                foreach (var element in statusElements)
                {
                    var text = element.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text) && text.Length > 3)
                    {
                        var emoji = GetStatusEmoji(text);
                        return ($"{emoji} {text}", "GLS Website (Fallback)");
                    }
                }
                
                var progressElements = _driver!.FindElements(By.CssSelector("[class*='progress'], [class*='step'][class*='active']"));
                
                foreach (var element in progressElements)
                {
                    var text = element.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text) && text.Length > 3)
                    {
                        var emoji = GetStatusEmoji(text);
                        return ($"📊 {text}", "GLS Website (Progress)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler bei Fallback-Status-Extraktion: {ex.Message}");
            }
            
            return null;
        }

        private string GetStatusEmoji(string status)
        {
            var lowerStatus = status.ToLower();
            
            if (lowerStatus.Contains("delivered") || lowerStatus.Contains("zugestellt"))
                return "✅";
            if (lowerStatus.Contains("delivery") || lowerStatus.Contains("zustellung"))
                return "🚚";
            if (lowerStatus.Contains("transit") || lowerStatus.Contains("transport"))
                return "🚛";
            if (lowerStatus.Contains("warehouse") || lowerStatus.Contains("lager"))
                return "📦";
            if (lowerStatus.Contains("collected") || lowerStatus.Contains("abgeholt"))
                return "📤";
            if (lowerStatus.Contains("sorted") || lowerStatus.Contains("sortiert"))
                return "🔄";
            if (lowerStatus.Contains("preadvice") || lowerStatus.Contains("voravisiert"))
                return "📋";
            
            return "📍";
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                if (_driver == null)
                    return false;
                
                _driver.Navigate().GoToUrl("https://gls-group.eu");
                await Task.Delay(3000);
                
                return _driver.Title.Contains("GLS");
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
                Console.WriteLine("[Selenium] 🔄 Browser geschlossen");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Selenium] ⚠️ Fehler beim Schließen: {ex.Message}");
            }
            
            _disposed = true;
        }
    }
}
