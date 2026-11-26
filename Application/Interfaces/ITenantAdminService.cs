using Shared.DTOs.Plataforma;

namespace Application.Interfaces;

/// <summary>Operaciones de plataforma del superadmin sobre las cuentas (tenants).</summary>
public interface ITenantAdminService
{
    Task<TenantDto> CrearAsync(TenantCrearDto dto, CancellationToken cancellationToken = default);
    Task<List<TenantDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task SuspenderAsync(int id, CancellationToken cancellationToken = default);
    Task ReactivarAsync(int id, CancellationToken cancellationToken = default);
    Task<PagoSuscripcionDto> RegistrarPagoAsync(int id, RegistrarPagoDto dto, CancellationToken cancellationToken = default);

    /// <summary>Vincula la cuenta a un teléfono de Wabix Gateway (null = desvincular). Valida que exista.</summary>
    Task<TenantDto> VincularWhatsappAsync(int id, int? telefonoId, CancellationToken cancellationToken = default);
}
