using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Constantes;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Helpers;

/// <summary>
/// Genera tokens JWT (HS256). Emite los claims multi-tenant:
/// token de negocio (tenant_id + rol, tipo_acceso=tenant) o de superadmin (tipo_acceso=plataforma, sin tenant_id).
/// </summary>
public class JwtTokenGenerator(IConfiguration configuration)
{
    public string GenerarToken(
        int usuarioId,
        string nombreUsuario,
        string? email,
        string? nombreApellido,
        bool habilitado,
        int? tenantId = null,
        string? rol = null,
        string tipoSesion = "Normal")
    {
        var secretKey = configuration["JwtSettings:Key"]
            ?? throw new InvalidOperationException("JwtSettings:Key no configurada.");
        var issuer = configuration["JwtSettings:Issuer"];
        var audience = configuration["JwtSettings:Audience"];
        var expirationMinutes = configuration.GetValue("JwtSettings:ExpirationInMinutes", 120);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, nombreUsuario),
            new(ClaimTypes.Email, email ?? ""),
            new("NombreApellido", nombreApellido ?? ""),
            new("TipoSesion", tipoSesion),
            new("Habilitado", habilitado.ToString()),
        };

        // Claims multi-tenant: con tenant_id => usuario de negocio; sin él => superadmin (plataforma).
        var esTenant = tenantId.HasValue;
        claims.Add(new Claim(
            CteSeguridad.ClaimTipoAcceso,
            esTenant ? CteSeguridad.TipoAccesoTenant : CteSeguridad.TipoAccesoPlataforma));

        if (esTenant)
            claims.Add(new Claim(CteSeguridad.ClaimTenantId, tenantId!.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(rol))
            claims.Add(new Claim(CteSeguridad.ClaimRol, rol));

        var now = DateTime.UtcNow;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            IssuedAt = now,
            Expires = now.AddMinutes(expirationMinutes),
            SigningCredentials = credentials,
            Issuer = issuer,
            Audience = audience
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}
