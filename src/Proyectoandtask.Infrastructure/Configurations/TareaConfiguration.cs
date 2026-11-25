using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyectandtask.Domain.Entities;

namespace Proyectoandtask.Infrastructure.Configurations;

public sealed class TareaConfiguration : IEntityTypeConfiguration<Tarea>
{
    public void Configure(EntityTypeBuilder<Tarea> builder)
    {
        builder.ToTable("Tareas");

        builder.HasKey(tarea => tarea.Id);

        builder.Property(tarea => tarea.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tarea => tarea.Description)
            .HasMaxLength(1000);

        builder.Property(tarea => tarea.FechaLimite);

        builder.Property(tarea => tarea.EstadoTarea)
            .IsRequired();

        builder.Property(tarea => tarea.PrioridadTarea);

        builder.Property(tarea => tarea.CreatedAt)
            .IsRequired();

        builder.HasIndex(tarea => tarea.ProyectoId);
    }
}
