using Microsoft.EntityFrameworkCore;
using JUSTLockers.Classes;
using MySqlConnector;
using Microsoft.AspNetCore.Connections;

namespace JUSTLockers.DataBase
{
    public interface IDbConnectionFactory
    {
        public Task<MySqlConnection> CreateConnectionAsync(CancellationToken token = default);
    }
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _config;
        public DbConnectionFactory(string config)
        {
            _config = config;
        }
        public async Task<MySqlConnection> CreateConnectionAsync(CancellationToken token = default)
        {
            var connection = new MySqlConnection(_config);
            await connection.OpenAsync(token);
            return connection;
        }
    }
}