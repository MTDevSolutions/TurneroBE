using Application.Interfaces;
using Application.UseCases.Whatsapp;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>Fake in-memory de ITenantRepository.</summary>
public class TenantRepositoryFake : ITenantRepository
{
    private readonly List<Tenant> _tenants = [];

    public HashSet<string> EmailsEnUso { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<PagoSuscripcion> Pagos { get; } = [];

    /// <summary>Lo último que se mandó a CrearConAdminAsync, para assertar qué se armó.</summary>
    public (Tenant Tenant, Sucursal Sucursal, Usuario Admin, Configuracion Configuracion)? UltimoAlta { get; private set; }

    public Tenant Agregar(Tenant tenant)
    {
        _tenants.Add(tenant);
        return tenant;
    }

    public Task<Tenant> CrearConAdminAsync(Tenant tenant, Sucursal sucursal, Usuario admin, Configuracion configuracion, CancellationToken cancellationToken = default)
    {
        tenant.Id = _tenants.Count + 1;
        sucursal.TenantId = admin.TenantId = configuracion.TenantId = tenant.Id;
        _tenants.Add(tenant);
        UltimoAlta = (tenant, sucursal, admin, configuracion);
        return Task.FromResult(tenant);
    }

    public Task<List<Tenant>> ListarAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_tenants.ToList());

    public Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id));

    public void AgregarPago(PagoSuscripcion pago) => Pagos.Add(pago);

    public Task<bool> EmailUsuarioEnUsoAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(EmailsEnUso.Contains(email));

    public Task<TenantWhatsappInfo?> WhatsappInfoAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tenants.Where(t => t.Id == tenantId).Select(Info).FirstOrDefault());

    public Task<List<TenantWhatsappInfo>> WhatsappInfoAsync(IReadOnlyCollection<int> tenantIds, CancellationToken cancellationToken = default)
        => Task.FromResult(_tenants.Where(t => tenantIds.Contains(t.Id)).Select(Info).ToList());

    private static TenantWhatsappInfo Info(Tenant t)
        => new(t.Id, t.Nombre, t.Estado, t.WhatsappTelefonoId, t.TerminosVersionAceptada);
}
