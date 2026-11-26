using proyectoandtask.Application.Interfaces.Repositories;
using Proyectoandtask.Infrastructure.Context;

namespace Proyectoandtask.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IUnitOfWork"/> con Entity Framework Core para coordinar los
/// repositorios y guardar sus cambios pendientes en una única operación.
/// </summary>
/// <remarks>
/// El contexto y los repositorios deben registrarse con un ciclo de vida compartido
/// (normalmente scoped) para que todos los cambios se guarden en la misma unidad de trabajo.
/// El ciclo de vida del contexto se administra mediante inyección de dependencias.
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Crea una unidad de trabajo con el contexto y los repositorios de proyectos y tareas.
    /// </summary>
    /// <param name="context">Contexto de EF Core que persiste los cambios.</param>
    /// <param name="proyectos">Repositorio de proyectos asociado al contexto.</param>
    /// <param name="tareas">Repositorio de tareas asociado al mismo contexto.</param>
    /// <exception cref="ArgumentNullException">
    /// Se produce si el contexto o alguno de los repositorios es <see langword="null"/>.
    /// </exception>
    public UnitOfWork(
        ApplicationDbContext context,
        IProyectoRepository proyectos,
        ITareaRepository tareas)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(proyectos);
        ArgumentNullException.ThrowIfNull(tareas);

        _context = context;
        Proyectos = proyectos;
        Tareas = tareas;
    }

    /// <summary>
    /// Obtiene el repositorio de proyectos que comparte esta unidad de trabajo.
    /// </summary>
    public IProyectoRepository Proyectos { get; }

    /// <summary>
    /// Obtiene el repositorio de tareas que comparte esta unidad de trabajo.
    /// </summary>
    public ITareaRepository Tareas { get; }

    /// <summary>
    /// Persiste en la base de datos todos los cambios pendientes en el contexto compartido.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar la operación de guardado.</param>
    /// <returns>La cantidad de entradas de entidad escritas en la base de datos.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
