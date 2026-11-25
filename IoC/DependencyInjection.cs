using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Auth;
using Application.Services;
using Application.UseCases.Auth;
using Application.UseCases.Security_Context;
using Infrastructure.Data;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IoC;

/// <summary>
/// Registro centralizado de servicios. Web referencia solo a IoC.
/// Acá se irán sumando repositorios y servicios a medida que crezcan las features.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        // Unit of Work: punto de commit centralizado
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Contexto de seguridad (usuario autenticado)
        services.AddScoped<ISecurityContextService, CurrentUserService>();

        // Contexto de tenant (multi-tenant, resuelto desde el JWT)
        services.AddScoped<ITenantContext, TenantContext>();

        // Generación de JWT
        services.AddScoped<JwtTokenGenerator>();

        // Auth: hashing de passwords
        services.AddSingleton<IPasswordService, BcryptPasswordService>();

        // Auth: login de superadmin
        services.AddScoped<ISuperadminRepository, SuperadminRepository>();
        services.AddScoped<AutenticacionSuperadmin>();

        // Auth: login de usuario de la cuenta
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<AutenticacionUsuario>();

        // Dominio: consultorios (sucursales)
        services.AddScoped<ISucursalRepository, SucursalRepository>();
        services.AddScoped<ISucursalService, SucursalService>();

        // Dominio: configuración de la agenda por cuenta
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddScoped<IConfiguracionService, ConfiguracionService>();

        // Plataforma: gestión de cuentas (superadmin)
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantAdminService, TenantAdminService>();

        // Multi-tenant: estado de la cuenta (suspensión y términos de uso)
        services.AddScoped<ITenantEstadoService, TenantEstadoService>();
        services.AddScoped<ITerminosService, TerminosService>();

        return services;
    }
}
