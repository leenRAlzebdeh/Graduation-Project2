using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JUSTLockers.Tests.IntegartionTest
{
    public class SemesterEndIntegrationTest : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly AdminService _adminService;
        private readonly MySqlConnection _connection;
        private MySqlTransaction? _transaction;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";

        public SemesterEndIntegrationTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                        {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _adminService = new AdminService(_configuration);

            _connection = new MySqlConnection(connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        #region Helper Methods
        public async Task<T?> GetRandomEntityAsync<T>(string tableName, Func<IDataReader, T> mapFunc, string? whereClause = null, object? parameters = null)
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = $"SELECT * FROM {tableName}" + (whereClause != null ? $" WHERE {whereClause}" : "") + " ORDER BY RAND() LIMIT 1";
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                }
            }
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapFunc(reader);
            }
            return default;
        }

        private async Task CleanupOrphanedReservations()
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = @"
                    DELETE FROM Reservations 
                    WHERE LockerId NOT IN (
                        SELECT COALESCE(locker_id, '') FROM Students WHERE locker_id IS NOT NULL
                    )";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> GetNextIdAsync(string tableName)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = $"SELECT IFNULL(MAX(Id), 0) + 1 FROM {tableName}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                }
            }
            return await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        [Fact]
        public async Task SemesterEndProcess_CancelsReservationsAndArchivesReports()
        {
            var result = await _adminService.SaveSemesterSettings(DateTime.UtcNow);
            var leen=await _adminService.ClearReservationsAndReports();
            Assert.True(leen);


        }

        public void Dispose()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
