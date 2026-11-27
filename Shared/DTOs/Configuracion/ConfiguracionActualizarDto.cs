using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Configuracion;

public class ConfiguracionActualizarDto
{
    /// <summary>Entre 5 minutos y 8 horas.</summary>
    [Range(5, 480)]
    public int DuracionTurnoMinutos { get; set; }

    /// <summary>0 = sin recordatorio; hasta 7 días antes.</summary>
    [Range(0, 168)]
    public int HorasRecordatorio { get; set; }

    public bool AvisosWhatsappActivos { get; set; }
}
