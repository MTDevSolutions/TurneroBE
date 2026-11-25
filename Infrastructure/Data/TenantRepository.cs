using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Whatsapp;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TenantRepository(TurneroContext context) : ITenantRepository
{
    public async Task<Tenant> CrearConAdminAsync(
        Tenant tenant, Sucursal sucursal, Usuario admin, Configuracion configuracion,
        CancellationToken cancellationToken = default)
    {
        // Única operación del sistema que maneja su propia transacción y sus propios SaveChanges:
        // necesita el Id generado del tenant para setear los FK del resto, así que no se puede
        // resolver con un único commit del IUnitOfWork. El superadmin no tiene tenant en contexto
        // (StampTenant no autocompleta), así que los FK se asignan explícitamente.
        // Transacción: si el admin falla (ej. email duplicado), no queda nada huérfano.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        sucursal.TenantId = tenant.Id;
        context.Sucursales.Add(sucursal);
        configuracion.TenantId = tenant.Id;
        context.Configuraciones.Add(configuracion);
        await context.SaveChangesAsync(cancellationToken);

        admin.TenantId = tenant.Id;
        admin.SucursalId = sucursal.Id;
        context.Usuarios.Add(admin);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Carrera TOCTOU: otro alta con el mismo email commiteó entre el pre-check y este save.
            await transaction.RollbackAsync(cancellationToken);
            throw AppException.Conflicto($"El email '{admin.Email}' ya está en uso.");
        }

        await transaction.CommitAsync(cancellationToken);
        return tenant;
    }

    public async Task<List<Tenant>> ListarAsync(CancellationToken cancellationToken = default)
        => await context.Tenants.OrderBy(t => t.Nombre).ToListAsync(cancellationToken);

    public async Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void AgregarPago(PagoSuscripcion pago)
        => context.PagosSuscripcion.Add(pago);

    public async Task<bool> EmailUsuarioEnUsoAsync(string email, CancellationToken cancellationToken = default)
        => await context.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<TenantWhatsappInfo?> WhatsappInfoAsync(int tenantId, CancellationToken cancellationToken = default)
        => await context.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantWhatsappInfo(t.Id, t.Nombre, t.Estado, t.WhatsappTelefonoId, t.TerminosVersionAceptada))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<TenantWhatsappInfo>> WhatsappInfoAsync(IReadOnlyCollection<int> tenantIds, CancellationToken cancellationToken = default)
        => await context.Tenants.AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new TenantWhatsappInfo(t.Id, t.Nombre, t.Estado, t.WhatsappTelefonoId, t.TerminosVersionAceptada))
            .ToListAsync(cancellationToken);
}
