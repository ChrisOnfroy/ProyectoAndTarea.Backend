using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Proyectandtask.Api;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];
        var methodAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var attributes = controllerAttributes.Concat(methodAttributes).ToArray();

        if (attributes.OfType<AllowAnonymousAttribute>().Any() ||
            !attributes.OfType<AuthorizeAttribute>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
