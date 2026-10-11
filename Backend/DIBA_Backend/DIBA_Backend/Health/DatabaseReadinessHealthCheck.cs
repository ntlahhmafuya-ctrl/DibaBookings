using DIBA_Backend.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DIBA_Backend.Health
{
    /// <summary>
    /// Checks whether the API can reach its configured database.
    /// The response deliberately avoids returning connection details.
    /// </summary>
    public sealed class DatabaseReadinessHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseReadinessHealthCheck> _logger;

        public DatabaseReadinessHealthCheck(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseReadinessHealthCheck> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<DIBABookingsDbContext>();

                var canConnect = await dbContext.Database
                    .CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is available.")
                    : HealthCheckResult.Unhealthy("Database connection is unavailable.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Database readiness check failed.");
                return HealthCheckResult.Unhealthy(
                    "Database connection is unavailable.");
            }
        }
    }
}
