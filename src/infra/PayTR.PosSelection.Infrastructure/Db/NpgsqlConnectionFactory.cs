using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PayTR.PosSelection.Infrastructure.Db
{
    public abstract class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string? _connectionString;

        protected NpgsqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres")            
                                        ?? configuration["Postgres:ConnectionString"];  
        }


        public virtual IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}

