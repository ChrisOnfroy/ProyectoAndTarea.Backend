using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;

namespace proyectoandtask.Application.Interfaces.Repositories;

public interface ITareaRepository
{
    Task<Tarea?> ObtenerPorIdAsync(Guid tareaId, Guid ownerId, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    Task<IReadOnlyList<Tarea>> ObtenerPorProyectoAsync(Guid proyectoId, Guid ownerId, string? name = null, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    Task<IReadOnlyList<Tarea>> ObtenerPorEstadoAsync(Guid proyectoId, Guid ownerId, EstadoTarea estado, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    Task<IReadOnlyList<Tarea>> ObtenerPorPrioridadAsync(Guid proyectoId, Guid ownerId, PrioridadTarea prioridad, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    Task<IReadOnlyList<Tarea>> ObtenerVencidasNoCompletadasAsync(Guid proyectoId, Guid ownerId, DateTime fechaReferencia, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    Task<(int Total, int Completadas)> ObtenerConteosPorProyectoAsync(Guid proyectoId, Guid ownerId, CancellationToken cancellationToken = default);
    Task<bool> TieneTareasPendientesPorProyectoAsync(Guid proyectoId, Guid ownerId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Tarea tarea, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Tarea tarea, CancellationToken cancellationToken = default);
    Task EliminarAsync(Tarea tarea, CancellationToken cancellationToken = default);
}
