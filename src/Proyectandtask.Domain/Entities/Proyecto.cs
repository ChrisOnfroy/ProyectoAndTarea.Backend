using proyectandtask.Domain.Common;

namespace proyectandtask.Domain.Entities;

public class Proyecto : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? OwnerId { get; set; }
    
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFinalizacion { get; set; } 
    
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    
    public Proyecto()
    {
    }

    public Proyecto(
        string name,
        string description,
        DateTime fechaCreacion,
        DateTime fechaInicio,
        DateTime fechaFinalizacion)
    {
        Name = name;
        Description = description;
        FechaCreacion = fechaCreacion;
        FechaInicio = fechaInicio;
        FechaFinalizacion = fechaFinalizacion;
        
    }
}
