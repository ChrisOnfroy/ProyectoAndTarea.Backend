using proyectandtask.Domain.Entities;

namespace proyectoandtask.Application.Interfaces.Repositories;

public interface IProyectoRepository
{
    Task<Proyecto?> GetProyectoAsync(Guid proyectoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Proyecto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Proyecto proyecto, CancellationToken cancellationToken = default);
}
