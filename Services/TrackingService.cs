using System;
using System.Threading.Tasks;
using GlsTrackingApp.Models;
using GlsTrackingApp.Config;

namespace GlsTrackingApp.Services
{
    /// <summary>
    /// Hauptservice für Paket-Tracking der zwischen verschiedenen Tracking-Methoden wählt
    /// </summary>
    public class TrackingService : IDisposable
    {
        private readonly SeleniumTrackingService _seleniumService;
        private readonly GlsAuthenticationService _glsPortalService;
        private readonly AppConfig _config;
        private bool _disposed = false;

        public TrackingService()
        {
            _seleniumService = new SeleniumTrackingService(headless: true);
            _glsPortalService = new GlsAuthenticationService();
            _config = AppConfig.Instance;
        }

        /// <summary>
        /// Verfolgt ein Paket mit der konfigurierten Methode
        /// </summary>
        public async Task<TrackingInfo?> TrackPackageAsync(string trackingNumber)
        {
            try
            {
                // Prüfe ob GLS Portal-Tracking aktiviert und konfiguriert ist
                if (_config.UseGlsPortalTracking && 
                    !string.IsNullOrEmpty(_config.GlsUsername) && 
                    !string.IsNullOrEmpty(_config.GlsPassword))
                {
                    Console.WriteLine($"🌐 Verwende GLS Portal-Tracking für: {trackingNumber}");
                    
                    var result = await _glsPortalService.TrackPackageAsync(
                        trackingNumber, 
                        _config.GlsUsername, 
                        _config.GlsPassword);
                    
                    if (result != null)
                    {
                        Console.WriteLine($"✅ GLS Portal-Tracking erfolgreich für: {trackingNumber}");
                        return result;
                    }
                    
                    Console.WriteLine($"⚠️ GLS Portal-Tracking fehlgeschlagen, verwende Fallback für: {trackingNumber}");
                }
                
                // Fallback auf Selenium-Service
                Console.WriteLine($"🔄 Verwende Standard-Tracking für: {trackingNumber}");
                return await _seleniumService.TrackPackageAsync(trackingNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Tracking von {trackingNumber}: {ex.Message}");
                
                // Bei Fehler im GLS Portal, versuche Standard-Tracking
                if (_config.UseGlsPortalTracking)
                {
                    Console.WriteLine($"🔄 Versuche Standard-Tracking als Fallback für: {trackingNumber}");
                    try
                    {
                        return await _seleniumService.TrackPackageAsync(trackingNumber);
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"❌ Auch Standard-Tracking fehlgeschlagen für {trackingNumber}: {fallbackEx.Message}");
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// Testet die GLS Portal-Anmeldung
        /// </summary>
        public async Task<bool> TestGlsLoginAsync()
        {
            if (string.IsNullOrEmpty(_config.GlsUsername) || string.IsNullOrEmpty(_config.GlsPassword))
            {
                Console.WriteLine("⚠️ GLS-Anmeldedaten nicht konfiguriert");
                return false;
            }
            
            return await _glsPortalService.TestLoginAsync(_config.GlsUsername, _config.GlsPassword);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _seleniumService?.Dispose();
                _glsPortalService?.Dispose();
                _disposed = true;
            }
        }
    }
}
