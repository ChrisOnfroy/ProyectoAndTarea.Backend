using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;
using Proyectoandtask.Infrastructure.Context;
using Proyectoandtask.Infrastructure.Identity;

namespace Proyectoandtask.Infrastructure.Services;

public sealed class DemoDataSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    private const string TestPassword = "Pruebas2026!";

    private static readonly PersonSeed[] People =
    [
        new("Maria Fernanda Ruiz", "maria.ruiz@example.com", ApplicationRoles.Admin),
        new("Carlos Mendoza", "carlos.mendoza@example.com", ApplicationRoles.User),
        new("Ana Torres", "ana.torres@example.com", ApplicationRoles.User),
        new("Diego Ramirez", "diego.ramirez@example.com", ApplicationRoles.User),
        new("Valentina Castro", "valentina.castro@example.com", ApplicationRoles.User),
        new("Andres Herrera", "andres.herrera@example.com", ApplicationRoles.User),
        new("Sofia Morales", "sofia.morales@example.com", ApplicationRoles.User),
        new("Juan Camilo Ortiz", "juan.ortiz@example.com", ApplicationRoles.User)
    ];

    private static readonly string[] ProjectNames =
    [
        "Portal de atención a clientes",
        "Modernización de la plataforma de ventas",
        "Automatización de procesos internos",
        "Aplicación móvil para operaciones",
        "Migración de datos comerciales",
        "Panel de indicadores de gestión",
        "Sistema de gestión de inventario",
        "Renovación de la tienda en línea",
        "Integración con proveedores",
        "Mejora del proceso de incorporación",
        "Plataforma de capacitación interna",
        "Optimización de reportes financieros",
        "Digitalización de contratos",
        "Actualización del centro de soporte",
        "Gestión de campañas de mercadeo",
        "Control de calidad de productos",
        "Agenda de visitas comerciales",
        "Unificación de perfiles de clientes",
        "Tablero de seguimiento de entregas",
        "Portal de autoservicio para empleados",
        "Respaldo y recuperación de información",
        "Análisis de experiencia de usuario",
        "Gestión de solicitudes de compra",
        "Integración de canales de comunicación",
        "Actualización del sistema de facturación",
        "Catálogo digital de productos",
        "Seguimiento de oportunidades comerciales",
        "Organización de documentos corporativos",
        "Mejora de tiempos de respuesta",
        "Plan de continuidad operativa",
        "Aplicación de seguimiento de proyectos",
        "Consolidación de datos de operación"
    ];

    private static readonly string[] TaskNames =
    [
        "Reunir requisitos con las áreas involucradas",
        "Documentar el flujo de trabajo actual",
        "Preparar propuesta de solución",
        "Definir alcance y entregables",
        "Diseñar la experiencia de usuario",
        "Implementar los cambios principales",
        "Revisar la integración con los sistemas existentes",
        "Validar resultados con el equipo",
        "Corregir hallazgos de las pruebas",
        "Preparar la entrega y documentación"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(ApplicationRoles.User);
        await EnsureRoleAsync(ApplicationRoles.Admin);

        var owners = new List<(ApplicationUser User, string FullName)>();
        foreach (var person in People)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await EnsureUserAsync(person);
            owners.Add((user, person.FullName));
        }

        for (var projectIndex = 0; projectIndex < ProjectNames.Length; projectIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var owner = owners[projectIndex % owners.Count];
            await EnsureProjectAsync(owner.User, ProjectNames[projectIndex], projectIndex, cancellationToken);
        }
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
            throw new InvalidOperationException($"No se pudo crear el rol '{roleName}': {FormatErrors(result)}");
    }

    private async Task<ApplicationUser> EnsureUserAsync(PersonSeed person)
    {
        var user = await userManager.FindByEmailAsync(person.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = person.FullName.Replace(' ', '.'),
                Email = person.Email,
                LockoutEnabled = true
            };
            var createResult = await userManager.CreateAsync(user, TestPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"No se pudo crear la cuenta '{person.Email}': {FormatErrors(createResult)}");
        }

        if (!await userManager.IsInRoleAsync(user, person.Role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, person.Role);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"No se pudo asignar el rol '{person.Role}' a '{person.Email}': {FormatErrors(roleResult)}");
        }

        return user;
    }

    private async Task EnsureProjectAsync(
        ApplicationUser owner,
        string name,
        int projectIndex,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Proyectos.FirstOrDefaultAsync(
            candidate => candidate.OwnerId == owner.Id && candidate.Name == name,
            cancellationToken);
        if (project is null)
        {
            var start = DateTime.UtcNow.Date.AddDays(-projectIndex % 20);
            project = new Proyecto
            {
                OwnerId = owner.Id,
                Name = name,
                Description = $"Planificación y ejecución de {name.ToLowerInvariant()}.",
                FechaCreacion = DateTime.UtcNow,
                FechaInicio = start,
                FechaFinalizacion = start.AddDays(45 + projectIndex % 45)
            };
            dbContext.Proyectos.Add(project);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var taskCount = await dbContext.Tareas.CountAsync(task => task.ProyectoId == project.Id, cancellationToken);
        var desiredTaskCount = 6 + projectIndex % 5;
        for (var taskNumber = taskCount; taskNumber < desiredTaskCount; taskNumber++)
        {
            var state = (taskNumber % 6) switch
            {
                0 or 1 => EstadoTarea.completada,
                2 or 3 => EstadoTarea.progreso,
                _ => EstadoTarea.pendiente
            };

            dbContext.Tareas.Add(new Tarea
            {
                ProyectoId = project.Id,
                Name = TaskNames[taskNumber],
                Description = $"Coordinar y completar la actividad: {TaskNames[taskNumber].ToLowerInvariant()}.",
                FechaLimite = DateTime.UtcNow.Date.AddDays((taskNumber - 3) * 4 + projectIndex % 10),
                EstadoTarea = state,
                PrioridadTarea = (PrioridadTarea)((taskNumber + projectIndex) % 3)
            });
        }

        if (taskCount < desiredTaskCount)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));

    private sealed record PersonSeed(string FullName, string Email, string Role);
}
