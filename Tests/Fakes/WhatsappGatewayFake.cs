using Application.Interfaces;

namespace Tests.Fakes;

/// <summary>Fake de IWhatsappGateway: respuestas de envío encoladas y registro de lo enviado.</summary>
public class WhatsappGatewayFake : IWhatsappGateway
{
    public bool ConfiguradoFake { get; set; } = true;
    public bool Configurado => ConfiguradoFake;

    /// <summary>Respuestas en orden; vacía = 200 con MessageId "m-1", "m-2", ...</summary>
    public Queue<RespuestaEnvioGateway> Respuestas { get; } = new();
    public List<(int TelefonoId, string Destino, string Texto)> Envios { get; } = [];

    public SesionWhatsapp? Sesion { get; set; }
    public bool? Existe { get; set; } = true;

    /// <summary>Se ejecuta en cada envío (ej. cancelar el token justo después de que el gateway mandó).</summary>
    public Action? AlEnviar { get; set; }

    public Task<RespuestaEnvioGateway> EnviarTextoAsync(int telefonoId, string destino, string texto, CancellationToken cancellationToken = default)
    {
        Envios.Add((telefonoId, destino, texto));
        AlEnviar?.Invoke();
        return Task.FromResult(Respuestas.Count > 0 ? Respuestas.Dequeue() : new RespuestaEnvioGateway(200, $"m-{Envios.Count}", null));
    }

    public Task<SesionWhatsapp?> ObtenerSesionAsync(int telefonoId, CancellationToken cancellationToken = default)
        => Task.FromResult(Sesion);

    public Task<bool?> ExisteTelefonoAsync(int telefonoId, CancellationToken cancellationToken = default)
        => Task.FromResult(Existe);
}
