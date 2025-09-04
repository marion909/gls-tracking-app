using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GlsTrackingApp.Models;
using GlsTrackingApp.Config;

namespace GlsTrackingApp.Services
{
    public class SqliteDatabaseService
    {
        private readonly string _connectionString;

        public SqliteDatabaseService()
        {
            var config = AppConfig.Instance;
            var dbPath = config.DatabasePath;
            
            // Stelle sicher, dass der Ordner existiert
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS TrackingInfo (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TrackingNumber TEXT NOT NULL UNIQUE,
                    CustomerName TEXT,
                    CreatedDate TEXT,
                    LastStatus TEXT,
                    LastLocation TEXT,
                    LastUpdate TEXT
                )";
            createTableCommand.ExecuteNonQuery();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<StoredTrackingInfo>> GetAllTrackingInfoAsync()
        {
            var trackingInfoList = new List<StoredTrackingInfo>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM TrackingInfo";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var trackingNumberOrdinal = reader.GetOrdinal("TrackingNumber");
                var customerNameOrdinal = reader.GetOrdinal("CustomerName");
                var lastStatusOrdinal = reader.GetOrdinal("LastStatus");
                var lastLocationOrdinal = reader.GetOrdinal("LastLocation");
                var createdDateOrdinal = reader.GetOrdinal("CreatedDate");
                var lastUpdateOrdinal = reader.GetOrdinal("LastUpdate");
                
                var trackingInfo = new StoredTrackingInfo
                {
                    TrackingNumber = reader.GetString(trackingNumberOrdinal),
                    CustomerName = reader.IsDBNull(customerNameOrdinal) ? string.Empty : reader.GetString(customerNameOrdinal),
                    LastStatus = reader.IsDBNull(lastStatusOrdinal) ? string.Empty : reader.GetString(lastStatusOrdinal),
                    LastLocation = reader.IsDBNull(lastLocationOrdinal) ? string.Empty : reader.GetString(lastLocationOrdinal)
                };

                if (!reader.IsDBNull(createdDateOrdinal))
                {
                    if (DateTime.TryParse(reader.GetString(createdDateOrdinal), out var createdDate))
                        trackingInfo.CreatedDate = createdDate;
                }

                if (!reader.IsDBNull(lastUpdateOrdinal))
                {
                    if (DateTime.TryParse(reader.GetString(lastUpdateOrdinal), out var lastUpdate))
                        trackingInfo.LastUpdate = lastUpdate;
                }

                trackingInfoList.Add(trackingInfo);
            }

            return trackingInfoList;
        }

        public async Task SaveTrackingInfoAsync(StoredTrackingInfo trackingInfo)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO TrackingInfo 
                (TrackingNumber, CustomerName, CreatedDate, LastStatus, LastLocation, LastUpdate)
                VALUES (@TrackingNumber, @CustomerName, @CreatedDate, @LastStatus, @LastLocation, @LastUpdate)";

            command.Parameters.AddWithValue("@TrackingNumber", trackingInfo.TrackingNumber);
            command.Parameters.AddWithValue("@CustomerName", trackingInfo.CustomerName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", trackingInfo.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@LastStatus", trackingInfo.LastStatus ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastLocation", trackingInfo.LastLocation ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastUpdate", trackingInfo.LastUpdate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteTrackingInfoAsync(string trackingNumber)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TrackingInfo WHERE TrackingNumber = @TrackingNumber";
            command.Parameters.AddWithValue("@TrackingNumber", trackingNumber);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(string trackingNumber)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM TrackingInfo WHERE TrackingNumber = @TrackingNumber";
            command.Parameters.AddWithValue("@TrackingNumber", trackingNumber);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
    }
}
