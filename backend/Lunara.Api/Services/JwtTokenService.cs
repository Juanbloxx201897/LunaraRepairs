using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lunara.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Lunara.Api.Services;

// Construye tokens firmados que identifican al usuario y su rol durante la sesión.
public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(AuthUser user)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "Falta configurar Jwt:Secret.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var username = user.Role is "Admin" or "Administrator"
            ? configuration["Admin:Username"] ?? "admin"
            : user.Email.Split('@', 2)[0];

        // Los identificadores vinculados permiten reconocer el cliente o técnico del usuario.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("username", username),
            new("sessionKey", user.SessionKey)
        };

        if (user.ClientId.HasValue)
            claims.Add(new Claim("clientId", user.ClientId.Value.ToString()));

        if (user.TechnicianId.HasValue)
            claims.Add(new Claim(
                "technicianId", user.TechnicianId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
