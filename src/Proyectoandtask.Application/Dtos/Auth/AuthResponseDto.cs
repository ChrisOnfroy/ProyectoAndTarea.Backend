namespace proyectoandtask.Application.Dtos;

public sealed class AuthResponseDto
{
    public required string AccessToken { get; init; }
    public required string UserName { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
