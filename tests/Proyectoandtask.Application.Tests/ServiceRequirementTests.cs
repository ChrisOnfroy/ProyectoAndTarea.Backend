using proyectoandtask.Application.Dtos;
using proyectoandtask.Application.Interfaces.Repositories;
using Proyectoandtask.Application.Interfaces;
using Proyectoandtask.Application.Services;
using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;
using proyectandtask.Domain.Exceptions;

namespace Proyectoandtask.Application.Tests;

public sealed class ServiceRequirementTests
{
    [Fact]
    public async Task CrearProyectoAsync_NormalizaNombreYGuardaFechas()
    {
        var proyectos = new FakeProyectoRepository();
        var tareas = new FakeTareaRepository();
        var unidadDeTrabajo = new FakeUnitOfWork(proyectos, tareas);
        var servicio = new ProyectoService(proyectos, tareas, unidadDeTrabajo, new FakeCurrentUser());
        var fechaInicio = DateTime.UtcNow.Date;
        var request = new ProyectoRequestDto
        {
            Name = "  Proyecto de prueba  ",
            FechaInicio = fechaInicio,
            FechaFinalizacion = fechaInicio.AddDays(5)
        };

        var resultado = await servicio.CrearAsync(request);

        Assert.Equal("Proyecto de prueba", resultado.Name);
        Assert.Equal(fechaInicio, resultado.FechaInicio);
        Assert.Equal(fechaInicio.AddDays(5), resultado.FechaFinalizacion);
        Assert.NotNull(proyectos.AddedProyecto);
        Assert.Equal(FakeCurrentUser.TestUserId, proyectos.AddedProyecto.OwnerId);
        Assert.Equal(1, unidadDeTrabajo.SaveChangesCount);
    }

    [Fact]
    public async Task CrearProyectoAsync_RechazaNombreDuplicado()
    {
        var proyectos = new FakeProyectoRepository { NombreExiste = true };
        var tareas = new FakeTareaRepository();
        var servicio = new ProyectoService(proyectos, tareas, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());
        var fechaInicio = DateTime.UtcNow.Date;
        var request = new ProyectoRequestDto
        {
            Name = "Proyecto existente",
            FechaInicio = fechaInicio,
            FechaFinalizacion = fechaInicio.AddDays(1)
        };

        await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(request));

        Assert.Null(proyectos.AddedProyecto);
    }

    [Fact]
    public async Task CrearProyectoAsync_RechazaFechaFinalAnteriorAFechaInicio()
    {
        var proyectos = new FakeProyectoRepository();
        var tareas = new FakeTareaRepository();
        var servicio = new ProyectoService(proyectos, tareas, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());
        var fechaInicio = DateTime.UtcNow.Date;
        var request = new ProyectoRequestDto
        {
            Name = "Proyecto de prueba",
            FechaInicio = fechaInicio,
            FechaFinalizacion = fechaInicio.AddDays(-1)
        };

        await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(request));

        Assert.Null(proyectos.AddedProyecto);
    }

    [Fact]
    public async Task CrearTareaAsync_NormalizaNombreYGuardaCuandoElProyectoExiste()
    {
        var proyecto = new Proyecto { Id = Guid.NewGuid() };
        var proyectos = new FakeProyectoRepository { Proyecto = proyecto };
        var tareas = new FakeTareaRepository();
        var unidadDeTrabajo = new FakeUnitOfWork(proyectos, tareas);
        var servicio = new TareaService(tareas, proyectos, unidadDeTrabajo, new FakeCurrentUser());
        var request = new TareaRequestDto
        {
            Name = "  Tarea de prueba  ",
            ProyectoId = proyecto.Id
        };

        var resultado = await servicio.CrearAsync(request);

        Assert.Equal("Tarea de prueba", resultado.Name);
        Assert.Equal(proyecto.Id, resultado.ProyectoId);
        Assert.NotNull(tareas.AddedTarea);
        Assert.Equal(1, unidadDeTrabajo.SaveChangesCount);
    }

    [Fact]
    public async Task CrearTareaAsync_RechazaProyectoInexistente()
    {
        var proyectos = new FakeProyectoRepository();
        var tareas = new FakeTareaRepository();
        var servicio = new TareaService(tareas, proyectos, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());
        var request = new TareaRequestDto
        {
            Name = "Tarea de prueba",
            ProyectoId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(request));

        Assert.Null(tareas.AddedTarea);
    }

    [Fact]
    public async Task ObtenerAvancePorcentajeAsync_CalculaPorcentajeDeTareasCompletadas()
    {
        var proyecto = new Proyecto { Id = Guid.NewGuid() };
        var proyectos = new FakeProyectoRepository { Proyecto = proyecto };
        var tareas = new FakeTareaRepository { Conteos = (4, 3) };
        var servicio = new ProyectoService(proyectos, tareas, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());

        var avance = await servicio.ObtenerAvancePorcentajeAsync(proyecto.Id);

        Assert.Equal(75m, avance);
    }

    [Fact]
    public async Task ObtenerAvancePorcentajeAsync_DevuelveCeroCuandoNoHayTareas()
    {
        var proyecto = new Proyecto { Id = Guid.NewGuid() };
        var proyectos = new FakeProyectoRepository { Proyecto = proyecto };
        var tareas = new FakeTareaRepository { Conteos = (0, 0) };
        var servicio = new ProyectoService(proyectos, tareas, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());

        var avance = await servicio.ObtenerAvancePorcentajeAsync(proyecto.Id);

        Assert.Equal(0m, avance);
    }

    [Fact]
    public async Task EliminarAsync_RechazaProyectoConTareasSinCompletar()
    {
        var proyecto = new Proyecto { Id = Guid.NewGuid() };
        var proyectos = new FakeProyectoRepository { Proyecto = proyecto };
        var tareas = new FakeTareaRepository { TieneSinCompletar = true };
        var servicio = new ProyectoService(proyectos, tareas, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());

        var exception = await Assert.ThrowsAsync<DomainException>(() => servicio.EliminarAsync(proyecto.Id));

        Assert.Equal("No se puede eliminar un proyecto que tiene tareas sin completar.", exception.Message);
        Assert.False(proyectos.DeleteCalled);
    }

    [Fact]
    public async Task CambiarEstadoAsync_ActualizaEstadoYFechaDeModificacion()
    {
        var tarea = new Tarea { Id = Guid.NewGuid() };
        var tareas = new FakeTareaRepository { Tarea = tarea };
        var proyectos = new FakeProyectoRepository();
        var servicio = new TareaService(tareas, proyectos, new FakeUnitOfWork(proyectos, tareas), new FakeCurrentUser());

        var resultado = await servicio.CambiarEstadoAsync(tarea.Id, EstadoTarea.completada);

        Assert.NotNull(resultado);
        Assert.Equal(EstadoTarea.completada, resultado.EstadoTarea);
        Assert.NotNull(tarea.UpdatedAt);
        Assert.True(tareas.UpdateCalled);
    }

    private sealed class FakeProyectoRepository : IProyectoRepository
    {
        public Proyecto? Proyecto { get; set; }
        public bool NombreExiste { get; init; }
        public Proyecto? AddedProyecto { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<Proyecto?> GetProyectoAsync(Guid proyectoId, Guid ownerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Proyecto?.Id == proyectoId ? Proyecto : null);
        }

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid ownerId,
            Guid? excludeProyectoId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(NombreExiste);

        public Task<IReadOnlyList<Proyecto>> GetAllAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Proyecto>>(Array.Empty<Proyecto>());

        public Task AddAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
        {
            AddedProyecto = proyecto;
            Proyecto = proyecto;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTareaRepository : ITareaRepository
    {
        public Tarea? Tarea { get; set; }
        public (int Total, int Completadas) Conteos { get; init; }
        public bool TieneSinCompletar { get; init; }
        public Tarea? AddedTarea { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<Tarea?> ObtenerPorIdAsync(Guid tareaId, Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tarea?.Id == tareaId ? Tarea : null);

        public Task<IReadOnlyList<Tarea>> ObtenerPorProyectoAsync(
            Guid proyectoId,
            Guid ownerId,
            string? name = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tarea>>(Array.Empty<Tarea>());

        public Task<IReadOnlyList<Tarea>> ObtenerPorEstadoAsync(
            Guid proyectoId,
            Guid ownerId,
            EstadoTarea estado,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tarea>>(Array.Empty<Tarea>());

        public Task<IReadOnlyList<Tarea>> ObtenerPorPrioridadAsync(
            Guid proyectoId,
            Guid ownerId,
            PrioridadTarea prioridad,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tarea>>(Array.Empty<Tarea>());

        public Task<IReadOnlyList<Tarea>> ObtenerVencidasNoCompletadasAsync(
            Guid proyectoId,
            Guid ownerId,
            DateTime fechaReferencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tarea>>(Array.Empty<Tarea>());

        public Task<(int Total, int Completadas)> ObtenerConteosPorProyectoAsync(
            Guid proyectoId,
            Guid ownerId,
            CancellationToken cancellationToken = default) => Task.FromResult(Conteos);

        public Task<bool> TieneTareasSinCompletarPorProyectoAsync(
            Guid proyectoId,
            Guid ownerId,
            CancellationToken cancellationToken = default) => Task.FromResult(TieneSinCompletar);

        public Task AgregarAsync(Tarea tarea, CancellationToken cancellationToken = default)
        {
            AddedTarea = tarea;
            Tarea = tarea;
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Tarea tarea, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Tarea tarea, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork(IProyectoRepository proyectos, ITareaRepository tareas) : IUnitOfWork
    {
        public IProyectoRepository Proyectos { get; } = proyectos;
        public ITareaRepository Tareas { get; } = tareas;
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public static Guid TestUserId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public Guid UserId => TestUserId;
    }
}
