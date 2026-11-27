using Application.Exceptions;
using Application.Interfaces;

namespace Tests.Fakes;

/// <summary>
/// Fake de IUnitOfWork. Cuenta los commits para poder assertar "guardó exactamente una vez"
/// y "no guardó nada" cuando la validación falla.
/// </summary>
public class UnitOfWorkFake : IUnitOfWork
{
    public int Guardados { get; private set; }
    public int Intentos { get; private set; }
    public int Descartes { get; private set; }

    public void DescartarPendientes() => Descartes++;

    /// <summary>Si es true, el próximo IntentarGuardarAsync devuelve false (simula conflicto).</summary>
    public bool ProximoIntentoFalla { get; set; }

    public Task GuardarAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // como SaveChangesAsync con un token cancelado
        Guardados++;
        return Task.CompletedTask;
    }

    public Task<bool> IntentarGuardarAsync(CancellationToken cancellationToken = default)
    {
        Intentos++;
        if (ProximoIntentoFalla)
        {
            ProximoIntentoFalla = false;
            return Task.FromResult(false);
        }
        Guardados++;
        return Task.FromResult(true);
    }

    /// <summary>Índice cuyo duplicado simula el próximo GuardarTraduciendoDuplicadoAsync.</summary>
    public string? ProximoDuplicadoEnIndice { get; set; }

    public Task GuardarTraduciendoDuplicadoAsync(string indiceUnico, string mensaje, CancellationToken cancellationToken = default)
    {
        if (ProximoDuplicadoEnIndice == indiceUnico)
        {
            ProximoDuplicadoEnIndice = null;
            throw AppException.Conflicto(mensaje);
        }
        return GuardarAsync(cancellationToken);
    }
}
