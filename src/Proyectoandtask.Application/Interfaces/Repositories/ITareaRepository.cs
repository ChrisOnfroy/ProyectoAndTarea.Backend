using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;

namespace proyectoandtask.Application.Interfaces.Repositories;

public interface ITareaRepository
{
    Task<Tarea?> ObtenerPorIdAsync(Guid tareaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tarea>> ObtenerPorProyectoAsync(Guid proyectoId, string? name = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tarea>> ObtenerPorEstadoAsync(Guid proyectoId, EstadoTarea estado, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tarea>> ObtenerPorPrioridadAsync(Guid proyectoId, PrioridadTarea prioridad, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tarea>> ObtenerVencidasNoCompletadasAsync(Guid proyectoId, DateTime fechaReferencia, CancellationToken cancellationToken = default);
    Task AgregarAsync(Tarea tarea, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Tarea tarea, CancellationToken cancellationToken = default);
    Task EliminarAsync(Tarea tarea, CancellationToken cancellationToken = default);
}
