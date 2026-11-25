using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;

namespace Tests.Fakes;

/// <summary>Fake in-memory de IMensajeWhatsappRepository. Reclamar respeta estado, vencimiento, máximo y lease.</summary>
public class MensajeWhatsappRepositoryFake : IMensajeWhatsappRepository
{
    private readonly List<MensajeWhatsapp> _mensajes = [];
    private long _proximoId = 1;

    public IReadOnlyList<MensajeWhatsapp> Mensajes => _mensajes;

    public void Agregar(MensajeWhatsapp mensaje)
    {
        mensaje.Id = _proximoId++;
        mensaje.TurnoId ??= mensaje.Turno?.Id;
        _mensajes.Add(mensaje);
    }

    public Task<List<MensajeWhatsapp>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_mensajes.Where(m => (m.TurnoId ?? m.Turno?.Id) == turnoId).OrderBy(m => m.Id).ToList());

    public Task<List<MensajeWhatsapp>> ReclamarPendientesAsync(DateTime ahora, int maximo, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var lote = _mensajes
            .Where(m => m.Estado == CteMensajesWhatsapp.Pendiente && m.ProximoIntentoEn <= ahora)
            .OrderBy(m => m.Id)
            .Take(maximo)
            .ToList();

        foreach (var m in lote)
            m.ProximoIntentoEn = ahora + lease;

        return Task.FromResult(lote);
    }
}
