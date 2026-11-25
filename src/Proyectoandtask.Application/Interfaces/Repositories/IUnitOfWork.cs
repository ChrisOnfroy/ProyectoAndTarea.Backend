namespace proyectoandtask.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IProyectoRepository Proyectos { get; }
    ITareaRepository Tareas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}