using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using proyectandtask.Domain.Exceptions;
using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;
using Proyectoandtask.Infrastructure.Identity;

namespace Proyectoandtask.Infrastructure.Services;

public sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtOptions _jwtOptions;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        SignInManager<ApplicationUser> signInManager,
        JwtOptions jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _jwtOptions = jwtOptions;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            throw new DomainException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new DomainException("El correo electrónico es obligatorio.");
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            throw new DomainException("La contraseña es obligatoria.");
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            LockoutEnabled = true
        };

        await EnsureUserRoleAsync();

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException exception) when (IsEmailUniqueConstraintViolation(exception))
        {
            throw new DomainException(
                "Ya existe una cuenta registrada con ese correo electrónico.",
                exception);
        }

        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new DomainException(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.User);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new DomainException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        return await CreateAuthResponseAsync(user);
    }

    private async Task EnsureUserRoleAsync()
    {
        if (await _roleManager.RoleExistsAsync(ApplicationRoles.User))
            return;

        var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.User));
        if (!result.Succeeded && !await _roleManager.RoleExistsAsync(ApplicationRoles.User))
            throw new DomainException(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    private static bool IsEmailUniqueConstraintViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                       postgresException.ConstraintName == "EmailIndex";
            }
        }

        return false;
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            throw new DomainException("Credenciales inválidas.");
        }

        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new DomainException("Credenciales inválidas.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            throw new DomainException("Credenciales inválidas.");
        }

        return await CreateAuthResponseAsync(user);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            UserName = user.UserName ?? string.Empty,
            ExpiresAtUtc = expiresAt
        };
    }
}
