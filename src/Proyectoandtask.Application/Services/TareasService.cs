using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;
using proyectandtask.Domain.Enums;

namespace Proyectoandtask.Application.Services;

public class TareasService : ITareaService
{
    public Task<TareaResponseDto> CrearAsync(TareaRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TareaResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TareaResponseDto>> ObtenerPorProyectoAsync(Guid proyectoId, string? name = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TareaResponseDto>> ObtenerPorEstadoAsync(Guid proyectoId, EstadoTarea estado, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TareaResponseDto>> ObtenerPorPrioridadAsync(Guid proyectoId, PrioridadTarea prioridad, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TareaResponseDto>> ObtenerVencidasNoCompletadasAsync(Guid proyectoId, DateTime fechaReferencia,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TareaResponseDto?> ActualizarAsync(Guid id, TareaRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
