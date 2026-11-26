using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using proyectoandtask.Application.Dtos;
using proyectandtask.Domain.Enums;
using proyectandtask.Domain.Entities;

namespace Proyectandtask.Api.Tests;

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

        var response = await client.GetAsync("/api/proyectos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenAutenticadoSinIdDeUsuario_DevuelveUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Integration_Test_Jwt_Key_At_Least_32_Bytes_123!"));
        var token = new JwtSecurityToken(
            issuer: "Proyectandtask.Api.Tests",
            audience: "Proyectandtask.Api.Tests.Client",
            claims: [new Claim(ClaimTypes.Name, "sin-id")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));

        var response = await client.GetAsync("/api/proyectos");

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

        var createResponse = await client.PostAsJsonAsync("/api/proyectos", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var proyecto = await createResponse.Content.ReadFromJsonAsync<ProyectoResponseDto>();
        Assert.NotNull(proyecto);

        var getResponse = await client.GetAsync($"/api/proyectos/{proyecto.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var progressResponse = await client.GetAsync($"/api/proyectos/{proyecto.Id}/avance");
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        Assert.Equal(0m, await progressResponse.Content.ReadFromJsonAsync<decimal>());
    }

    [Fact]
    public async Task Tarea_SeCreaSeFiltraYSeCambiaSuEstado()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var fechaInicio = DateTime.UtcNow.Date;
        var projectResponse = await client.PostAsJsonAsync("/api/proyectos", new
        {
            name = $"Proyecto para tareas {Guid.NewGuid():N}",
            fechaInicio,
            fechaFinalizacion = fechaInicio.AddDays(10)
        });
        projectResponse.EnsureSuccessStatusCode();
        var proyecto = await projectResponse.Content.ReadFromJsonAsync<ProyectoResponseDto>();
        Assert.NotNull(proyecto);

        var createTaskResponse = await client.PostAsJsonAsync("/api/tareas", new
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
            $"/api/tareas/{tarea.Id}/estado",
            new { estado = "completada" });
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);

        var filteredResponse = await client.GetAsync(
            $"/api/tareas/proyecto/{proyecto.Id}/estado?estado=completada");
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
        var response = await client.PostAsJsonAsync("/api/proyectos", new
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

        var projectsForB = await clientB.GetFromJsonAsync<List<ProyectoResponseDto>>("/api/proyectos");
        Assert.NotNull(projectsForB);
        Assert.Contains(projectsForB, project => project.Id == projectB.Id);
        Assert.DoesNotContain(projectsForB, project => project.Id == projectA.Id);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/proyectos/{projectA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/proyectos/{projectA.Id}/avance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/proyectos/{projectA.Id}", ProjectRequest("Changed", projectA))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/proyectos/{projectA.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/tareas/{taskA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/tareas/{taskA.Id}", TaskRequest("Changed", projectB.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PatchAsJsonAsync($"/api/tareas/{taskA.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/tareas/{taskA.Id}")).StatusCode);

        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/tareas/proyecto/{projectA.Id}"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/tareas/proyecto/{projectA.Id}/estado?estado=pendiente"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/tareas/proyecto/{projectA.Id}/prioridad?prioridad=Alta"));
        AssertProjectTaskListNotFound(await clientB.GetAsync($"/api/tareas/proyecto/{projectA.Id}/vencidas"));
        using var createInForeignProject = await clientB.PostAsJsonAsync("/api/tareas", TaskRequest("Tarea invasora", projectA.Id));
        using var moveToForeignProject = await clientB.PutAsJsonAsync($"/api/tareas/{taskB.Id}", TaskRequest("Tarea movida", projectA.Id));
        Assert.Equal(HttpStatusCode.BadRequest, createInForeignProject.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, moveToForeignProject.StatusCode);
        Assert.Contains("El proyecto indicado no existe.", await createInForeignProject.Content.ReadAsStringAsync());
        Assert.Contains("El proyecto indicado no existe.", await moveToForeignProject.Content.ReadAsStringAsync());

        var projectAAfter = await clientA.GetFromJsonAsync<ProyectoResponseDto>($"/api/proyectos/{projectA.Id}");
        var taskAAfter = await clientA.GetFromJsonAsync<TareaResponseDto>($"/api/tareas/{taskA.Id}", ResponseJsonOptions);
        Assert.NotNull(projectAAfter);
        Assert.Equal(projectA.Name, projectAAfter.Name);
        Assert.NotNull(taskAAfter);
        Assert.Equal("Tarea privada A", taskAAfter.Name);
        Assert.Equal(EstadoTarea.pendiente, taskAAfter.EstadoTarea);

        // El usuario A conserva las operaciones sobre sus propios recursos.
        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/proyectos/{projectA.Id}", ProjectRequest("Proyecto A actualizado", projectA))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.PatchAsJsonAsync($"/api/tareas/{taskA.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/tareas/{taskA.Id}", TaskRequest("Tarea A actualizada", projectA.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clientA.DeleteAsync($"/api/tareas/{taskA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clientA.DeleteAsync($"/api/proyectos/{projectA.Id}")).StatusCode);
    }

    [Fact]
    public async Task NombreDeProyectoEsUnicoPorUsuario()
    {
        using var clientA = await CreateAuthenticatedClientAsync();
        using var clientB = await CreateAuthenticatedClientAsync();
        var name = $"Nombre compartido {Guid.NewGuid():N}";

        Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsJsonAsync("/api/proyectos", ProjectRequest(name))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await clientA.PostAsJsonAsync("/api/proyectos", ProjectRequest(name.ToUpperInvariant()))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsJsonAsync("/api/proyectos", ProjectRequest(name.ToLowerInvariant()))).StatusCode);
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

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/proyectos/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/proyectos/{project.Id}", ProjectRequest("Intento", new ProyectoResponseDto
        {
            FechaInicio = project.FechaInicio,
            FechaFinalizacion = project.FechaFinalizacion
        }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/proyectos/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/proyectos/{project.Id}/avance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tareas/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/tareas/{task.Id}", TaskRequest("Intento", project.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/tareas/{task.Id}/estado", new { estado = "completada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/tareas/{task.Id}")).StatusCode);
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/tareas/proyecto/{project.Id}"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/tareas/proyecto/{project.Id}/estado?estado=pendiente"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/tareas/proyecto/{project.Id}/prioridad?prioridad=Alta"));
        AssertProjectTaskListNotFound(await client.GetAsync($"/api/tareas/proyecto/{project.Id}/vencidas"));
        var visibleProjects = await client.GetFromJsonAsync<List<ProyectoResponseDto>>("/api/proyectos");
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
        var response = await client.PostAsJsonAsync("/api/proyectos", ProjectRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProyectoResponseDto>())!;
    }

    private static async Task<TareaResponseDto> CreateTaskAsync(HttpClient client, Guid projectId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/tareas", TaskRequest(name, projectId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TareaResponseDto>(ResponseJsonOptions))!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _fixture.Factory.CreateClient();
        var registrationResponse = await client.PostAsJsonAsync("/api/auth/register", new
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
