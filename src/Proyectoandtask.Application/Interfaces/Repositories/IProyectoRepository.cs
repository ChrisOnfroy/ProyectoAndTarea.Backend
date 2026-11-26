using proyectandtask.Domain.Entities;

namespace proyectoandtask.Application.Interfaces.Repositories;

public interface IProyectoRepository
{
    Task<Proyecto?> GetProyectoAsync(Guid proyectoId, Guid ownerId, CancellationToken cancellationToken = default, bool includeAllOwners = false);
    
    Task<bool> ExistsByNameAsync(
        string name,
        Guid ownerId,
        Guid? excludeProyectoId = null,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Proyecto>> GetAllAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
}
