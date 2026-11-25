using Microsoft.Extensions.DependencyInjection;
using Proyectoandtask.Application.Interfaces;
using Proyectoandtask.Application.Services;

namespace Proyectoandtask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProyectoService, ProyectoService>();
        services.AddScoped<ITareaService, TareaService>();
        return services;
    }
}
