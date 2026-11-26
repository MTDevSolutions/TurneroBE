using Domain.Entities;

namespace Application.Interfaces;

public interface IMensajeEntranteRepository
{
    /// <summary>True si ese mensaje del gateway ya se procesó (el gateway reintenta el webhook).</summary>
    Task<bool> ExisteAsync(long gatewayMensajeId, CancellationToken cancellationToken = default);

    /// <summary>El TenantId lo pone quien llama (el webhook no tiene cuenta en contexto).</summary>
    void Agregar(MensajeEntrante mensaje);

    /// <summary>Respuestas de un turno de la cuenta del request, la más vieja primero.</summary>
    Task<List<MensajeEntrante>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default);
}
