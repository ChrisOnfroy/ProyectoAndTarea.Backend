using proyectandtask.Domain.Common;
using proyectandtask.Domain.Enums;

namespace proyectandtask.Domain.Entities;

public class Tarea : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime? FechaLimite { get; set; }
    
    public EstadoTarea EstadoTarea { get; set; } = EstadoTarea.pendiente;
    
    public PrioridadTarea? PrioridadTarea { get; set; } = null;
    
    public Guid ProyectoId { get; set; }
    
    public Proyecto? Proyecto { get; set; }

    public Tarea()
    {
        
    }

    public Tarea(String name, String description, DateTime? fechaLimite, EstadoTarea estadoTarea, PrioridadTarea prioridadTarea, Guid proyectoId, Proyecto? proyecto)
    {
        Name = name;
        Description = description;
        FechaLimite = fechaLimite;
        EstadoTarea = estadoTarea;
        PrioridadTarea = prioridadTarea;
        ProyectoId = proyectoId;
        Proyecto = proyecto;
    }

}