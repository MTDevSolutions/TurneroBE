using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>Worker de recordatorios: una pasada por minuto.</summary>
public class RecordatoriosWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider reloj,
    ILogger<RecordatoriosWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var encolados = 0;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var procesador = scope.ServiceProvider.GetRequiredService<ProcesadorRecordatorios>();
                encolados = await procesador.ProcesarAsync(reloj.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una pasada fallida (ej. DB caída) no debe tumbar el host.
                logger.LogError(ex, "Error en la pasada de recordatorios.");
            }

            // Lote lleno = probablemente hay más: se sigue sin dormir.
            if (encolados < ProcesadorRecordatorios.MaxPorPasada)
                await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
