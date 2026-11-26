namespace Proyectoandtask.Application.Interfaces;

/// <summary>Identidad del usuario autenticado que realiza la solicitud actual.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
