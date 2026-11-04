using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BeOnLookout.Api.Health;

public sealed class DatabaseHealthCheck(
    IConfiguration configuration,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("BankingDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Database readiness failed: BankingDatabase is not configured.");
            return HealthCheckResult.Unhealthy("The database is not configured.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = 10;
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                "Database readiness failed ({ExceptionType}).",
                exception.GetType().Name);
            return HealthCheckResult.Unhealthy("The database is unavailable.");
        }
    }
}
