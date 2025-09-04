using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlsTrackingApp.Models;

namespace GlsTrackingApp.Services
{
    public class ShipmentDetail
    {
        public string TrackingNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string Recipient { get; set; } = "";
        public string Date { get; set; } = "";
    }
    
    public class GlsAuthenticationService : IDisposable
    {
        private IWebDriver? _driver;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        // Callback für Fortschritts-Updates
        public Action<string, int, int>? ProgressCallback { get; set; }

        private void UpdateProgress(string message, int current, int total)
        {
            ProgressCallback?.Invoke(message, current, total);
        }

        public async Task<bool> TestLoginAsync(string username, string password)
        {
            try
            {
                InitializeDriver();
                
                Console.WriteLine("🌐 Navigiere zur GLS-Startseite...");
                // Zuerst zur Hauptseite navigieren, um eine saubere Session zu starten
                _driver!.Navigate().GoToUrl("https://gls-group.eu/authenticate/?locale=de-AT");
                
                await Task.Delay(3000); // Warten bis Seite geladen ist
                
                // Warte auf automatische Weiterleitung zur Keycloak-Login-Seite
                var wait = new WebDriverWait(_driver, _defaultTimeout);
                
                Console.WriteLine("⏳ Warte auf Weiterleitung zur Login-Seite...");
                
                // Warten bis wir auf der Keycloak-Authentifikationsseite sind
                wait.Until(driver => driver.Url.Contains("auth.dc.gls-group.eu"));
                
                Console.WriteLine($"✅ Auf Login-Seite: {_driver.Url}");
                
                await Task.Delay(2000);
                
                // Suche Username-Feld mit mehreren Strategien
                Console.WriteLine("🔍 Suche Benutzername-Feld...");
                IWebElement? usernameField = null;
                
                try
                {
                    // Verschiedene Selektoren für das Username-Feld probieren
                    var usernameSelectors = new[]
                    {
                        By.Id("username"),
                        By.Name("username"),
                        By.CssSelector("input[name='username']"),
                        By.CssSelector("#username"),
                        By.CssSelector("input[type='text']"),
                        By.CssSelector("input[placeholder*='Benutzer']"),
                        By.CssSelector("input[placeholder*='User']"),
                        By.CssSelector("input[placeholder*='Email']"),
                        By.XPath("//input[@type='text' or @type='email']")
                    };
                    
                    foreach (var selector in usernameSelectors)
                    {
                        try
                        {
                            usernameField = wait.Until(driver => driver.FindElement(selector));
                            if (usernameField.Displayed && usernameField.Enabled)
                            {
                                Console.WriteLine($"✅ Username-Feld gefunden mit: {selector}");
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Nächsten Selektor probieren
                        }
                    }
                    
                    if (usernameField == null)
                    {
                        throw new Exception("Username-Feld nicht gefunden");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Fehler beim Finden des Username-Feldes: {ex.Message}");
                    Console.WriteLine($"📄 Aktuelle URL: {_driver.Url}");
                    Console.WriteLine($"📝 Seitentitel: {_driver.Title}");
                    
                    // Debug: Alle Input-Felder anzeigen
                    var allInputs = _driver.FindElements(By.TagName("input"));
                    Console.WriteLine($"🔍 Gefundene Input-Felder: {allInputs.Count}");
                    foreach (var input in allInputs.Take(5))
                    {
                        try
                        {
                            Console.WriteLine($"   - Type: {input.GetAttribute("type")}, Name: {input.GetAttribute("name")}, ID: {input.GetAttribute("id")}, Placeholder: {input.GetAttribute("placeholder")}");
                        }
                        catch { }
                    }
                    throw;
                }
                
                Console.WriteLine("✏️ Gebe Benutzername ein...");
                usernameField.Clear();
                usernameField.SendKeys(username);
                
                // Enter nach Benutzername drücken für Navigation zum nächsten Feld
                Console.WriteLine("⌨️ Drücke Enter nach Benutzername...");
                usernameField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                await Task.Delay(1000);
                
                // Suche Passwort-Feld
                Console.WriteLine("🔍 Suche Passwort-Feld...");
                IWebElement? passwordField = null;
                
                var passwordSelectors = new[]
                {
                    By.Id("password"),
                    By.Name("password"),
                    By.CssSelector("input[name='password']"),
                    By.CssSelector("#password"),
                    By.CssSelector("input[type='password']"),
                    By.XPath("//input[@type='password']")
                };
                
                foreach (var selector in passwordSelectors)
                {
                    try
                    {
                        passwordField = _driver.FindElement(selector);
                        if (passwordField.Displayed && passwordField.Enabled)
                        {
                            Console.WriteLine($"✅ Passwort-Feld gefunden mit: {selector}");
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // Nächsten Selektor probieren
                    }
                }
                
                if (passwordField == null)
                {
                    throw new Exception("Passwort-Feld nicht gefunden");
                }
                
                Console.WriteLine("🔑 Gebe Passwort ein...");
                passwordField.Clear();
                passwordField.SendKeys(password);
                
                await Task.Delay(1000);
                
                // Einfach Enter drücken nach Passwort-Eingabe - das ist der zuverlässigste Weg
                Console.WriteLine("⌨️ Drücke Enter zum Anmelden...");
                passwordField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                Console.WriteLine("⏳ Warte auf Login-Ergebnis...");
                await Task.Delay(5000); // Warten auf Login-Verarbeitung
                
                // Prüfe ob Login erfolgreich war
                var currentUrl = _driver.Url;
                Console.WriteLine($"📍 Nach Login URL: {currentUrl}");
                
                // Erfolg wenn wir nicht mehr auf der Login-Seite sind und keine Fehlermeldungen vorhanden sind
                bool loginSuccessful = !currentUrl.Contains("/login-actions/authenticate") && 
                                     !currentUrl.Contains("/auth/realms/gls/login") &&
                                     !_driver.PageSource.Contains("Invalid username or password") &&
                                     !_driver.PageSource.Contains("Ungültiger Benutzername oder Passwort") &&
                                     !_driver.PageSource.Contains("Account is disabled") &&
                                     !_driver.PageSource.Contains("error");
                
                if (loginSuccessful)
                {
                    Console.WriteLine($"✅ Login erfolgreich! Weitergeleitet zu: {currentUrl}");
                    
                    // Nach erfolgreichem Login zur Sendungsübersicht navigieren
                    try
                    {
                        Console.WriteLine("📦 Navigiere zur Sendungsübersicht...");
                        _driver.Navigate().GoToUrl("https://gls-group.eu/app/service/closed/page/AT/de/witt004#/");
                        await Task.Delay(5000); // Warten bis Seite geladen ist
                        
                        // Zuerst auf den Search-Button klicken
                        Console.WriteLine("🔍 Klicke auf Search-Button...");
                        await ClickSearchButtonAsync();
                        
                        Console.WriteLine("🔍 Suche nach Sendungsdetails...");
                        var shipmentDetails = await ScrapeShipmentDetailsAsync();
                        
                        if (shipmentDetails.Count > 0)
                        {
                            Console.WriteLine($"✅ {shipmentDetails.Count} Sendungen gefunden!");
                            foreach (var detail in shipmentDetails)
                            {
                                Console.WriteLine($"   📋 {detail.TrackingNumber} | Status: {detail.Status} | Empfänger: {detail.Recipient}");
                            }
                            
                            // Erweiterte Anzeige mit Details
                            var detailedMessage = $"Login erfolgreich!\n\nGefundene Sendungen ({shipmentDetails.Count}):\n\n";
                            
                            foreach (var detail in shipmentDetails)
                            {
                                detailedMessage += $"📦 {detail.TrackingNumber}\n";
                                detailedMessage += $"   📊 Status: {detail.Status}\n";
                                detailedMessage += $"   👤 Empfänger: {detail.Recipient}\n";
                                detailedMessage += $"   📅 Datum: {detail.Date}\n\n";
                            }
                            
                            System.Windows.MessageBox.Show(detailedMessage, "GLS Sendungsübersicht", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                        else
                        {
                            Console.WriteLine("⚠️ Keine Sendungsnummern gefunden");
                            System.Windows.MessageBox.Show("Login erfolgreich!\n\nEs wurden jedoch keine Sendungsnummern auf der Seite gefunden.", 
                                "GLS Login", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception scrapeEx)
                    {
                        Console.WriteLine($"⚠️ Fehler beim Scrapen der Sendungsnummern: {scrapeEx.Message}");
                        System.Windows.MessageBox.Show($"Login erfolgreich!\n\nFehler beim Laden der Sendungsnummern:\n{scrapeEx.Message}", 
                            "GLS Login", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                else
                {
                    Console.WriteLine("❌ Login fehlgeschlagen oder noch auf Login-Seite");
                    Console.WriteLine($"🌐 Aktuelle URL: {currentUrl}");
                    Console.WriteLine($"📄 Seitentitel: {_driver.Title}");
                    
                    // Suche nach Fehlermeldungen
                    try
                    {
                        var errorSelectors = new[]
                        {
                            ".alert-error", ".error", ".alert-danger", ".text-danger", 
                            "[class*='error']", "#input-error", ".kc-feedback-text",
                            ".login-error", ".auth-error"
                        };
                        
                        foreach (var selector in errorSelectors)
                        {
                            var errorElements = _driver.FindElements(By.CssSelector(selector));
                            foreach (var error in errorElements)
                            {
                                if (!string.IsNullOrWhiteSpace(error.Text))
                                {
                                    Console.WriteLine($"❗ Fehlermeldung ({selector}): {error.Text}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Fehler beim Suchen von Fehlermeldungen: {ex.Message}");
                    }
                }
                
                return loginSuccessful;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Fehler beim Login-Test: {ex.Message}");
                Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");
                
                try
                {
                    Console.WriteLine($"📍 Aktuelle URL: {_driver?.Url ?? "Unbekannt"}");
                    Console.WriteLine($"📄 Seitentitel: {_driver?.Title ?? "Unbekannt"}");
                }
                catch { }
                
                return false;
            }
            finally
            {
                CleanupDriver();
            }
        }
        
        private async Task ClickSearchButtonAsync()
        {
            UpdateProgress("🔍 Suche nach Sendungen...", 7, 10);
            
            try
            {
                // Warten bis die Seite vollständig geladen ist
                await Task.Delay(3000);
                
                Console.WriteLine("🔍 Suche Search-Button mit ID 'search'...");
                
                // Verschiedene Strategien zum Finden des Search-Buttons
                var searchButtonSelectors = new[]
                {
                    By.Id("search"),
                    By.CssSelector("#search"),
                    By.CssSelector("button#search"),
                    By.CssSelector("input#search"),
                    By.CssSelector("[id='search']"),
                    By.XPath("//button[@id='search']"),
                    By.XPath("//input[@id='search']"),
                    By.XPath("//*[@id='search']")
                };
                
                IWebElement? searchButton = null;
                
                foreach (var selector in searchButtonSelectors)
                {
                    try
                    {
                        searchButton = _driver!.FindElement(selector);
                        if (searchButton.Displayed && searchButton.Enabled)
                        {
                            Console.WriteLine($"✅ Search-Button gefunden mit: {selector}");
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // Nächsten Selektor probieren
                    }
                }
                
                if (searchButton == null)
                {
                    Console.WriteLine("⚠️ Search-Button mit ID 'search' nicht gefunden. Suche nach alternativen Buttons...");
                    
                    // Alternative Suche nach Search-Buttons
                    var alternativeSelectors = new[]
                    {
                        By.CssSelector("button[type='submit']"),
                        By.CssSelector("input[type='submit']"),
                        By.CssSelector("button:contains('Search')"),
                        By.CssSelector("button:contains('Suchen')"),
                        By.CssSelector(".search-button"),
                        By.CssSelector(".btn-search"),
                        By.XPath("//button[contains(text(),'Search')]"),
                        By.XPath("//button[contains(text(),'Suchen')]"),
                        By.XPath("//input[@value='Search']"),
                        By.XPath("//input[@value='Suchen']")
                    };
                    
                    foreach (var selector in alternativeSelectors)
                    {
                        try
                        {
                            searchButton = _driver!.FindElement(selector);
                            if (searchButton.Displayed && searchButton.Enabled)
                            {
                                Console.WriteLine($"✅ Alternative Search-Button gefunden mit: {selector}");
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Nächsten Selektor probieren
                        }
                    }
                }
                
                if (searchButton != null)
                {
                    Console.WriteLine("🖱️ Klicke auf Search-Button...");
                    
                    // Scroll zum Button falls nötig
                    ((IJavaScriptExecutor)_driver!).ExecuteScript("arguments[0].scrollIntoView(true);", searchButton);
                    await Task.Delay(1000);
                    
                    // Klick auf den Button
                    searchButton.Click();
                    
                    Console.WriteLine("✅ Search-Button geklickt. Warte auf Ergebnisse...");
                    await Task.Delay(5000); // Warten auf Lade der Suchergebnisse
                }
                else
                {
                    Console.WriteLine("❌ Kein Search-Button gefunden. Liste alle verfügbaren Buttons auf:");
                    
                    // Debug: Alle verfügbaren Buttons anzeigen
                    var allButtons = _driver!.FindElements(By.TagName("button"));
                    var allInputs = _driver!.FindElements(By.CssSelector("input[type='submit'], input[type='button']"));
                    
                    Console.WriteLine($"🔍 Gefundene Buttons: {allButtons.Count}");
                    foreach (var button in allButtons.Take(10))
                    {
                        try
                        {
                            Console.WriteLine($"   - Button: ID='{button.GetAttribute("id")}', Class='{button.GetAttribute("class")}', Text='{button.Text}', Type='{button.GetAttribute("type")}'");
                        }
                        catch { }
                    }
                    
                    Console.WriteLine($"🔍 Gefundene Input-Buttons: {allInputs.Count}");
                    foreach (var input in allInputs.Take(5))
                    {
                        try
                        {
                            Console.WriteLine($"   - Input: ID='{input.GetAttribute("id")}', Class='{input.GetAttribute("class")}', Value='{input.GetAttribute("value")}', Type='{input.GetAttribute("type")}'");
                        }
                        catch { }
                    }
                    
                    throw new Exception("Search-Button konnte nicht gefunden werden");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Klicken des Search-Buttons: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Lädt alle verfügbaren Sendungen aus dem GLS Portal - Basierend auf der funktionierenden TestLoginAsync Methode
        /// </summary>
        public async Task<List<ShipmentDetail>> LoadAllShipmentsAsync(string username, string password)
        {
            try
            {
                ProgressCallback?.Invoke("🌐 Navigiere zur GLS-Startseite...", 1, 10);
                
                InitializeDriver();
                
                Console.WriteLine("🌐 Navigiere zur GLS-Startseite...");
                // Zuerst zur Hauptseite navigieren, um eine saubere Session zu starten
                _driver!.Navigate().GoToUrl("https://gls-group.eu/authenticate/?locale=de-AT");
                
                await Task.Delay(3000); // Warten bis Seite geladen ist
                
                ProgressCallback?.Invoke("⏳ Warte auf Weiterleitung zur Login-Seite...", 2, 10);
                
                // Warte auf automatische Weiterleitung zur Keycloak-Login-Seite
                var wait = new WebDriverWait(_driver, _defaultTimeout);
                
                Console.WriteLine("⏳ Warte auf Weiterleitung zur Login-Seite...");
                
                // Warten bis wir auf der Keycloak-Authentifikationsseite sind
                wait.Until(driver => driver.Url.Contains("auth.dc.gls-group.eu"));
                
                Console.WriteLine($"✅ Auf Login-Seite: {_driver.Url}");
                
                await Task.Delay(2000);
                
                ProgressCallback?.Invoke("🔍 Suche Anmeldefelder...", 3, 10);
                
                // Suche Username-Feld mit mehreren Strategien
                Console.WriteLine("🔍 Suche Benutzername-Feld...");
                IWebElement? usernameField = null;
                
                try
                {
                    // Verschiedene Selektoren für das Username-Feld probieren
                    var usernameSelectors = new[]
                    {
                        By.Id("username"),
                        By.Name("username"),
                        By.CssSelector("input[name='username']"),
                        By.CssSelector("#username"),
                        By.CssSelector("input[type='text']"),
                        By.CssSelector("input[placeholder*='Benutzer']"),
                        By.CssSelector("input[placeholder*='User']"),
                        By.CssSelector("input[placeholder*='Email']"),
                        By.XPath("//input[@type='text' or @type='email']")
                    };
                    
                    foreach (var selector in usernameSelectors)
                    {
                        try
                        {
                            usernameField = wait.Until(driver => driver.FindElement(selector));
                            if (usernameField.Displayed && usernameField.Enabled)
                            {
                                Console.WriteLine($"✅ Username-Feld gefunden mit: {selector}");
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Nächsten Selektor probieren
                        }
                    }
                    
                    if (usernameField == null)
                    {
                        throw new Exception("Username-Feld nicht gefunden");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Fehler beim Finden des Username-Feldes: {ex.Message}");
                    Console.WriteLine($"📄 Aktuelle URL: {_driver.Url}");
                    Console.WriteLine($"📝 Seitentitel: {_driver.Title}");
                    
                    // Debug: Alle Input-Felder anzeigen
                    var allInputs = _driver.FindElements(By.TagName("input"));
                    Console.WriteLine($"🔍 Gefundene Input-Felder: {allInputs.Count}");
                    foreach (var input in allInputs.Take(5))
                    {
                        try
                        {
                            Console.WriteLine($"   - Type: {input.GetAttribute("type")}, Name: {input.GetAttribute("name")}, ID: {input.GetAttribute("id")}, Placeholder: {input.GetAttribute("placeholder")}");
                        }
                        catch { }
                    }
                    throw;
                }
                
                Console.WriteLine("✏️ Gebe Benutzername ein...");
                usernameField.Clear();
                usernameField.SendKeys(username);
                
                // Enter nach Benutzername drücken für Navigation zum nächsten Feld
                Console.WriteLine("⌨️ Drücke Enter nach Benutzername...");
                usernameField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                await Task.Delay(1000);
                
                // Suche Passwort-Feld
                Console.WriteLine("🔍 Suche Passwort-Feld...");
                IWebElement? passwordField = null;
                
                var passwordSelectors = new[]
                {
                    By.Id("password"),
                    By.Name("password"),
                    By.CssSelector("input[name='password']"),
                    By.CssSelector("#password"),
                    By.CssSelector("input[type='password']"),
                    By.XPath("//input[@type='password']")
                };
                
                foreach (var selector in passwordSelectors)
                {
                    try
                    {
                        passwordField = _driver.FindElement(selector);
                        if (passwordField.Displayed && passwordField.Enabled)
                        {
                            Console.WriteLine($"✅ Passwort-Feld gefunden mit: {selector}");
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // Nächsten Selektor probieren
                    }
                }
                
                if (passwordField == null)
                {
                    throw new Exception("Passwort-Feld nicht gefunden");
                }
                
                Console.WriteLine("🔑 Gebe Passwort ein...");
                passwordField.Clear();
                passwordField.SendKeys(password);
                
                await Task.Delay(1000);
                
                // Einfach Enter drücken nach Passwort-Eingabe - das ist der zuverlässigste Weg
                Console.WriteLine("⌨️ Drücke Enter zum Anmelden...");
                passwordField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                Console.WriteLine("⏳ Warte auf Login-Ergebnis...");
                await Task.Delay(5000); // Warten auf Login-Verarbeitung
                
                // Prüfe ob Login erfolgreich war
                var currentUrl = _driver.Url;
                Console.WriteLine($"📍 Nach Login URL: {currentUrl}");
                
                // Erfolg wenn wir nicht mehr auf der Login-Seite sind und keine Fehlermeldungen vorhanden sind
                bool loginSuccessful = !currentUrl.Contains("/login-actions/authenticate") && 
                                     !currentUrl.Contains("/auth/realms/gls/login") &&
                                     !_driver.PageSource.Contains("Invalid username or password") &&
                                     !_driver.PageSource.Contains("Ungültiger Benutzername oder Passwort") &&
                                     !_driver.PageSource.Contains("Account is disabled") &&
                                     !_driver.PageSource.Contains("error");
                
                if (loginSuccessful)
                {
                    Console.WriteLine($"✅ Login erfolgreich! Weitergeleitet zu: {currentUrl}");
                    
                    // Nach erfolgreichem Login zur Sendungsübersicht navigieren
                    try
                    {
                        Console.WriteLine("📦 Navigiere zur Sendungsübersicht...");
                        _driver.Navigate().GoToUrl("https://gls-group.eu/app/service/closed/page/AT/de/witt004#/");
                        await Task.Delay(5000); // Warten bis Seite geladen ist
                        
                        // Zuerst auf den Search-Button klicken
                        Console.WriteLine("🔍 Klicke auf Search-Button...");
                        await ClickSearchButtonAsync();
                        
                        Console.WriteLine("🔍 Suche nach Sendungsdetails...");
                        var shipmentDetails = await ScrapeShipmentDetailsAsync();
                        
                        if (shipmentDetails.Count > 0)
                        {
                            UpdateProgress("✅ Sendungen erfolgreich geladen!", 10, 10);
                            
                            Console.WriteLine($"✅ {shipmentDetails.Count} Sendungen erfolgreich geladen!");
                            foreach (var detail in shipmentDetails)
                            {
                                Console.WriteLine($"   📋 {detail.TrackingNumber} | Status: {detail.Status} | Empfänger: {detail.Recipient} | Datum: {detail.Date}");
                            }
                            
                            return shipmentDetails;
                        }
                        else
                        {
                            Console.WriteLine("⚠️ Keine Sendungsnummern gefunden");
                            return new List<ShipmentDetail>();
                        }
                    }
                    catch (Exception scrapeEx)
                    {
                        Console.WriteLine($"⚠️ Fehler beim Scrapen der Sendungsnummern: {scrapeEx.Message}");
                        return new List<ShipmentDetail>();
                    }
                }
                else
                {
                    Console.WriteLine("❌ Login fehlgeschlagen oder noch auf Login-Seite");
                    Console.WriteLine($"🌐 Aktuelle URL: {currentUrl}");
                    Console.WriteLine($"📄 Seitentitel: {_driver.Title}");
                    
                    // Suche nach Fehlermeldungen
                    try
                    {
                        var errorSelectors = new[]
                        {
                            ".alert-error", ".error", ".alert-danger", ".text-danger", 
                            "[class*='error']", "#input-error", ".kc-feedback-text",
                            ".login-error", ".auth-error"
                        };
                        
                        foreach (var selector in errorSelectors)
                        {
                            var errorElements = _driver.FindElements(By.CssSelector(selector));
                            foreach (var error in errorElements)
                            {
                                if (!string.IsNullOrWhiteSpace(error.Text))
                                {
                                    Console.WriteLine($"❗ Fehlermeldung ({selector}): {error.Text}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Fehler beim Suchen von Fehlermeldungen: {ex.Message}");
                    }
                    
                    return new List<ShipmentDetail>();
                }
            }
            catch (Exception ex)
            {
                UpdateProgress("❌ Fehler beim Laden", 0, 10);
                Console.WriteLine($"💥 Fehler beim Laden aller Sendungen: {ex.Message}");
                Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");
                
                try
                {
                    Console.WriteLine($"📍 Aktuelle URL: {_driver?.Url ?? "Unbekannt"}");
                    Console.WriteLine($"📄 Seitentitel: {_driver?.Title ?? "Unbekannt"}");
                }
                catch { }
                
                return new List<ShipmentDetail>();
            }
            finally
            {
                CleanupDriver();
            }
        }
        
        private async Task<List<ShipmentDetail>> ScrapeShipmentDetailsAsync()
        {
            UpdateProgress("📋 Lade Sendungsdetails...", 8, 10);
            
            var shipmentDetails = new List<ShipmentDetail>();
            
            try
            {
                // Warten bis die Suchergebnisse vollständig geladen sind
                await Task.Delay(3000);
                
                Console.WriteLine("🔍 Analysiere Seitenstruktur nach Sendungsdetails...");
                
                // Primäre Suche: Spezifische <a>-Elemente mit ng-click="openDetail(parcel.tuNo, '')"
                Console.WriteLine("🎯 Suche nach spezifischen ng-click Links...");
                
                var specificSelectors = new[]
                {
                    // Exakte Übereinstimmung mit dem ng-click Attribut
                    "a[ng-click=\"openDetail(parcel.tuNo, '')\"]",
                    "a[ng-click='openDetail(parcel.tuNo, \"\")']",
                    
                    // Teilweise Übereinstimmung mit ng-click
                    "a[ng-click*='openDetail']",
                    "a[ng-click*='parcel.tuNo']",
                    
                    // Klassen-basierte Suche für ähnliche Elemente
                    "a.ng-binding[ng-click*='openDetail']",
                    "a[class*='ng-binding'][ng-click*='parcel']"
                };
                
                foreach (var selector in specificSelectors)
                {
                    try
                    {
                        var elements = _driver!.FindElements(By.CssSelector(selector));
                        Console.WriteLine($"🔍 Selektor '{selector}': {elements.Count} Elemente gefunden");
                        
                        foreach (var element in elements)
                        {
                            try
                            {
                                var trackingNumber = element.Text?.Trim();
                                if (!string.IsNullOrEmpty(trackingNumber) && IsValidTrackingNumber(trackingNumber))
                                {
                                    // Prüfe ob diese Sendungsnummer bereits erfasst wurde
                                    if (!shipmentDetails.Any(s => s.TrackingNumber == trackingNumber))
                                    {
                                        // Zusätzliche Details für diese Sendung sammeln
                                        var shipmentDetail = await ExtractShipmentDetailsAsync(element, trackingNumber);
                                        shipmentDetails.Add(shipmentDetail);
                                        
                                        Console.WriteLine($"✅ Sendung gefunden: {trackingNumber} | Status: {shipmentDetail.Status} | Empfänger: {shipmentDetail.Recipient} | Datum: {shipmentDetail.Date}");
                                    }
                                }
                            }
                            catch
                            {
                                // Element nicht zugänglich, weiter zum nächsten
                            }
                        }
                    }
                    catch
                    {
                        // Selektor nicht gültig, weiter zum nächsten
                    }
                }
                
                // Sekundäre Suche: Alle <a>-Elemente mit ng-click Attribut
                if (shipmentDetails.Count == 0)
                {
                    Console.WriteLine("🔍 Erweiterte Suche nach allen ng-click Links...");
                    
                    try
                    {
                        var allNgClickLinks = _driver!.FindElements(By.CssSelector("a[ng-click]"));
                        Console.WriteLine($"🔗 {allNgClickLinks.Count} ng-click Links gefunden");
                        
                        foreach (var link in allNgClickLinks)
                        {
                            try
                            {
                                var ngClick = link.GetAttribute("ng-click");
                                var text = link.Text?.Trim();
                                var className = link.GetAttribute("class");
                                
                                Console.WriteLine($"   - Link: ng-click='{ngClick}', text='{text}', class='{className}'");
                                
                                if (!string.IsNullOrEmpty(text) && IsValidTrackingNumber(text))
                                {
                                    if (!shipmentDetails.Any(s => s.TrackingNumber == text))
                                    {
                                        var shipmentDetail = await ExtractShipmentDetailsAsync(link, text);
                                        shipmentDetails.Add(shipmentDetail);
                                        
                                        Console.WriteLine($"✅ Sendung aus ng-click Link: {text} | Status: {shipmentDetail.Status} | Empfänger: {shipmentDetail.Recipient} | Datum: {shipmentDetail.Date}");
                                    }
                                }
                            }
                            catch
                            {
                                // Element nicht zugänglich
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Fehler bei ng-click Link Suche: {ex.Message}");
                    }
                }
                
                // Debug: Zeige Seitenstruktur wenn keine Nummern gefunden
                if (shipmentDetails.Count == 0)
                {
                    Console.WriteLine("🔍 Debug: Analysiere Seitenstruktur...");
                    
                    try
                    {
                        // Zeige alle verfügbaren ng-click Attribute
                        var allNgElements = _driver!.FindElements(By.CssSelector("[ng-click]"));
                        Console.WriteLine($"📋 {allNgElements.Count} Elemente mit ng-click gefunden:");
                        
                        foreach (var element in allNgElements.Take(10))
                        {
                            try
                            {
                                var tagName = element.TagName;
                                var ngClick = element.GetAttribute("ng-click");
                                var text = element.Text?.Trim();
                                var className = element.GetAttribute("class");
                                
                                Console.WriteLine($"   - {tagName}: ng-click='{ngClick}', text='{text}', class='{className}'");
                            }
                            catch { }
                        }
                        
                        // Suche nach Tabellen oder Listen die Sendungsnummern enthalten könnten
                        var tables = _driver!.FindElements(By.TagName("table"));
                        Console.WriteLine($"📊 {tables.Count} Tabellen gefunden");
                        
                        var lists = _driver!.FindElements(By.TagName("ul"));
                        Console.WriteLine($"📋 {lists.Count} Listen gefunden");
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Fehler bei Debug-Analyse: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"📋 Insgesamt {shipmentDetails.Count} eindeutige Sendungen gefunden");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Scrapen der Sendungsdetails: {ex.Message}");
            }
            
            return shipmentDetails;
        }
        
        private async Task<ShipmentDetail> ExtractShipmentDetailsAsync(IWebElement trackingElement, string trackingNumber)
        {
            var shipmentDetail = new ShipmentDetail
            {
                TrackingNumber = trackingNumber,
                Status = "Unbekannt",
                Recipient = "Unbekannt",
                Date = "Unbekannt"
            };
            
            try
            {
                // Versuche die Tabellenzelle mit dem Status zu finden
                // Format: <td ng-attr-id="{{'status_' + $index}}" ng-show="tableConfig.indexOf('status') >= 0" class="bold ng-binding parcel-status-2" ng-class="('parcel-status-' + parcel.progressBar.colourIndex)" id="status_0">Daten übermittelt</td>
                
                Console.WriteLine($"🔍 Suche Status für Sendung {trackingNumber}...");
                
                // Finde das übergeordnete tr-Element
                var parentRow = trackingElement;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        parentRow = parentRow.FindElement(By.XPath(".."));
                        if (parentRow.TagName.ToLower() == "tr")
                        {
                            break;
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
                
                if (parentRow?.TagName.ToLower() == "tr")
                {
                    Console.WriteLine("✅ Tabellenzeile gefunden, suche Status...");
                    
                    // Suche nach Status-Zelle in derselben Zeile
                    var statusSelectors = new[]
                    {
                        ".//td[contains(@id, 'status_')]",
                        ".//td[contains(@class, 'parcel-status')]",
                        ".//td[contains(@class, 'ng-binding') and contains(@class, 'bold')]",
                        ".//td[@ng-show and contains(@ng-show, 'status')]"
                    };
                    
                    foreach (var selector in statusSelectors)
                    {
                        try
                        {
                            var statusElement = parentRow.FindElement(By.XPath(selector));
                            if (statusElement != null && !string.IsNullOrWhiteSpace(statusElement.Text))
                            {
                                shipmentDetail.Status = statusElement.Text.Trim();
                                Console.WriteLine($"✅ Status gefunden: {shipmentDetail.Status}");
                                break;
                            }
                        }
                        catch
                        {
                            // Nächsten Selektor probieren
                        }
                    }
                    
                    // Suche nach Empfänger-Information in der spezifischen consigneeName-Zelle
                    // Format: <td ng-attr-id="{{'consigneeName_' + $index}}" id="consigneeName_0"><p class="truncate-ellipsis mb-0 ng-binding" title="Dr. Herbert Illmer">Dr. Herbert Illmer</p></td>
                    Console.WriteLine("🔍 Suche Empfänger-Information...");
                    
                    var recipientSelectors = new[]
                    {
                        // Spezifische Suche nach consigneeName-Zelle
                        ".//td[contains(@id, 'consigneeName_')]//p[contains(@class, 'truncate-ellipsis')]",
                        ".//td[contains(@id, 'consigneeName_')]//p[@title]",
                        ".//td[contains(@id, 'consigneeName_')]//p",
                        
                        // Fallback: Allgemeine Suche nach Empfänger-Pattern
                        ".//p[contains(@class, 'truncate-ellipsis') and contains(@class, 'ng-binding') and contains(@class, 'mb-0')]",
                        ".//p[contains(@class, 'truncate-ellipsis') and contains(@class, 'ng-binding')]",
                        ".//p[@ng-attr-title and contains(@class, 'truncate-ellipsis')]",
                        ".//p[@title and contains(@class, 'truncate-ellipsis')]"
                    };
                    
                    foreach (var selector in recipientSelectors)
                    {
                        try
                        {
                            var recipientElements = parentRow.FindElements(By.XPath(selector));
                            foreach (var recipientElement in recipientElements)
                            {
                                var text = recipientElement.Text?.Trim();
                                var title = recipientElement.GetAttribute("title")?.Trim();
                                var ngAttrTitle = recipientElement.GetAttribute("ng-attr-title")?.Trim();
                                
                                // Bevorzuge title-Attribut, dann ng-attr-title, dann Text
                                var potentialName = "";
                                if (!string.IsNullOrEmpty(title))
                                    potentialName = title;
                                else if (!string.IsNullOrEmpty(ngAttrTitle))
                                    potentialName = ngAttrTitle;
                                else if (!string.IsNullOrEmpty(text))
                                    potentialName = text;
                                
                                Console.WriteLine($"🔍 Prüfe potentiellen Empfänger: '{potentialName}' (title: '{title}', text: '{text}')");
                                
                                if (!string.IsNullOrEmpty(potentialName) && 
                                    potentialName.Length > 2 &&
                                    potentialName.Any(c => char.IsLetter(c)) &&
                                    !IsValidTrackingNumber(potentialName) &&
                                    !potentialName.ToLower().Contains("status") &&
                                    !potentialName.ToLower().Contains("übermittelt") &&
                                    !potentialName.ToLower().Contains("zugestellt") &&
                                    !potentialName.ToLower().Contains("daten") &&
                                    !potentialName.Equals(trackingNumber))
                                {
                                    shipmentDetail.Recipient = potentialName;
                                    Console.WriteLine($"✅ Empfänger gefunden mit Selektor '{selector}': {shipmentDetail.Recipient}");
                                    break;
                                }
                            }
                            
                            if (shipmentDetail.Recipient != "Unbekannt")
                                break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Fehler bei Empfänger-Selektor '{selector}': {ex.Message}");
                        }
                    }
                    
                    // Suche nach Datum-Information in der spezifischen date-Zelle
                    // Format: <td ng-attr-id="{{'date_' + $index}}" id="date_0"><span class="ng-binding">04.09.25</span></td>
                    Console.WriteLine("🔍 Suche Datum-Information...");
                    
                    var dateSelectors = new[]
                    {
                        // Spezifische Suche nach date-Zelle
                        ".//td[contains(@id, 'date_')]//span[contains(@class, 'ng-binding')]",
                        ".//td[contains(@id, 'date_')]//span",
                        ".//td[contains(@id, 'date_')]",
                        
                        // Fallback: Allgemeine Suche nach Datum-Pattern
                        ".//span[contains(@class, 'ng-binding') and string-length(text()) <= 10 and contains(text(), '.')]",
                        ".//td[contains(@ng-show, 'date')]//span"
                    };
                    
                    foreach (var selector in dateSelectors)
                    {
                        try
                        {
                            var dateElements = parentRow.FindElements(By.XPath(selector));
                            foreach (var dateElement in dateElements)
                            {
                                var text = dateElement.Text?.Trim();
                                
                                Console.WriteLine($"🔍 Prüfe potentielles Datum: '{text}'");
                                
                                // Prüfe ob es sich um ein Datum handelt (Format: DD.MM.YY oder ähnlich)
                                if (!string.IsNullOrEmpty(text) && 
                                    text.Length >= 6 && text.Length <= 10 &&
                                    text.Contains('.') &&
                                    !text.Equals(trackingNumber) &&
                                    !text.ToLower().Contains("status") &&
                                    !IsValidTrackingNumber(text))
                                {
                                    // Zusätzliche Validierung für Datum-Format
                                    var parts = text.Split('.');
                                    if (parts.Length >= 2 && 
                                        parts.All(part => part.Length >= 1 && part.All(c => char.IsDigit(c))))
                                    {
                                        shipmentDetail.Date = text;
                                        Console.WriteLine($"✅ Datum gefunden mit Selektor '{selector}': {shipmentDetail.Date}");
                                        break;
                                    }
                                }
                            }
                            
                            if (shipmentDetail.Date != "Unbekannt")
                                break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Fehler bei Datum-Selektor '{selector}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler beim Extrahieren der Sendungsdetails für {trackingNumber}: {ex.Message}");
            }
            
            return shipmentDetail;
        }
        
        private bool IsValidTrackingNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            
            // Bereinige den Text
            text = text.Trim();
            
            // GLS-Sendungsnummern sind typischerweise 11-15 Ziffern
            if (text.Length < 11 || text.Length > 15)
                return false;
            
            // Muss nur Ziffern enthalten
            if (!text.All(char.IsDigit))
                return false;
            
            // Ausschluss von offensichtlich ungültigen Nummern
            // (z.B. alle gleichen Ziffern oder sehr einfache Muster)
            if (text.All(c => c == text[0])) // Alle Ziffern gleich
                return false;
            
            if (text == "00000000000" || text == "11111111111") // Bekannte Test-Nummern
                return false;
            
            return true;
        }
        
        private void InitializeDriver()
        {
            var config = GlsTrackingApp.Config.AppConfig.Instance;
            var options = new ChromeOptions();
            
            // Headless-Modus für unsichtbaren Browser
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            
            // Basis-Optionen für Stabilität
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--allow-running-insecure-content");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-ssl-errors");
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArgument("--ignore-urlfetcher-cert-requests");
            
            // ZScaler/Corporate Proxy Kompatibilität
            if (config.ZScalerMode)
            {
                Console.WriteLine("🔵 ZScaler-Modus aktiviert - Spezielle Proxy-Konfiguration");
                options.AddArgument("--disable-features=VizDisplayCompositor");
                options.AddArgument("--disable-features=TranslateUI");
                options.AddArgument("--disable-ipc-flooding-protection");
                options.AddArgument("--disable-background-networking");
                options.AddArgument("--disable-background-timer-throttling");
                options.AddArgument("--disable-client-side-phishing-detection");
                options.AddArgument("--disable-default-apps");
                options.AddArgument("--disable-hang-monitor");
                options.AddArgument("--disable-prompt-on-repost");
                options.AddArgument("--disable-sync");
                options.AddArgument("--disable-domain-reliability");
                
                // ZScaler-spezifische Proxy-Einstellungen
                if (!config.DisableProxyDetection)
                {
                    options.AddArgument("--proxy-auto-detect");
                }
                else
                {
                    options.AddArgument("--no-proxy-server");
                    options.AddArgument("--proxy-bypass-list=*");
                }
            }
            else
            {
                // Standard-Proxy-Detection
                options.AddArgument("--disable-features=VizDisplayCompositor");
                options.AddArgument("--disable-features=TranslateUI");
                options.AddArgument("--disable-ipc-flooding-protection");
                options.AddArgument("--disable-background-networking");
                options.AddArgument("--disable-background-timer-throttling");
                options.AddArgument("--disable-client-side-phishing-detection");
                options.AddArgument("--disable-default-apps");
                options.AddArgument("--disable-hang-monitor");
                options.AddArgument("--disable-prompt-on-repost");
                options.AddArgument("--disable-sync");
                options.AddArgument("--disable-domain-reliability");
                
                // Proxy-Detection
                options.AddArgument("--no-proxy-server");
                options.AddArgument("--proxy-bypass-list=*");
                
                // System-Proxy verwenden (für ZScaler Integration)
                options.AddArgument("--proxy-auto-detect");
            }
            
            // Custom Proxy falls konfiguriert
            if (!string.IsNullOrEmpty(config.CustomProxyServer))
            {
                Console.WriteLine($"🌐 Custom Proxy konfiguriert: {config.CustomProxyServer}");
                options.AddArgument($"--proxy-server={config.CustomProxyServer}");
            }
            
            // Für Corporate Networks mit selbst-signiertem Zertifikat
            options.AddArgument("--allow-running-insecure-content");
            options.AddArgument("--disable-features=VizDisplayCompositor");
            options.AddArgument("--test-type");
            
            // User-Agent für bessere Kompatibilität
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            // Anti-Detection
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            
            // Für bessere Performance
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-images");
            
            // Fenster-Größe für Headless-Modus
            options.AddArgument("--window-size=1920,1080");
            
            // Command-Fenster verstecken
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            
            Console.WriteLine("🚀 Initialisiere Chrome WebDriver (Headless-Modus)...");
            
            // Proxy-Erkennung und Logging
            DetectAndLogProxySettings();
            
            try
            {
                _driver = new ChromeDriver(service, options);
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60); // Verlängert für VPN
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15); // Verlängert für VPN
                
                // Test der Internetverbindung
                TestInternetConnectivity();
                
                // Anti-Detection JavaScript ausführen
                var script = @"
                    Object.defineProperty(navigator, 'webdriver', {
                        get: () => undefined,
                    });
                    window.chrome = {
                        runtime: {},
                    };
                    Object.defineProperty(navigator, 'languages', {
                        get: () => ['de-DE', 'de', 'en-US', 'en'],
                    });
                    Object.defineProperty(navigator, 'plugins', {
                        get: () => [1, 2, 3, 4, 5],
                    });
                ";
                
                ((IJavaScriptExecutor)_driver).ExecuteScript(script);
                Console.WriteLine("✅ Chrome WebDriver erfolgreich initialisiert");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Initialisieren des WebDrivers: {ex.Message}");
                throw;
            }
        }
        
        private void DetectAndLogProxySettings()
        {
            try
            {
                Console.WriteLine("🔍 Überprüfe Proxy-Einstellungen...");
                
                // System-Proxy abfragen
                var proxySettings = System.Net.WebRequest.GetSystemWebProxy();
                var testUri = new Uri("https://gls-group.eu");
                var proxyUri = proxySettings?.GetProxy(testUri);
                
                if (proxyUri != null && !proxyUri.Equals(testUri))
                {
                    Console.WriteLine($"🌐 System-Proxy erkannt: {proxyUri}");
                    Console.WriteLine("💡 Hinweis: Proxy-Auto-Detect aktiviert für ZScaler-Kompatibilität");
                }
                else
                {
                    Console.WriteLine("🔴 Kein System-Proxy gefunden");
                }
                
                // ZScaler-spezifische Erkennung
                CheckForZScaler();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Proxy-Erkennung fehlgeschlagen: {ex.Message}");
            }
        }
        
        private void CheckForZScaler()
        {
            try
            {
                // Typische ZScaler-Prozesse und Services
                var zscalerIndicators = new[] { "ZSATunnel", "ZSAService", "ZscalerService" };
                var processes = System.Diagnostics.Process.GetProcesses();
                
                foreach (var indicator in zscalerIndicators)
                {
                    if (processes.Any(p => p.ProcessName.Contains(indicator)))
                    {
                        Console.WriteLine($"🔵 ZScaler-Komponente erkannt: {indicator}");
                        Console.WriteLine("💡 Empfehlung: Zusätzliche Proxy-Optionen aktiviert");
                        return;
                    }
                }
                
                Console.WriteLine("🟢 Keine ZScaler-Komponenten erkannt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ZScaler-Erkennung fehlgeschlagen: {ex.Message}");
            }
        }
        
        private void TestInternetConnectivity()
        {
            try
            {
                Console.WriteLine("🌐 Teste Internetverbindung...");
                _driver!.Navigate().GoToUrl("about:blank");
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("✅ Basis-Navigation funktioniert");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Internetverbindungstest fehlgeschlagen: {ex.Message}");
                throw new Exception("Internetverbindung über WebDriver nicht möglich. Möglicherweise blockiert ZScaler die Verbindung.", ex);
            }
        }
        
        private void CleanupDriver()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler beim Schließen des Browsers: {ex.Message}");
            }
            finally
            {
                _driver = null;
            }
        }
        
        public async Task<TrackingInfo?> TrackPackageAsync(string trackingNumber, string username, string password)
        {
            try
            {
                InitializeDriver();
                
                Console.WriteLine($"🔍 Starte GLS Portal Tracking für: {trackingNumber}");
                
                // Login zum GLS Portal
                _driver!.Navigate().GoToUrl("https://gls-group.eu/authenticate/?locale=de-AT");
                await Task.Delay(3000);
                
                var wait = new WebDriverWait(_driver, _defaultTimeout);
                
                // Warte auf Weiterleitung zur Login-Seite
                wait.Until(driver => driver.Url.Contains("auth.dc.gls-group.eu"));
                
                // Login durchführen
                var usernameField = wait.Until(driver => driver.FindElement(By.Id("username")));
                usernameField.Clear();
                usernameField.SendKeys(username);
                usernameField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                await Task.Delay(1000);
                
                var passwordField = _driver.FindElement(By.Id("password"));
                passwordField.Clear();
                passwordField.SendKeys(password);
                passwordField.SendKeys(OpenQA.Selenium.Keys.Enter);
                
                await Task.Delay(5000);
                
                // Zur Sendungsübersicht navigieren
                _driver.Navigate().GoToUrl("https://gls-group.eu/app/service/closed/page/AT/de/witt004#/");
                await Task.Delay(5000);
                
                // Search-Button klicken
                await ClickSearchButtonAsync();
                
                // Alle Sendungsdetails abrufen
                var shipmentDetails = await ScrapeShipmentDetailsAsync();
                
                // Die gewünschte Sendung finden
                var targetShipment = shipmentDetails.FirstOrDefault(s => s.TrackingNumber == trackingNumber);
                
                if (targetShipment != null)
                {
                    Console.WriteLine($"✅ Sendung gefunden: {targetShipment.TrackingNumber} | Status: {targetShipment.Status}");
                    
                    // Konvertiere ShipmentDetail zu TrackingInfo
                    var trackingInfo = new TrackingInfo
                    {
                        TrackingNumber = targetShipment.TrackingNumber,
                        Status = targetShipment.Status,
                        LastUpdate = DateTime.Now,
                        Location = "GLS Portal"
                    };
                    
                    // Events-Collection ist nicht verfügbar in TrackingInfo, daher setzen wir die Description
                    // trackingInfo.Description = $"Empfänger: {targetShipment.Recipient} | Datum: {targetShipment.Date}";
                    
                    return trackingInfo;
                }
                else
                {
                    Console.WriteLine($"⚠️ Sendung {trackingNumber} nicht gefunden");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Tracking von {trackingNumber}: {ex.Message}");
                return null;
            }
            finally
            {
                CleanupDriver();
            }
        }
        
        public void Dispose()
        {
            CleanupDriver();
        }
    }
}
