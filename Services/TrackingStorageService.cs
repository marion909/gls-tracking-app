using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GlsTrackingApp.Models;
using GlsTrackingApp.Config;

namespace GlsTrackingApp.Services
{
    public class TrackingStorageService
    {
        private readonly SqliteDatabaseService _sqliteDb;

        public TrackingStorageService()
        {
            var config = AppConfig.Instance;
            
            // Nur SQLite verwenden
            _sqliteDb = new SqliteDatabaseService();
            
            // Stelle sicher, dass Config auf SQLite steht
            config.DatabaseType = "SQLite";
            if (config.DatabasePath.EndsWith(".mdb"))
            {
                config.DatabasePath = config.DatabasePath.Replace(".mdb", ".db");
            }
            config.SaveConfig();
        }

        public async Task<List<StoredTrackingInfo>> LoadStoredTrackingsAsync()
        {
            try
            {
                return await _sqliteDb.GetAllTrackingInfoAsync();
            }
            catch (Exception)
            {
                return new List<StoredTrackingInfo>();
            }
        }

        public async Task SaveStoredTrackingsAsync(List<StoredTrackingInfo> trackings)
        {
            try
            {
                // SQLite Service speichert einzeln, also iterieren
                foreach (var tracking in trackings)
                {
                    await _sqliteDb.SaveTrackingInfoAsync(tracking);
                }
            }
            catch (Exception)
            {
                // Fehler beim Speichern ignorieren - könnte in UI angezeigt werden
            }
        }

        public async Task SaveSingleTrackingAsync(StoredTrackingInfo tracking)
        {
            try
            {
                await _sqliteDb.SaveTrackingInfoAsync(tracking);
            }
            catch (Exception)
            {
                // Fehler beim Speichern ignorieren
            }
        }

        public async Task DeleteTrackingAsync(string trackingNumber)
        {
            try
            {
                await _sqliteDb.DeleteTrackingInfoAsync(trackingNumber);
            }
            catch (Exception)
            {
                // Fehler beim Löschen ignorieren
            }
        }

        public async Task UpdateTrackingStatusAsync(StoredTrackingInfo tracking, TrackingInfo newInfo)
        {
            tracking.LastStatus = newInfo.Status;
            tracking.LastLocation = newInfo.Location;
            tracking.LastUpdate = newInfo.LastUpdate;
            
            await SaveSingleTrackingAsync(tracking);
        }

        public async Task AddTrackingAsync(StoredTrackingInfo tracking)
        {
            await SaveSingleTrackingAsync(tracking);
        }

        public async Task SaveStoredTrackingAsync(StoredTrackingInfo tracking)
        {
            await SaveSingleTrackingAsync(tracking);
        }

        public async Task DeleteStoredTrackingAsync(string trackingNumber)
        {
            await DeleteTrackingAsync(trackingNumber);
        }

        public async Task RemoveTrackingAsync(string trackingNumber)
        {
            await DeleteTrackingAsync(trackingNumber);
        }
    }
}
