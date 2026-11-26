using proyectoandtask.Application.Dtos;

namespace Proyectoandtask.Application.Interfaces;

public interface IProyectoService
{
    Task<ProyectoResponseDto> CrearAsync(ProyectoRequestDto request, CancellationToken cancellationToken = default);
    Task<ProyectoResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProyectoResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<decimal?> ObtenerAvancePorcentajeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProyectoResponseDto?> ActualizarAsync(Guid id, ProyectoRequestDto request, CancellationToken cancellationToken = default);
    /// <returns><see langword="true"/> si se eliminó el proyecto; <see langword="false"/> si no existe.</returns>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
