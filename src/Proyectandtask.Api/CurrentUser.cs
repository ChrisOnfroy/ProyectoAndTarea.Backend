using System.Security.Claims;
using Proyectoandtask.Application.Interfaces;

namespace Proyectandtask.Api;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");

            return userId;
        }
    }
}
