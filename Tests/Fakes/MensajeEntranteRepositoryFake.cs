using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

public class MensajeEntranteRepositoryFake : IMensajeEntranteRepository
{
    public List<MensajeEntrante> Mensajes { get; } = [];

    public Task<bool> ExisteAsync(long gatewayMensajeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Mensajes.Any(m => m.GatewayMensajeId == gatewayMensajeId));

    public void Agregar(MensajeEntrante mensaje)
    {
        mensaje.Id = Mensajes.Count + 1;
        Mensajes.Add(mensaje);
    }

    public Task<List<MensajeEntrante>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default)
        => Task.FromResult(Mensajes.Where(m => m.TurnoId == turnoId).OrderBy(m => m.Id).ToList());
}
