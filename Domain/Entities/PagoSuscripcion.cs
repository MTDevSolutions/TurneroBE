namespace Domain.Entities;

/// <summary>
/// Pago de un tenant por la suscripción al SaaS (entidad de plataforma).
/// Su columna TenantId es un FK normal a Tenant, NO el aislamiento multi-tenant:
/// por eso no implementa ITenantEntity y no se filtra.
/// </summary>
public class PagoSuscripcion
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public decimal Monto { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public DateTime FechaRegistro { get; set; }

    /// <summary>Id del superadmin que registró el pago.</summary>
    public int? RegistradoPor { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
