using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Proyectoandtask.Infrastructure.Context;

/// <summary>
/// Crea el contexto para las herramientas de diseño de Entity Framework Core,
/// como la creación y actualización de migraciones.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Construye el contexto usando la cadena de conexión definida en el entorno local.
    /// </summary>
    /// <param name="args">Argumentos recibidos por la herramienta de EF Core.</param>
    /// <returns>Un contexto configurado para PostgreSQL.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se produce si no se configuró la variable ConnectionStrings__DefaultConnection.
    /// </exception>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Define ConnectionStrings__DefaultConnection para ejecutar las herramientas de EF Core.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
