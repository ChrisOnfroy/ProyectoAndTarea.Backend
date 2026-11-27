using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Proyectoandtask.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using proyectoandtask.Application.Dtos;
using proyectandtask.Domain.Enums;
using proyectandtask.Domain.Entities;

namespace Proyectandtask.Api.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ApiEndpointTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiPostgreSqlFixture _fixture;

    public ApiEndpointTests(ApiPostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Proyectos_SinAutenticacion_DevuelveUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/proyectos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenAutenticadoSinIdDeUsuario_DevuelveUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Integration_Test_Jwt_Key_At_Least_32_Bytes_123!"));
        var token = new JwtSecurityToken(
            issuer: "Proyectandtask.Api.IntegrationTests",
            audience: "Proyectandtask.Api.IntegrationTests.Client",
            claims: [new Claim(ClaimTypes.Name, "sin-id")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));

        var response = await client.GetAsync("/api/v1/proyectos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Proyecto_SeCreaSeConsultaYExponeSuAvance()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var fechaInicio = DateTime.UtcNow.Date;
        var request = new
        {
            name = $"Proyecto API {Guid.NewGuid():N}",
            description = "Proyecto creado desde una prueba HTTP.",
            fechaInicio,
            fechaFinalizacion = fechaInicio.AddDays(10)
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/proyectos", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var proyecto = await createResponse.Content.ReadFromJsonAsync<ProyectoResponseDto>();
        Assert.NotNull(proyecto);

        var getResponse = await client.GetAsync($"/api/v1/proyectos/{proyecto.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var progressResponse = await client.GetAsync($"/api/v1/proyectos/{proyecto.Id}/avance");
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        Assert.Equal(0m, await progressResponse.Content.ReadFromJsonAsync<decimal>());
    }

    [Fact]
    public async Task Tarea_SeCreaSeFiltraYSeCambiaSuEstado()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var fechaInicio = DateTime.UtcNow.Date;
        var projectResponse = await client.PostAsJsonAsync("/api/v1/proyectos", new
        {
            name = $"Proyecto para tareas {Guid.NewGuid():N}",
            fechaInicio,
            fechaFinalizacion = fechaInicio.AddDays(10)
        });
        projectResponse.EnsureSuccessStatusCode();
        var proyecto = await projectResponse.Content.ReadFromJsonAsync<ProyectoResponseDto>();
        Assert.NotNull(proyecto);

        var createTaskResponse = await client.PostAsJsonAsync("/api/v1/tareas", new
        {
            name = "Tarea de integración",
            proyectoId = proyecto.Id,
            estadoTarea = "pendiente",
            prioridadTarea = "Alta"
        });

        Assert.Equal(HttpStatusCode.Created, createTaskResponse.StatusCode);
        var tarea = await createTaskResponse.Content.ReadFromJsonAsync<TareaResponseDto>(ResponseJsonOptions);
        Assert.NotNull(tarea);

        var stateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/tareas/{tarea.Id}/estado",
            new { estado = "completada" });
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);

        var filteredResponse = await client.GetAsync(
            $"/api/v1/tareas/proyecto/{proyecto.Id}/estado?estado=completada");
        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        var tareas = await filteredResponse.Content.ReadFromJsonAsync<List<TareaResponseDto>>(ResponseJsonOptions);
        Assert.NotNull(tareas);
        Assert.Contains(tareas, item => item.Id == tarea.Id);
    }

    [Fact]
    public async Task Proyecto_ConFechasInvalidas_DevuelveProblemDetails400()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var fechaInicio = DateTime.UtcNow.Date;
        var response = await client.PostAsJsonAsync("/api/v1/proyectos", new
        {
            name = $"Proyecto inválido {Guid.NewGuid():N}",
            fechaInicio,
            fechaFinalizacion = fechaInicio.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Usuarios_SoloAccedenASusProyectosYTareas_YLasOperacionesAjenaNoAlteranDatos()
    {
        using var clientA = await CreateAuthenticatedClientAsync();
        using var clientB = await CreateAuthenticatedClientAsync();
        var projectA = await CreateProjectAsync(clientA, $"Privado-{Guid.NewGuid():N}");
        var taskA = await CreateTaskAsync(clientA, projectA.Id, "Tarea privada A");
        var projectB = await CreateProjectAsync(clientB, $"Privado-B-{Guid.NewGuid():N}");
        var taskB = await CreateTaskAsync(clientB, projectB.Id, "Tarea privada B");

        var projectsForB = await clientB.GetFromJsonAsync<List<ProyectoResponseDto>>("/api/v1/proyectos");
        Assert.NotNull(projectsForB);
        Assert.Contains(projectsForB, project => project.Id == projectB.Id);
        Assert.DoesNotContain(projectsForB, project => project.Id == projectA.Id);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/proyectos/{projectA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/proyectos/{projectA.Id}/avance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/v1/proyectos/{projectA.Id}", ProjectRequest("Changed", projectA))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/v1/proyectos/{projectA.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/tareas/{taskA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/v1/tareas/{taskA.Id}", TaskRequest("Changed", projectB.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PatchAsJsonAsync($"/api/v1/tareas/{taskA.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/v1/tareas/{taskA.Id}")).StatusCode);

        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/v1/tareas/proyecto/{projectA.Id}"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/v1/tareas/proyecto/{projectA.Id}/estado?estado=pendiente"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/v1/tareas/proyecto/{projectA.Id}/prioridad?prioridad=Alta"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/v1/tareas/proyecto/{projectA.Id}/vencidas"));
        using var createInForeignProject = await clientB.PostAsJsonAsync("/api/v1/tareas", TaskRequest("Tarea invasora", projectA.Id));
        using var moveToForeignProject = await clientB.PutAsJsonAsync($"/api/v1/tareas/{taskB.Id}", TaskRequest("Tarea movida", projectA.Id));
        Assert.Equal(HttpStatusCode.BadRequest, createInForeignProject.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, moveToForeignProject.StatusCode);
        Assert.Contains("El proyecto indicado no existe.", await createInForeignProject.Content.ReadAsStringAsync());
        Assert.Contains("El proyecto indicado no existe.", await moveToForeignProject.Content.ReadAsStringAsync());

        var projectAAfter = await clientA.GetFromJsonAsync<ProyectoResponseDto>($"/api/v1/proyectos/{projectA.Id}");
        var taskAAfter = await clientA.GetFromJsonAsync<TareaResponseDto>($"/api/v1/tareas/{taskA.Id}", ResponseJsonOptions);
        Assert.NotNull(projectAAfter);
        Assert.Equal(projectA.Name, projectAAfter.Name);
        Assert.NotNull(taskAAfter);
        Assert.Equal("Tarea privada A", taskAAfter.Name);
        Assert.Equal(EstadoTarea.pendiente, taskAAfter.EstadoTarea);

        // El usuario A conserva las operaciones sobre sus propios recursos.
        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/v1/proyectos/{projectA.Id}", ProjectRequest("Proyecto A actualizado", projectA))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.PatchAsJsonAsync($"/api/v1/tareas/{taskA.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/v1/tareas/{taskA.Id}", TaskRequest("Tarea A actualizada", projectA.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clientA.DeleteAsync($"/api/v1/tareas/{taskA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clientA.DeleteAsync($"/api/v1/proyectos/{projectA.Id}")).StatusCode);
    }

    [Fact]
    public async Task NombreDeProyectoEsUnicoPorUsuario()
    {
        using var clientA = await CreateAuthenticatedClientAsync();
        using var clientB = await CreateAuthenticatedClientAsync();
        var name = $"Nombre compartido {Guid.NewGuid():N}";

        Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsJsonAsync("/api/v1/proyectos", ProjectRequest(name))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await clientA.PostAsJsonAsync("/api/v1/proyectos", ProjectRequest(name.ToUpperInvariant()))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsJsonAsync("/api/v1/proyectos", ProjectRequest(name.ToLowerInvariant()))).StatusCode);
    }

    [Fact]
    public async Task EliminarProyecto_SoloPermiteVacioOTodasCompletadas()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var emptyProject = await CreateProjectAsync(client, $"Vacío-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/proyectos/{emptyProject.Id}")).StatusCode);

        var completedProject = await CreateProjectAsync(client, $"Completado-{Guid.NewGuid():N}");
        var completedTask = await CreateTaskAsync(client, completedProject.Id, "Completada");
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/v1/tareas/{completedTask.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/proyectos/{completedProject.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tareas/{completedTask.Id}")).StatusCode);

        foreach (var states in new[]
                 {
                     new[] { "pendiente" },
                     new[] { "progreso" },
                     new[] { "completada", "progreso" }
                 })
        {
            var blockedProject = await CreateProjectAsync(client, $"Bloqueado-{Guid.NewGuid():N}");
            var tasks = new List<TareaResponseDto>();
            foreach (var state in states)
            {
                var task = await CreateTaskAsync(client, blockedProject.Id, $"{state}-{Guid.NewGuid():N}");
                if (state != "pendiente")
                    Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/v1/tareas/{task.Id}/estado", new { estado = state })).StatusCode);
                tasks.Add(task);
            }

            var blocked = await client.DeleteAsync($"/api/v1/proyectos/{blockedProject.Id}");
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
            Assert.Contains("No se puede eliminar un proyecto que tiene tareas sin completar.", await blocked.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/proyectos/{blockedProject.Id}")).StatusCode);
            foreach (var task in tasks)
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        }

        var projectWithCompleted = await CreateProjectAsync(client, $"Solo completadas-{Guid.NewGuid():N}");
        var completed = await CreateTaskAsync(client, projectWithCompleted.Id, "Completada local");
        await client.PatchAsJsonAsync($"/api/v1/tareas/{completed.Id}/estado", new { estado = "completada" });
        var otherProject = await CreateProjectAsync(client, $"Otro proyecto-{Guid.NewGuid():N}");
        var inProgressElsewhere = await CreateTaskAsync(client, otherProject.Id, "En progreso ajena");
        await client.PatchAsJsonAsync($"/api/v1/tareas/{inProgressElsewhere.Id}/estado", new { estado = "progreso" });
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/proyectos/{projectWithCompleted.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tareas/{completed.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.DeleteAsync($"/api/v1/proyectos/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Admin_PuedeLeerTareasDeOtrosUsuariosPeroNoModificarlas()
    {
        using var ownerClient = await CreateAuthenticatedClientAsync();
        var project = await CreateProjectAsync(ownerClient, $"Proyecto usuario {Guid.NewGuid():N}");
        var task = await CreateTaskAsync(ownerClient, project.Id, "Tarea privada");

        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        var adminName = $"admin-{Guid.NewGuid():N}";
        using var registrationClient = _fixture.Factory.CreateClient();
        var registration = await registrationClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            userName = adminName,
            email = adminEmail,
            password = "Integration_Test!Pass123"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roleManager.RoleExistsAsync(ApplicationRoles.Admin))
                Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Admin))).Succeeded);

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(adminUser);
            Assert.True((await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin)).Succeeded);
        }

        var loginClient = _fixture.Factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = adminEmail,
            password = "Integration_Test!Pass123"
        });
        var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", loginBody.RootElement.GetProperty("accessToken").GetString());

        Assert.Equal(HttpStatusCode.OK, (await loginClient.GetAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await loginClient.GetAsync($"/api/v1/tareas/proyecto/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await loginClient.PatchAsJsonAsync($"/api/v1/tareas/{task.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await loginClient.DeleteAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        loginBody.Dispose();
        loginClient.Dispose();
    }

    [Fact]
    public async Task ProyectoSinDueño_NoEsVisibleParaNingunUsuario()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var project = new Proyecto
        {
            Name = $"Proyecto legado {Guid.NewGuid():N}",
            FechaCreacion = DateTime.UtcNow,
            FechaInicio = DateTime.UtcNow,
            FechaFinalizacion = DateTime.UtcNow.AddDays(1)
        };
        var task = new Tarea { Name = "Tarea legado", ProyectoId = project.Id };
        await using (var context = _fixture.CreateContext())
        {
            context.Proyectos.Add(project);
            context.Tareas.Add(task);
            await context.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/proyectos/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/proyectos/{project.Id}", ProjectRequest("Intento", new ProyectoResponseDto
        {
            FechaInicio = project.FechaInicio,
            FechaFinalizacion = project.FechaFinalizacion
        }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/proyectos/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/proyectos/{project.Id}/avance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/tareas/{task.Id}", TaskRequest("Intento", project.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/tareas/{task.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/tareas/{task.Id}")).StatusCode);
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/v1/tareas/proyecto/{project.Id}"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/v1/tareas/proyecto/{project.Id}/estado?estado=pendiente"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/v1/tareas/proyecto/{project.Id}/prioridad?prioridad=Alta"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/v1/tareas/proyecto/{project.Id}/vencidas"));
        var visibleProjects = await client.GetFromJsonAsync<List<ProyectoResponseDto>>("/api/v1/proyectos");
        Assert.NotNull(visibleProjects);
        Assert.DoesNotContain(visibleProjects, item => item.Id == project.Id);
    }

    private static void AssertProjectTaskListNotFound(HttpResponseMessage response) =>
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    private static object ProjectRequest(string name, ProyectoResponseDto? project = null)
    {
        var start = project?.FechaInicio ?? DateTime.UtcNow.Date;
        return new
        {
            name,
            description = project?.Description,
            fechaInicio = start,
            fechaFinalizacion = project?.FechaFinalizacion ?? start.AddDays(10)
        };
    }

    private static object TaskRequest(string name, Guid projectId) => new
    {
        name,
        proyectoId = projectId,
        estadoTarea = "pendiente",
        prioridadTarea = "Alta"
    };

    private static async Task<ProyectoResponseDto> CreateProjectAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/proyectos", ProjectRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProyectoResponseDto>())!;
    }

    private static async Task<TareaResponseDto> CreateTaskAsync(HttpClient client, Guid projectId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/tareas", TaskRequest(name, projectId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TareaResponseDto>(ResponseJsonOptions))!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _fixture.Factory.CreateClient();
        var registrationResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            userName = $"tester-{Guid.NewGuid():N}",
            email = $"{Guid.NewGuid():N}@example.com",
            password = "Integration_Test!Pass123"
        });
        registrationResponse.EnsureSuccessStatusCode();

        var registrationJson = await registrationResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(registrationJson);
        var accessToken = document.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
