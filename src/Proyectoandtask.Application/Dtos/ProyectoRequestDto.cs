namespace proyectoandtask.Application.Dtos;

public class ProyectoRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
}
