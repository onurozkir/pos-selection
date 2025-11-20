using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PayTR.PosSelection.Infrastructure.Db;
using PayTR.PosSelection.Infrastructure.Interfaces.PosRatios;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;
using Polly;
using Polly.CircuitBreaker;

namespace PayTR.PosSelection.Infrastructure.Repositories
{
    public class PosRatios : NpgsqlConnectionFactory, IPosRatios
    {
        private readonly IAsyncPolicy _circuitBreaker;
        
        public PosRatios(IConfiguration configuration, IAsyncPolicy circuitBreaker): base(configuration)
        {
            _circuitBreaker = circuitBreaker;
        }

        public async Task<bool> InsertVersion(
            int version,
            string ratiosJson,
            DateTimeOffset posReceiveFinishDate,
            CancellationToken cancellationToken)
        {
            
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    using var transaction = connection.BeginTransaction();
                
                    try
                    {
                        const string sql = @"
INSERT INTO pos_ratios (version, pos_ratios, pos_receive_finish_date)
VALUES (@Version, CAST(@PosRatios AS JSONB), @PosReceiveFinishDate);
";

                        var parameters = new
                        {
                            Version = version,
                            PosRatios = ratiosJson,
                            PosReceiveFinishDate = posReceiveFinishDate
                        };

                        var result = await connection.ExecuteAsync(
                            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
             
                        transaction.Commit();
                
                        return result > 0;
                    }
                    catch (Exception e)
                    {
                        // tabloda version alanı unique index olduğundan exception önemsenmez.
                        return false;
                    } 
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (PostgresException e)
            {
                throw new CustomErrorException("We are currently experiencing a technical issue and are working to resolve the issue. Please try again later.", e);
            }
            
        }
        
        
      public async Task<string?> GetLastVersion( 
            CancellationToken cancellationToken)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection(); 
                
                    const string sql = @"SELECT pos_ratios FROM pos_ratios ORDER BY version DESC LIMIT 1;";
                        
                    var result = await connection.ExecuteScalarAsync<string>(
                        new CommandDefinition(sql, new {}, cancellationToken: cancellationToken));
                    
                    return result;
                     
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (PostgresException e)
            {
                throw new CustomErrorException("We are currently experiencing a technical issue and are working to resolve the issue. Please try again later.", e);
            }
        }
    }
}

