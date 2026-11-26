using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Proyectoandtask.Application.Interfaces;
using proyectoandtask.Application.Interfaces.Repositories;
using Proyectoandtask.Infrastructure.Context;
using Proyectoandtask.Infrastructure.Identity;
using Proyectoandtask.Infrastructure.Repositories;
using Proyectoandtask.Infrastructure.Services;

namespace Proyectoandtask.Infrastructure;

/// <summary>
/// Métodos de extensión para registrar los servicios de Infrastructure en el contenedor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configura EF Core con PostgreSQL y registra el contexto, los repositorios y la unidad de trabajo.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="connectionString">Cadena de conexión de PostgreSQL.</param>
    /// <returns>La misma colección de servicios, para permitir encadenar registros.</returns>
    /// <exception cref="ArgumentNullException">La colección de servicios es nula.</exception>
    /// <exception cref="ArgumentException">La cadena de conexión está vacía o solo contiene espacios.</exception>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(jwtOptions);
        jwtOptions.Validate();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager();

        services.AddSingleton(jwtOptions);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

        services.AddScoped<IProyectoRepository, ProyectoRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, IdentityAuthService>();

        return services;
    }
}
