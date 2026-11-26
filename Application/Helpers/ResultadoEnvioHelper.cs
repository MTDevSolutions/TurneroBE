namespace Application.Helpers;

public enum TipoResultadoEnvio { Enviado, Transitorio, Permanente }

/// <summary>
/// Qué hacer con un mensaje según la respuesta del gateway. Transitorio = reintentar con backoff;
/// permanente = no tiene arreglo reintentando (número inválido, teléfono inexistente).
/// </summary>
public static class ResultadoEnvioHelper
{
    public static TipoResultadoEnvio Clasificar(int? statusCode) => statusCode switch
    {
        >= 200 and < 300 => TipoResultadoEnvio.Enviado,
        null => TipoResultadoEnvio.Transitorio,
        // 401 (API key del turnero) y 403 (empresa inactiva en el gateway) se arreglan en la config
        // de la plataforma: el mensaje no tiene la culpa.
        401 or 403 or 408 or 429 => TipoResultadoEnvio.Transitorio,
        >= 500 => TipoResultadoEnvio.Transitorio,
        _ => TipoResultadoEnvio.Permanente,
    };
}
