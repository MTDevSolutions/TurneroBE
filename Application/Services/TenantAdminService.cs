using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Auth;
using Application.UseCases.Security_Context;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Plataforma;

namespace Application.Services;

public class TenantAdminService(
    ITenantRepository repository,
    IPasswordService passwordService,
    ISecurityContextService currentUser,
    ITenantEstadoService tenantEstadoService,
    IUnitOfWork uow) : ITenantAdminService
{
    public async Task<TenantDto> CrearAsync(TenantCrearDto dto, CancellationToken cancellationToken = default)
    {
        var especialidad = (dto.Especialidad ?? string.Empty).Trim().ToLowerInvariant();
        if (!CteEspecialidades.Todas.Contains(especialidad))
            throw AppException.Validacion($"Especialidad inválida. Valores posibles: {string.Join(", ", CteEspecialidades.Todas)}.");

        // El celular personal recibe los avisos de cancelación: uno mal cargado los manda a otra persona.
        var celular = TelefonoWhatsappHelper.Normalizar(dto.AdminCelular)
            ?? throw AppException.Validacion("El celular del profesional no es válido. Cargalo con código de área, sin 0 ni 15 (ej. 11 2345-6789).");

        var email = dto.AdminEmail.Trim();
        if (await repository.EmailUsuarioEnUsoAsync(email, cancellationToken))
            throw AppException.Conflicto($"El email '{email}' ya está en uso.");

        var tenant = new Tenant
        {
            Nombre = dto.Nombre.Trim(),
            Especialidad = especialidad,
            Plan = dto.Plan,
            Estado = CteEstadosTenant.Activo,
            FechaAlta = DateTime.UtcNow
        };

        var consultorio = new Sucursal
        {
            Nombre = dto.ConsultorioNombre.Trim(),
            Direccion = string.IsNullOrWhiteSpace(dto.ConsultorioDireccion) ? null : dto.ConsultorioDireccion.Trim(),
            Activa = true
        };

        var admin = new Usuario
        {
            Nombre = dto.AdminNombre.Trim(),
            Email = email,
            Rol = CteRoles.Admin,
            PasswordHash = passwordService.Encriptar(dto.AdminPassword),
            Celular = celular,
            Activo = true
        };

        await repository.CrearConAdminAsync(tenant, consultorio, admin, new Configuracion(), cancellationToken);
        return Mapear(tenant);
    }

    public async Task<List<TenantDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await repository.ListarAsync(cancellationToken);
        return tenants.Select(Mapear).ToList();
    }

    public Task SuspenderAsync(int id, CancellationToken cancellationToken = default)
        => CambiarEstadoAsync(id, CteEstadosTenant.Suspendido, cancellationToken);

    public Task ReactivarAsync(int id, CancellationToken cancellationToken = default)
        => CambiarEstadoAsync(id, CteEstadosTenant.Activo, cancellationToken);

    public async Task<PagoSuscripcionDto> RegistrarPagoAsync(int id, RegistrarPagoDto dto, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.RegistroNoExiste();

        if (dto.PeriodoHasta < dto.PeriodoDesde)
            throw AppException.Validacion("El período del pago termina antes de empezar.");

        var pago = new PagoSuscripcion
        {
            // El superadmin no tiene tenant en contexto, así que el stamp automático no aplica.
            TenantId = id,
            Monto = dto.Monto,
            PeriodoDesde = dto.PeriodoDesde,
            PeriodoHasta = dto.PeriodoHasta,
            FechaRegistro = DateTime.UtcNow,
            RegistradoPor = currentUser.Id
        };

        repository.AgregarPago(pago);
        await uow.GuardarAsync(cancellationToken);

        return new PagoSuscripcionDto
        {
            Id = pago.Id,
            TenantId = pago.TenantId,
            Monto = pago.Monto,
            PeriodoDesde = pago.PeriodoDesde,
            PeriodoHasta = pago.PeriodoHasta,
            FechaRegistro = pago.FechaRegistro,
            RegistradoPor = pago.RegistradoPor
        };
    }

    private async Task CambiarEstadoAsync(int id, string estado, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.RegistroNoExiste();

        tenant.Estado = estado;
        await uow.GuardarAsync(cancellationToken);

        // Sin esto, las sesiones abiertas siguen entrando hasta que venza el caché.
        tenantEstadoService.InvalidarCache(id);
    }

    private static TenantDto Mapear(Tenant t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Especialidad = t.Especialidad,
        Estado = t.Estado,
        Plan = t.Plan,
        FechaAlta = t.FechaAlta,
        FechaProximoPago = t.FechaProximoPago,
        FechaFinGracia = t.FechaFinGracia,
        WhatsappTelefonoId = t.WhatsappTelefonoId,
        TerminosVersionAceptada = t.TerminosVersionAceptada,
        TerminosAceptadosEn = t.TerminosAceptadosEn
    };
}
