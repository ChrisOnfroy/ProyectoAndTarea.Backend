using proyectoandtask.Application.Dtos;
using proyectandtask.Domain.Enums;

namespace Proyectoandtask.Application.Interfaces;

public interface ITareaService
{
    Task<TareaResponseDto> CrearAsync(TareaRequestDto request, CancellationToken cancellationToken = default);
    Task<TareaResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorProyectoAsync(Guid proyectoId, string? name = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorEstadoAsync(Guid proyectoId, EstadoTarea estado, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorPrioridadAsync(Guid proyectoId, PrioridadTarea prioridad, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TareaResponseDto>?> ObtenerVencidasNoCompletadasAsync(Guid proyectoId, DateTime fechaReferencia, CancellationToken cancellationToken = default);
    Task<TareaResponseDto?> CambiarEstadoAsync(Guid id, EstadoTarea estado, CancellationToken cancellationToken = default);
    Task<TareaResponseDto?> ActualizarAsync(Guid id, TareaRequestDto request, CancellationToken cancellationToken = default);
    /// <returns><see langword="true"/> si se eliminó la tarea; <see langword="false"/> si no existe.</returns>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
