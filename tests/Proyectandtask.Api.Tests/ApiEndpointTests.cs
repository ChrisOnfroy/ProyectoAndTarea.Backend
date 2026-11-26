using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using proyectoandtask.Application.Dtos;
using proyectandtask.Domain.Enums;

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
