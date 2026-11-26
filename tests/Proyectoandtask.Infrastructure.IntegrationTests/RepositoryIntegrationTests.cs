using Microsoft.EntityFrameworkCore;
using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;
using Proyectoandtask.Infrastructure.Identity;
using Proyectoandtask.Infrastructure.Repositories;

namespace Proyectoandtask.Infrastructure.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class RepositoryIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public RepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProyectoRepository_PersisteYConsultaProyecto()
    {
        await using var context = _fixture.CreateContext();
        var repository = new ProyectoRepository(context);
        var proyecto = CreateProject();

        await repository.AddAsync(proyecto);
        await context.SaveChangesAsync();

        var result = await repository.GetProyectoAsync(proyecto.Id);

        Assert.NotNull(result);
        Assert.Equal(proyecto.Id, result.Id);
        Assert.Equal(proyecto.Name, result.Name);
    }

    [Fact]
    public async Task ProyectoRepository_ComparaNombresSinDistinguirMayusculas()
    {
        await using var context = _fixture.CreateContext();
        var repository = new ProyectoRepository(context);
        var proyecto = CreateProject();

        await repository.AddAsync(proyecto);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsByNameAsync(proyecto.Name.ToUpperInvariant());

        Assert.True(exists);
    }

    [Fact]
    public async Task TareaRepository_BuscaNombreParcialSinDistinguirMayusculas()
    {
        await using var context = _fixture.CreateContext();
        var proyecto = CreateProject();
        await context.Proyectos.AddAsync(proyecto);
        var tareaCoincidente = CreateTask(proyecto.Id, "Diseño de módulo");
        var tareaNoCoincidente = CreateTask(proyecto.Id, "Revisar documentación");
        var repository = new TareaRepository(context);

        await repository.AgregarAsync(tareaCoincidente);
        await repository.AgregarAsync(tareaNoCoincidente);
        await context.SaveChangesAsync();

        var result = await repository.ObtenerPorProyectoAsync(proyecto.Id, "DISEÑO");

        var tarea = Assert.Single(result);
        Assert.Equal(tareaCoincidente.Id, tarea.Id);
    }

    [Fact]
    public async Task TareaRepository_FiltraPorEstadoPrioridadVencimientoYCuentaAvance()
    {
        await using var context = _fixture.CreateContext();
        var proyecto = CreateProject();
        await context.Proyectos.AddAsync(proyecto);

        var vencidaPendiente = CreateTask(proyecto.Id, "Vencida pendiente");
        vencidaPendiente.EstadoTarea = EstadoTarea.pendiente;
        vencidaPendiente.PrioridadTarea = PrioridadTarea.Alta;
        vencidaPendiente.FechaLimite = DateTime.UtcNow.AddDays(-2);

        var vencidaCompletada = CreateTask(proyecto.Id, "Vencida completada");
        vencidaCompletada.EstadoTarea = EstadoTarea.completada;
        vencidaCompletada.PrioridadTarea = PrioridadTarea.Alta;
        vencidaCompletada.FechaLimite = DateTime.UtcNow.AddDays(-1);

        var tareaFutura = CreateTask(proyecto.Id, "Tarea futura");
        tareaFutura.EstadoTarea = EstadoTarea.progreso;
        tareaFutura.PrioridadTarea = PrioridadTarea.Media;
        tareaFutura.FechaLimite = DateTime.UtcNow.AddDays(2);

        var repository = new TareaRepository(context);
        await repository.AgregarAsync(vencidaPendiente);
        await repository.AgregarAsync(vencidaCompletada);
        await repository.AgregarAsync(tareaFutura);
        await context.SaveChangesAsync();

        var pendientes = await repository.ObtenerPorEstadoAsync(proyecto.Id, EstadoTarea.pendiente);
        var altaPrioridad = await repository.ObtenerPorPrioridadAsync(proyecto.Id, PrioridadTarea.Alta);
        var vencidas = await repository.ObtenerVencidasNoCompletadasAsync(proyecto.Id, DateTime.UtcNow);
        var conteos = await repository.ObtenerConteosPorProyectoAsync(proyecto.Id);
        var tienePendientes = await repository.TieneTareasPendientesPorProyectoAsync(proyecto.Id);

        Assert.Equal(vencidaPendiente.Id, Assert.Single(pendientes).Id);
        Assert.Equal(2, altaPrioridad.Count);
        Assert.Equal(vencidaPendiente.Id, Assert.Single(vencidas).Id);
        Assert.Equal((3, 1), conteos);
        Assert.True(tienePendientes);
    }

    [Fact]
    public async Task ApplicationDbContext_ImpideCorreosNormalizadosDuplicados()
    {
        await using var context = _fixture.CreateContext();
        var emailNormalizado = $"{Guid.NewGuid():N}@TEST.COM";
        var firstUser = CreateUser(emailNormalizado, "user-one");
        var secondUser = CreateUser(emailNormalizado, "user-two");

        context.Users.AddRange(firstUser, secondUser);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Proyecto CreateProject()
    {
        var now = DateTime.UtcNow;
        return new Proyecto
        {
            Name = $"Project-{Guid.NewGuid():N}",
            FechaCreacion = now,
            FechaInicio = now,
            FechaFinalizacion = now.AddDays(30)
        };
    }

    private static Tarea CreateTask(Guid proyectoId, string name)
    {
        return new Tarea
        {
            Name = name,
            ProyectoId = proyectoId
        };
    }

    private static ApplicationUser CreateUser(string normalizedEmail, string suffix)
    {
        return new ApplicationUser
        {
            UserName = $"{suffix}-{Guid.NewGuid():N}",
            NormalizedUserName = $"{suffix}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Email = normalizedEmail.ToLowerInvariant(),
            NormalizedEmail = normalizedEmail
        };
    }
}
