using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Constantes;

namespace Application.Services;

/// <summary>
/// Worker del outbox de WhatsApp: cada pocos segundos procesa una pasada. Si el gateway no está
/// configurado (dev sin gateway), no arranca el ciclo y los mensajes quedan pendientes.
/// </summary>
public class EnvioWhatsappWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EnvioWhatsappWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            if (!scope.ServiceProvider.GetRequiredService<IWhatsappGateway>().Configurado)
            {
                logger.LogInformation("WhatsappGateway sin configurar: el envío de WhatsApp queda deshabilitado.");
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var procesados = 0;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var procesador = scope.ServiceProvider.GetRequiredService<ProcesadorEnvioWhatsapp>();
                procesados = await procesador.ProcesarAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // No propagar: una pasada fallida (ej. DB caída) no debe tumbar el host.
                logger.LogError(ex, "Error en la pasada de envío de WhatsApp.");
            }

            // Lote lleno = probablemente hay más esperando: se sigue sin dormir.
            if (procesados < CteMensajesWhatsapp.MaxPorPasada)
                await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
