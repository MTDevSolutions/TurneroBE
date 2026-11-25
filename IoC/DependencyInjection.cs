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
using Microsoft.Extensions.Options;
using Shared.Settings;

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

        // Dominio: pacientes y agenda
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<ITurnoRepository, TurnoRepository>();
        services.AddScoped<ITurnoService, TurnoService>();

        // Plataforma: gestión de cuentas (superadmin)
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantAdminService, TenantAdminService>();

        // Multi-tenant: estado de la cuenta (suspensión y términos de uso)
        services.AddScoped<ITenantEstadoService, TenantEstadoService>();
        services.AddScoped<ITerminosService, TerminosService>();

        // WhatsApp (Wabix Gateway): config desde la sección "WhatsappGateway"; timeout por config.
        services.AddOptions<WhatsappGatewaySettings>().BindConfiguration(WhatsappGatewaySettings.Seccion);
        services.AddHttpClient<IWhatsappGateway, WhatsappGatewayClient>()
            .ConfigureHttpClient((sp, client) => client.Timeout = TimeSpan.FromSeconds(
                Math.Max(1, sp.GetRequiredService<IOptions<WhatsappGatewaySettings>>().Value.TimeoutSegundos)));
        services.AddScoped<IMensajeWhatsappRepository, MensajeWhatsappRepository>();
        services.AddScoped<IMensajeEntranteRepository, MensajeEntranteRepository>();
        services.AddScoped<IRespuestaWhatsappRepository, RespuestaWhatsappRepository>();
        services.AddScoped<IAvisoTurnoService, AvisoTurnoService>();
        services.AddScoped<IRespuestaWhatsappService, RespuestaWhatsappService>();
        services.AddScoped<IWhatsappConexionService, WhatsappConexionService>();
        services.AddScoped<IHistorialWhatsappService, HistorialWhatsappService>();
        services.AddScoped<ProcesadorEnvioWhatsapp>();
        services.AddScoped<ProcesadorRecordatorios>();
        services.AddHostedService<EnvioWhatsappWorker>();   // outbox: manda los pendientes al gateway
        services.AddHostedService<RecordatoriosWorker>();   // encola los recordatorios que tocan

        return services;
    }
}
