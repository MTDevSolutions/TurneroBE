using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Persistencia de las entidades de plataforma (Tenant, PagoSuscripcion).</summary>
public interface ITenantRepository
{
    /// <summary>
    /// Crea el tenant + su primer consultorio (sucursal) + su admin + su configuración,
    /// en una sola transacción.
    /// </summary>
    Task<Tenant> CrearConAdminAsync(Tenant tenant, Sucursal sucursal, Usuario admin, Configuracion configuracion, CancellationToken cancellationToken = default);

    Task<List<Tenant>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Trae el tenant trackeado; null si no existe.</summary>
    Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Suma un pago de suscripción al scope. El TenantId lo setea el servicio.</summary>
    void AgregarPago(PagoSuscripcion pago);

    /// <summary>True si el email ya está usado por algún usuario (chequeo GLOBAL, sin filtro de tenant).</summary>
    Task<bool> EmailUsuarioEnUsoAsync(string email, CancellationToken cancellationToken = default);
}
