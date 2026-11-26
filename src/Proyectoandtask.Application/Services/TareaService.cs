using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Exceptions;
using proyectandtask.Domain.Enums;
using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;
using proyectoandtask.Application.Interfaces.Repositories;

namespace Proyectoandtask.Application.Services;

public class TareaService : ITareaService
{
    private readonly ITareaRepository _tareaRepository;
    private readonly IProyectoRepository _proyectoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public TareaService(
        ITareaRepository tareaRepository,
        IProyectoRepository proyectoRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tareaRepository = tareaRepository;
        _proyectoRepository = proyectoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TareaResponseDto> CrearAsync(TareaRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ownerId = _currentUser.UserId;
        var nombre = ValidarYNormalizarNombre(request.Name);
        ValidarDescripcion(request.Description);
        ValidarFechaLimite(request.FechaLimite);
        ValidarEstado(request.EstadoTarea);
        ValidarPrioridad(request.PrioridadTarea);
        await ValidarProyectoAsync(request.ProyectoId, ownerId, cancellationToken);

        var tarea = new Tarea
        {
            Name = nombre,
            Description = request.Description,
            FechaLimite = request.FechaLimite,
            EstadoTarea = request.EstadoTarea,
            PrioridadTarea = request.PrioridadTarea,
            ProyectoId = request.ProyectoId
        };

        await _tareaRepository.AgregarAsync(tarea, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(tarea);
    }

    public async Task<TareaResponseDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var tarea = await _tareaRepository.ObtenerPorIdAsync(id, ownerId, cancellationToken);
        return tarea is null ? null : MapToDto(tarea);
    }

    public async Task<TareaResponseDto?> CambiarEstadoAsync(
        Guid id,
        EstadoTarea estado,
        CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        ValidarEstado(estado);

        var tarea = await _tareaRepository.ObtenerPorIdAsync(id, ownerId, cancellationToken);
        if (tarea is null)
            return null;

        tarea.EstadoTarea = estado;
        tarea.SetUpdatedAt();

        await _tareaRepository.ActualizarAsync(tarea, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(tarea);
    }

    public async Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorProyectoAsync(
        Guid proyectoId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        if (await _proyectoRepository.GetProyectoAsync(proyectoId, ownerId, cancellationToken) is null)
            return null;
        var nombre = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var tareas = await _tareaRepository.ObtenerPorProyectoAsync(proyectoId, ownerId, nombre, cancellationToken);
        return tareas.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorEstadoAsync(
        Guid proyectoId,
        EstadoTarea estado,
        CancellationToken cancellationToken = default)
    {
        ValidarEstado(estado);
        var ownerId = _currentUser.UserId;
        if (await _proyectoRepository.GetProyectoAsync(proyectoId, ownerId, cancellationToken) is null)
            return null;
        var tareas = await _tareaRepository.ObtenerPorEstadoAsync(proyectoId, ownerId, estado, cancellationToken);
        return tareas.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TareaResponseDto>?> ObtenerPorPrioridadAsync(
        Guid proyectoId,
        PrioridadTarea prioridad,
        CancellationToken cancellationToken = default)
    {
        ValidarPrioridad(prioridad);
        var ownerId = _currentUser.UserId;
        if (await _proyectoRepository.GetProyectoAsync(proyectoId, ownerId, cancellationToken) is null)
            return null;
        var tareas = await _tareaRepository.ObtenerPorPrioridadAsync(proyectoId, ownerId, prioridad, cancellationToken);
        return tareas.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TareaResponseDto>?> ObtenerVencidasNoCompletadasAsync(
        Guid proyectoId,
        DateTime fechaReferencia,
        CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        if (await _proyectoRepository.GetProyectoAsync(proyectoId, ownerId, cancellationToken) is null)
            return null;

        if (fechaReferencia == DateTime.MinValue)
            throw new DomainException("La fecha de referencia debe ser válida.");

        var tareas = await _tareaRepository.ObtenerVencidasNoCompletadasAsync(
            proyectoId,
            ownerId,
            fechaReferencia,
            cancellationToken);
        return tareas.Select(MapToDto).ToList();
    }

    public async Task<TareaResponseDto?> ActualizarAsync(
        Guid id,
        TareaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ownerId = _currentUser.UserId;
        var tarea = await _tareaRepository.ObtenerPorIdAsync(id, ownerId, cancellationToken);
        if (tarea is null)
            return null;

        var nombre = ValidarYNormalizarNombre(request.Name);
        ValidarDescripcion(request.Description);
        ValidarFechaLimite(request.FechaLimite);
        ValidarEstado(request.EstadoTarea);
        ValidarPrioridad(request.PrioridadTarea);
        await ValidarProyectoAsync(request.ProyectoId, ownerId, cancellationToken);

        tarea.Name = nombre;
        tarea.Description = request.Description;
        tarea.FechaLimite = request.FechaLimite;
        tarea.EstadoTarea = request.EstadoTarea;
        tarea.PrioridadTarea = request.PrioridadTarea;
        tarea.ProyectoId = request.ProyectoId;
        tarea.SetUpdatedAt();

        await _tareaRepository.ActualizarAsync(tarea, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(tarea);
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        var tarea = await _tareaRepository.ObtenerPorIdAsync(id, ownerId, cancellationToken);
        if (tarea is null)
            return false;

        await _tareaRepository.EliminarAsync(tarea, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidarProyectoAsync(Guid proyectoId, Guid ownerId, CancellationToken cancellationToken)
    {
        if (proyectoId == Guid.Empty ||
            await _proyectoRepository.GetProyectoAsync(proyectoId, ownerId, cancellationToken) is null)
        {
            throw new DomainException("El proyecto indicado no existe.");
        }
    }

    private static string ValidarYNormalizarNombre(string? nombre)
    {
        var nombreNormalizado = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            throw new DomainException("El nombre de la tarea es obligatorio.");

        if (nombreNormalizado.Length > 200)
            throw new DomainException("El nombre de la tarea no puede superar los 200 caracteres.");

        return nombreNormalizado;
    }

    private static void ValidarDescripcion(string? descripcion)
    {
        if (descripcion?.Length > 1000)
            throw new DomainException("La descripción de la tarea no puede superar los 1000 caracteres.");
    }

    private static void ValidarFechaLimite(DateTime? fechaLimite)
    {
        if (fechaLimite == DateTime.MinValue)
            throw new DomainException("La fecha límite debe ser válida.");
    }

    private static void ValidarEstado(EstadoTarea estado)
    {
        if (!Enum.IsDefined(estado))
            throw new DomainException("El estado de la tarea no es válido.");
    }

    private static void ValidarPrioridad(PrioridadTarea? prioridad)
    {
        if (prioridad.HasValue && !Enum.IsDefined(prioridad.Value))
            throw new DomainException("La prioridad de la tarea no es válida.");
    }

    private static TareaResponseDto MapToDto(Tarea tarea) => new()
    {
        Id = tarea.Id,
        Name = tarea.Name,
        Description = tarea.Description,
        FechaLimite = tarea.FechaLimite,
        EstadoTarea = tarea.EstadoTarea,
        PrioridadTarea = tarea.PrioridadTarea,
        ProyectoId = tarea.ProyectoId
    };
}
