using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constantes;
using Shared.Settings;

namespace Infrastructure.Services;

/// <summary>
/// HttpClient tipado contra Wabix Gateway. Nunca deja escapar una falla del gateway ni de configuración: el worker
/// decide qué hacer con cada resultado, y las pantallas muestran "desconectado". Solo propaga si el llamador cancela.
/// </summary>
public class WhatsappGatewayClient(
    HttpClient http,
    IOptions<WhatsappGatewaySettings> opciones,
    ILogger<WhatsappGatewayClient> logger) : IWhatsappGateway
{
    private const string HeaderApiKey = "X-Api-Key";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly WhatsappGatewaySettings _cfg = opciones.Value;

    public bool Configurado => !string.IsNullOrWhiteSpace(_cfg.BaseUrl) && !string.IsNullOrWhiteSpace(_cfg.ApiKey);

    public async Task<RespuestaEnvioGateway> EnviarTextoAsync(int telefonoId, string destino, string texto, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = Request(HttpMethod.Post, "api/mensajes/texto");
            request.Content = JsonContent.Create(new { destino, mensaje = texto, desdeTelefonoId = telefonoId }, options: Json);

            using var response = await http.SendAsync(request, cancellationToken);
            var cuerpo = await LeerAsync<MensajeEnviado>(response, cancellationToken);
            var status = (int)response.StatusCode;

            return response.IsSuccessStatusCode
                ? new RespuestaEnvioGateway(status, cuerpo?.Data?.MessageId, null)
                : new RespuestaEnvioGateway(status, null, cuerpo?.Message ?? $"El gateway respondió HTTP {status}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFallaDeRed(ex))
        {
            logger.LogWarning(ex, "No se pudo contactar al gateway de WhatsApp para enviar.");
            return new RespuestaEnvioGateway(null, null, "No se pudo contactar al gateway de WhatsApp.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en la configuración de WhatsappGateway (BaseUrl o ApiKey inválida).");
            return new RespuestaEnvioGateway(null, null, "Error de configuración del gateway de WhatsApp.");
        }
    }

    public async Task<SesionWhatsapp?> ObtenerSesionAsync(int telefonoId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = Request(HttpMethod.Get, $"api/telefonos/{telefonoId}/sesion");
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var data = (await LeerAsync<Sesion>(response, cancellationToken))?.Data;
            return new SesionWhatsapp(CteSesionWhatsapp.Normalizar(data?.Estado), data?.Numero, data?.Qr);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFallaDeRed(ex))
        {
            logger.LogWarning(ex, "No se pudo consultar la sesión de WhatsApp del teléfono {TelefonoId}.", telefonoId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en la configuración de WhatsappGateway (BaseUrl o ApiKey inválida) al consultar sesión de teléfono {TelefonoId}.", telefonoId);
            return null;
        }
    }

    public async Task<bool?> ExisteTelefonoAsync(int telefonoId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = Request(HttpMethod.Get, $"api/telefonos/{telefonoId}");
            using var response = await http.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode == HttpStatusCode.NotFound) return false;
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFallaDeRed(ex))
        {
            logger.LogWarning(ex, "No se pudo consultar el teléfono {TelefonoId} en el gateway.", telefonoId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en la configuración de WhatsappGateway (BaseUrl o ApiKey inválida) al consultar teléfono {TelefonoId}.", telefonoId);
            return null;
        }
    }

    private HttpRequestMessage Request(HttpMethod metodo, string ruta)
    {
        var request = new HttpRequestMessage(metodo, new Uri(new Uri(_cfg.BaseUrl.TrimEnd('/') + "/"), ruta));
        request.Headers.Add(HeaderApiKey, _cfg.ApiKey);
        return request;
    }

    // Timeout del HttpClient o error de conexión.
    private static bool EsFallaDeRed(Exception ex)
        => ex is HttpRequestException or TaskCanceledException;

    // Un cuerpo vacío o que no es JSON no invalida el status: se lee lo que se pueda.
    private static async Task<Respuesta<T>?> LeerAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<Respuesta<T>>(Json, ct);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed class Respuesta<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private sealed class MensajeEnviado
    {
        public string? MessageId { get; set; }
    }

    private sealed class Sesion
    {
        public string? Estado { get; set; }
        public string? Numero { get; set; }
        public string? Qr { get; set; }
    }
}
