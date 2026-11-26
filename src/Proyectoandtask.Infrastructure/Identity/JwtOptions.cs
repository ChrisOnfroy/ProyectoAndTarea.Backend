using System.Text;

namespace Proyectoandtask.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(SecretKey);

        if (Encoding.UTF8.GetByteCount(SecretKey) < 32)
        {
            throw new ArgumentException("La clave JWT debe tener al menos 32 bytes.", nameof(SecretKey));
        }

        if (AccessTokenMinutes is < 1 or > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(AccessTokenMinutes), "La duración del token debe estar entre 1 y 60 minutos.");
        }
    }
}
