using Microsoft.Extensions.Diagnostics.HealthChecks;
using Proyectoandtask.Infrastructure.Context;

namespace Proyectandtask.Api;

public sealed class PostgreSqlHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL responde.")
                : HealthCheckResult.Unhealthy("No se pudo conectar con PostgreSQL.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar con PostgreSQL.", exception);
        }
    }
}
