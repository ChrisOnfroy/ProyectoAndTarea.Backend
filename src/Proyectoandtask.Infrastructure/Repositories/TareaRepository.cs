using Microsoft.EntityFrameworkCore;
using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;
using proyectoandtask.Application.Interfaces.Repositories;
using Proyectoandtask.Infrastructure.Context;

namespace Proyectoandtask.Infrastructure.Repositories;

public class TareaRepository : ITareaRepository
{
    
    private readonly ApplicationDbContext _context;
    
    public TareaRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Task<Tarea?> ObtenerPorIdAsync(Guid tareaId, Guid ownerId, CancellationToken cancellationToken = default, bool includeAllOwners = false)
    {
        return _context.Tareas
            .Where(t => includeAllOwners || (t.Proyecto != null && t.Proyecto.OwnerId == ownerId))
            .FirstOrDefaultAsync(t => t.Id == tareaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Tarea>> ObtenerPorProyectoAsync(Guid proyectoId, Guid ownerId, string? name = null, CancellationToken cancellationToken = default, bool includeAllOwners = false)
    {
        var query = _context.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == proyectoId && (includeAllOwners || (tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId)));

        if (!string.IsNullOrWhiteSpace(name))
        {
            var nombre = name.Trim();
            query = query.Where(tarea => EF.Functions.ILike(tarea.Name, $"%{nombre}%"));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tarea>> ObtenerPorEstadoAsync(Guid proyectoId, Guid ownerId, EstadoTarea estado, CancellationToken cancellationToken = default, bool includeAllOwners = false)
    {
        return await _context.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == proyectoId && (includeAllOwners || (tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId)) && tarea.EstadoTarea == estado)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tarea>> ObtenerPorPrioridadAsync(Guid proyectoId, Guid ownerId, PrioridadTarea prioridad, CancellationToken cancellationToken = default, bool includeAllOwners = false)
    {
        return await _context.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == proyectoId && (includeAllOwners || (tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId)) && tarea.PrioridadTarea == prioridad)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tarea>> ObtenerVencidasNoCompletadasAsync(Guid proyectoId, Guid ownerId, DateTime fechaReferencia,
        CancellationToken cancellationToken = default, bool includeAllOwners = false)
    {
        return await _context.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == proyectoId && (includeAllOwners || (tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId)) &&
                            tarea.FechaLimite.HasValue &&
                            tarea.FechaLimite.Value < fechaReferencia &&
                            tarea.EstadoTarea != EstadoTarea.completada)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Total, int Completadas)> ObtenerConteosPorProyectoAsync(
        Guid proyectoId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var conteos = await _context.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == proyectoId && tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId)
            .GroupBy(tarea => tarea.ProyectoId)
            .Select(grupo => new
            {
                Total = grupo.Count(),
                Completadas = grupo.Count(tarea => tarea.EstadoTarea == EstadoTarea.completada)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return conteos is null ? (0, 0) : (conteos.Total, conteos.Completadas);
    }

    public Task<bool> TieneTareasPendientesPorProyectoAsync(
        Guid proyectoId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return _context.Tareas
            .AsNoTracking()
            .AnyAsync(
                tarea => tarea.ProyectoId == proyectoId && tarea.Proyecto != null && tarea.Proyecto.OwnerId == ownerId && tarea.EstadoTarea == EstadoTarea.pendiente,
                cancellationToken);
    }

    public async Task AgregarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        await _context.Tareas.AddAsync(tarea, cancellationToken);
    }

    public Task ActualizarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.Entry(tarea).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.Tareas.Remove(tarea);
        return Task.CompletedTask;
    }
}
