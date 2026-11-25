using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Exceptions;
using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;
using proyectoandtask.Application.Interfaces.Repositories;

namespace Proyectoandtask.Application.Services;

public class ProyectoService : IProyectoService
{
    private readonly IProyectoRepository _proyectoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProyectoService(IProyectoRepository proyectoRepository, IUnitOfWork unitOfWork)
    {
        _proyectoRepository = proyectoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProyectoResponseDto> CrearAsync(ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.FechaFinalizacion == DateTime.MinValue)
        {
            throw new DomainException("La fecha de finalización es obligatoria y debe ser válida.");
        }
        
        var nombre = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del proyecto es obligatorio.");

        if (await _proyectoRepository.ExistsByNameAsync(
                nombre,
                cancellationToken: cancellationToken))
        {
            throw new DomainException($"Ya existe un proyecto con el nombre '{nombre}'.");
        }
        
        var proyecto = new Proyecto
        {
            Name = nombre,
            Description = request.Description,
            FechaCreacion = DateTime.UtcNow,
            FechaFinalizacion = request.FechaFinalizacion
        };

        await _proyectoRepository.AddAsync(proyecto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(proyecto);

    }

    public async Task<ProyectoResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var proyecto = await _proyectoRepository.GetProyectoAsync(id, cancellationToken);
        return proyecto is null ? null : MapToDto(proyecto);
    }

    public async Task<IReadOnlyList<ProyectoResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        var proyectos = await _proyectoRepository.GetAllAsync(cancellationToken);
        return proyectos.Select(MapToDto).ToList();
        
    }

    public async Task<ProyectoResponseDto?> ActualizarAsync(Guid id, ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        var proyecto =  await _proyectoRepository.GetProyectoAsync(id, cancellationToken);
        if (proyecto is null) return null;
        
        if (request.FechaFinalizacion == DateTime.MinValue)
        {
            throw new DomainException("La fecha de finalización es obligatoria y debe ser válida.");
        }
        
        var nombre = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del proyecto es obligatorio.");

        if (await _proyectoRepository.ExistsByNameAsync(
                nombre,
                id,
                cancellationToken))
        {
            throw new DomainException($"Ya existe un proyecto con el nombre '{nombre}'.");
        }
        
        proyecto.Name = nombre;
        proyecto.Description = request.Description;
        proyecto.FechaFinalizacion = request.FechaFinalizacion;
        proyecto.SetUpdatedAt();
        
        
        
        await _proyectoRepository.UpdateAsync(proyecto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(proyecto);
        
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var proyecto =  await _proyectoRepository.GetProyectoAsync(id, cancellationToken);
        if (proyecto is null) return false;
        
        await _proyectoRepository.DeleteAsync(proyecto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProyectoResponseDto MapToDto(Proyecto proyecto) => new()
    {
        Id = proyecto.Id,
        Name = proyecto.Name,
        Description = proyecto.Description,
        FechaCreacion = proyecto.FechaCreacion,
        FechaFinalizacion = proyecto.FechaFinalizacion
    };

}
