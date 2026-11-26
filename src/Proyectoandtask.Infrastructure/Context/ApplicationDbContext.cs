using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proyectandtask.Domain.Entities;
using Proyectoandtask.Infrastructure.Identity;

namespace Proyectoandtask.Infrastructure.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet <Proyecto> Proyectos => Set<Proyecto> ();
    public DbSet <Tarea> Tareas => Set<Tarea> ();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("citext");

        builder.Entity<ApplicationUser>()
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
