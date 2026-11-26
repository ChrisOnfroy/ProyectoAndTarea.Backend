using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using proyectandtask.Domain.Exceptions;
using Proyectandtask.Api;
using Proyectoandtask.Application;
using Proyectoandtask.Infrastructure;
using Proyectoandtask.Infrastructure.Identity;

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
builder.Services.AddInfrastructureServices(connectionString, jwtOptions);
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
    var isClientError = exception is DomainException or ArgumentException;
    var statusCode = isClientError
        ? StatusCodes.Status400BadRequest
        : StatusCodes.Status500InternalServerError;

    if (!isClientError && exception is not null)
    {
        app.Logger.LogError(exception, "Error no controlado al procesar la solicitud.");
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = statusCode,
        Title = isClientError ? "Solicitud inválida" : "Error interno del servidor",
        Detail = isClientError
            ? exception?.Message
            : "Ocurrió un error inesperado al procesar la solicitud."
    }, JsonSerializerOptions.Web, "application/problem+json");
}));

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();

public partial class Program;
