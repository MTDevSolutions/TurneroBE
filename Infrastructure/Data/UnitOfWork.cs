using Application.Exceptions;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Data;

public class UnitOfWork(TurneroContext context) : IUnitOfWork
{
    public async Task GuardarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro request modificó la entidad entre la lectura y este save (xmin cambió).
            throw AppException.Conflicto("El registro fue modificado por otro usuario. Volvé a intentar.");
        }
    }

    public async Task<bool> IntentarGuardarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // El intento falló: el scope queda limpio. Se desenganchan TODAS las entradas
            // pendientes, no solo las del comando que chocó, para no envenenar los saves
            // siguientes del mismo ciclo; el próximo ciclo reevalúa desde cero.
            DescartarPendientes();
            return false;
        }
    }

    public void DescartarPendientes()
    {
        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                     .ToList())
            entry.State = EntityState.Detached;
    }

    public async Task GuardarTraduciendoDuplicadoAsync(string indiceUnico, string mensaje, CancellationToken cancellationToken = default)
    {
        try
        {
            await GuardarAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsViolacionDe(ex, indiceUnico))
        {
            throw AppException.Conflicto(mensaje);
        }
    }

    // Unique y exclusion (ej. turnos superpuestos) se traducen igual: los dos son "ya existe algo
    // que choca con esto" sobre una restricción con nombre.
    // Se exige el nombre del índice además del código: una violación de FK, un NOT NULL o el
    // duplicado de OTRO índice único de la misma tabla no son "ya existe X" y no deben salir
    // con ese mensaje. Todo lo que no matchea se propaga y termina en el 500 que corresponde.
    private static bool EsViolacionDe(DbUpdateException ex, string indiceUnico)
        => ex.InnerException is PostgresException pg
           && pg.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ExclusionViolation
           && pg.ConstraintName == indiceUnico;
}
