using Application.UseCases.Security_Context;
using Domain.Entities.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Data;

/// <summary>
/// DbContext de la aplicación (PostgreSQL vía Npgsql).
/// Aplica el aislamiento multi-tenant: global query filter por tenant en toda
/// entidad que implemente <see cref="ITenantEntity"/> y autocompletado de TenantId en inserts.
/// Las entidades de plataforma quedan fuera del filtro (no implementan la interfaz).
/// </summary>
public partial class TurneroContext : DbContext
{
    private readonly int _tenantId;

    public TurneroContext()
    {
    }

    public TurneroContext(DbContextOptions<TurneroContext> options, ITenantContext tenantContext)
        : base(options)
    {
        // 0 = sin tenant (superadmin / diseño). EF re-evalúa este valor por instancia de contexto.
        _tenantId = tenantContext.TenantId ?? 0;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        AplicarFiltroDeTenant(modelBuilder);
        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>Aplica el global query filter por tenant a toda entidad ITenantEntity.</summary>
    private void AplicarFiltroDeTenant(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filter = (LambdaExpression)typeof(TurneroContext)
                    .GetMethod(nameof(BuildTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, null)!;

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    // e => e.TenantId == _tenantId  (el valor se re-evalúa por consulta desde la instancia actual)
    private LambdaExpression BuildTenantFilter<TEntity>() where TEntity : class, ITenantEntity
    {
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == _tenantId;
        return filter;
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Autocompleta TenantId en entidades nuevas cuando hay un tenant en contexto.</summary>
    private void StampTenant()
    {
        if (_tenantId == 0)
            return; // superadmin / sin contexto: no se autocompleta

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
                entry.Entity.TenantId = _tenantId;
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
