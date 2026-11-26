using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Exceptions;
using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;
using proyectoandtask.Application.Interfaces.Repositories;

namespace Proyectoandtask.Application.Services;

public class ProyectoService : IProyectoService
{
    private readonly IProyectoRepository _proyectoRepository;
    private readonly ITareaRepository _tareaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ProyectoService(
        IProyectoRepository proyectoRepository,
        ITareaRepository tareaRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _proyectoRepository = proyectoRepository;
        _tareaRepository = tareaRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ProyectoResponseDto> CrearAsync(ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ownerId = _currentUser.UserId;
        ValidarFechas(request.FechaInicio, request.FechaFinalizacion);
        var nombre = ValidarYNormalizarNombre(request.Name);
        ValidarDescripcion(request.Description);

        if (await _proyectoRepository.ExistsByNameAsync(
                nombre,
                ownerId,
                cancellationToken: cancellationToken))
        {
            throw new DomainException($"Ya existe un proyecto con el nombre '{nombre}'.");
        }
        
        var proyecto = new Proyecto
        {
            Name = nombre,
            OwnerId = ownerId,
            Description = request.Description,
            FechaCreacion = DateTime.UtcNow,
            FechaInicio = request.FechaInicio,
            FechaFinalizacion = request.FechaFinalizacion
        };

        await _proyectoRepository.AddAsync(proyecto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(proyecto);

    }

    public async Task<ProyectoResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var proyecto = await _proyectoRepository.GetProyectoAsync(id, ownerId, cancellationToken);
        return proyecto is null ? null : MapToDto(proyecto);
    }

    public async Task<IReadOnlyList<ProyectoResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var proyectos = await _proyectoRepository.GetAllAsync(ownerId, cancellationToken);
        return proyectos.Select(MapToDto).ToList();
        
    }

    public async Task<decimal?> ObtenerAvancePorcentajeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var proyecto = await _proyectoRepository.GetProyectoAsync(id, ownerId, cancellationToken);
        if (proyecto is null)
            return null;

        var (total, completadas) = await _tareaRepository.ObtenerConteosPorProyectoAsync(id, ownerId, cancellationToken);
        if (total == 0)
            return 0m;

        return Math.Round((decimal)completadas * 100m / total, 2);
    }

    public async Task<ProyectoResponseDto?> ActualizarAsync(Guid id, ProyectoRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ownerId = _currentUser.UserId;
        var proyecto =  await _proyectoRepository.GetProyectoAsync(id, ownerId, cancellationToken);
        if (proyecto is null) return null;
        
        ValidarFechas(request.FechaInicio, request.FechaFinalizacion);
        var nombre = ValidarYNormalizarNombre(request.Name);
        ValidarDescripcion(request.Description);

        if (await _proyectoRepository.ExistsByNameAsync(
                nombre,
                ownerId,
                id,
                cancellationToken))
        {
            throw new DomainException($"Ya existe un proyecto con el nombre '{nombre}'.");
        }
        
        proyecto.Name = nombre;
        proyecto.Description = request.Description;
        proyecto.FechaInicio = request.FechaInicio;
        proyecto.FechaFinalizacion = request.FechaFinalizacion;
        proyecto.SetUpdatedAt();
        
        
        
        await _proyectoRepository.UpdateAsync(proyecto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(proyecto);
        
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var proyecto =  await _proyectoRepository.GetProyectoAsync(id, ownerId, cancellationToken);
        if (proyecto is null) return false;

        if (await _tareaRepository.TieneTareasSinCompletarPorProyectoAsync(id, ownerId, cancellationToken))
        {
            throw new DomainException("No se puede eliminar un proyecto que tiene tareas sin completar.");
        }
        
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
        FechaInicio =proyecto.FechaInicio,
        FechaFinalizacion = proyecto.FechaFinalizacion
    };

    private static void ValidarFechas(DateTime fechaInicio, DateTime fechaFinalizacion)
    {
        if (fechaInicio == DateTime.MinValue)
            throw new DomainException("La fecha de inicio es obligatoria y debe ser válida.");

        if (fechaFinalizacion == DateTime.MinValue)
            throw new DomainException("La fecha de finalización es obligatoria y debe ser válida.");

        if (fechaFinalizacion < fechaInicio)
            throw new DomainException("La fecha de finalización no puede ser anterior a la fecha de inicio.");
    }

    private static string ValidarYNormalizarNombre(string? nombre)
    {
        var nombreNormalizado = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            throw new DomainException("El nombre del proyecto es obligatorio.");

        if (nombreNormalizado.Length > 200)
            throw new DomainException("El nombre del proyecto no puede superar los 200 caracteres.");

        return nombreNormalizado;
    }

    private static void ValidarDescripcion(string? descripcion)
    {
        if (descripcion?.Length > 1000)
            throw new DomainException("La descripción del proyecto no puede superar los 1000 caracteres.");
    }

}
