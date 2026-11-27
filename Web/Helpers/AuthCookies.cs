using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Web.Helpers;

/// <summary>
/// Escribe las cookies de autenticación de forma consistente:
/// <c>access_token</c> (HttpOnly) y <c>csrf_token</c> (legible por JS, para el double-submit).
/// SameSite/Secure se toman de config (CookieSettings).
/// </summary>
public static class AuthCookies
{
    public static string Escribir(HttpContext context, IConfiguration configuration, string accessToken, DateTimeOffset expira)
    {
        var sameSite = ParseSameSite(configuration["CookieSettings:SameSite"]);
        var secure = configuration.GetValue("CookieSettings:Secure", false)
                     || context.Request.IsHttps
                     || sameSite == SameSiteMode.None; // SameSite=None exige Secure

        context.Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = expira
        });

        var csrfToken = GenerarCsrfToken();
        context.Response.Cookies.Append("csrf_token", csrfToken, new CookieOptions
        {
            HttpOnly = false, // el SPA lo lee y lo reenvía en el header X-CSRF-Token
            Secure = secure,
            SameSite = sameSite,
            Expires = expira
        });

        return csrfToken;
    }

    /// <summary>Expira <c>access_token</c> y <c>csrf_token</c> para invalidar la sesión (logout).</summary>
    public static void Borrar(HttpContext context, IConfiguration configuration)
    {
        var sameSite = ParseSameSite(configuration["CookieSettings:SameSite"]);
        var secure = configuration.GetValue("CookieSettings:Secure", false)
                     || context.Request.IsHttps
                     || sameSite == SameSiteMode.None;

        context.Response.Cookies.Append("access_token", string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UnixEpoch
        });

        context.Response.Cookies.Append("csrf_token", string.Empty, new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UnixEpoch
        });
    }

    private static SameSiteMode ParseSameSite(string? value)
        => Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var parsed) ? parsed : SameSiteMode.Lax;

    // Hex => URL-safe (sin / + =), así no se URL-encodea en la cookie y el SPA lo manda tal cual.
    private static string GenerarCsrfToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
