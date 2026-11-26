using Microsoft.EntityFrameworkCore;
using proyectandtask.Domain.Entities;
using proyectoandtask.Application.Interfaces.Repositories;
using Proyectoandtask.Infrastructure.Context;

namespace Proyectoandtask.Infrastructure.Repositories;

public class ProyectoRepository : IProyectoRepository
{
    private readonly ApplicationDbContext _context;

    public ProyectoRepository(ApplicationDbContext context)
    {
        _context = context;
    }


    public Task<Proyecto?> GetProyectoAsync(Guid proyectoId, CancellationToken cancellationToken = default)
    {
        return _context.Proyectos
            .FirstOrDefaultAsync(proyecto => proyecto.Id == proyectoId, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeProyectoId = null,
        CancellationToken cancellationToken = default)
    {
        var nombreNormalizado = name.Trim();
        var query = _context.Proyectos
            .AsNoTracking()
            .Where(proyecto => proyecto.Name == nombreNormalizado);

        if (excludeProyectoId.HasValue)
        {
            query = query.Where(proyecto => proyecto.Id != excludeProyectoId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Proyecto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Proyectos
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
    {
        await _context.Proyectos.AddAsync(proyecto, cancellationToken);
    }

    public Task UpdateAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.Entry(proyecto).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Proyecto proyecto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.Proyectos.Remove(proyecto);
        return Task.CompletedTask;
    }
}
