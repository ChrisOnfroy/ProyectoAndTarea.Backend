using proyectandtask.Domain.Enums;

namespace proyectoandtask.Application.Dtos;

public class TareaRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? FechaLimite { get; set; }
    public EstadoTarea EstadoTarea { get; set; } = EstadoTarea.pendiente;
    public PrioridadTarea? PrioridadTarea { get; set; }
    public Guid ProyectoId { get; set; }
}
