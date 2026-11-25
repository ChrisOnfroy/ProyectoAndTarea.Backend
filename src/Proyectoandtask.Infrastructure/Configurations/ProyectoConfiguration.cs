using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyectandtask.Domain.Entities;

namespace Proyectoandtask.Infrastructure.Configurations;

public sealed class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("Proyectos");

        builder.HasKey(proyecto => proyecto.Id);

        builder.Property(proyecto => proyecto.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(proyecto => proyecto.Description)
            .HasMaxLength(1000);

        builder.Property(proyecto => proyecto.FechaCreacion)
            .IsRequired();

        builder.Property(proyecto => proyecto.FechaFinalizacion);

        builder.Property(proyecto => proyecto.CreatedAt)
            .IsRequired();

        builder.HasMany(proyecto => proyecto.Tareas)
            .WithOne(tarea => tarea.Proyecto)
            .HasForeignKey(tarea => tarea.ProyectoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
