using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;

namespace Proyectoandtask.Application.Services;

public class ProyectoService : IProyectoService
{
    public Task<ProyectoResponseDto> CrearAsync(ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProyectoResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ProyectoResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProyectoResponseDto?> ActualizarAsync(Guid id, ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
