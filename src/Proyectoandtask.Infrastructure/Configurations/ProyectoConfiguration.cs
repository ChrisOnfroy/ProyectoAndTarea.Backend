using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyectandtask.Domain.Entities;
using Proyectoandtask.Infrastructure.Identity;

namespace Proyectoandtask.Infrastructure.Configurations;

public sealed class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("Proyectos");

        builder.HasKey(proyecto => proyecto.Id);

        builder.Property(proyecto => proyecto.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("citext");

        builder.HasIndex(proyecto => new { proyecto.OwnerId, proyecto.Name })
            .HasDatabaseName("IX_Proyectos_Name")
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(proyecto => proyecto.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(proyecto => proyecto.Description)
            .HasMaxLength(1000);

        builder.Property(proyecto => proyecto.FechaCreacion)
            .IsRequired();

        builder.Property(proyecto => proyecto.FechaInicio)
            .IsRequired();

        builder.Property(proyecto => proyecto.FechaFinalizacion)
            .IsRequired();

        builder.Property(proyecto => proyecto.CreatedAt)
            .IsRequired();

        builder.HasMany(proyecto => proyecto.Tareas)
            .WithOne(tarea => tarea.Proyecto)
            .HasForeignKey(tarea => tarea.ProyectoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
