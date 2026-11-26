using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using proyectandtask.Domain.Exceptions;
using Proyectandtask.Api;
using Proyectoandtask.Application;
using Proyectoandtask.Infrastructure;
using Proyectoandtask.Infrastructure.Identity;
using Proyectoandtask.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configura ConnectionStrings:DefaultConnection mediante User Secrets o una variable de entorno.");

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configura la sección Jwt de la API.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddInfrastructureServices(connectionString, jwtOptions);
builder.Services.AddHealthChecks()
    .AddCheck<PostgreSqlHealthCheck>("postgresql");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Proyectandtask API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Usa el token obtenido en /api/auth/login con el esquema Bearer."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var isUnauthorized = exception is UnauthorizedAccessException;
    var isClientError = exception is DomainException or ArgumentException;
    var statusCode = isUnauthorized
        ? StatusCodes.Status401Unauthorized
        : isClientError
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

    if (!isClientError && !isUnauthorized && exception is not null)
    {
        app.Logger.LogError(exception, "Error no controlado al procesar la solicitud.");
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = statusCode,
        Title = isUnauthorized
            ? "No autorizado"
            : isClientError ? "Solicitud inválida" : "Error interno del servidor",
        Detail = isUnauthorized
            ? "El token no contiene un identificador de usuario válido."
            : isClientError ? exception?.Message : "Ocurrió un error inesperado al procesar la solicitud."
    }, JsonSerializerOptions.Web, "application/problem+json");
}));

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
        });
    }
});
app.MapControllers();

app.Run();

public partial class Program;
