namespace Shared.DTOs.Configuracion;

public class ConfiguracionDto
{
    public int DuracionTurnoMinutos { get; set; }
    public int HorasRecordatorio { get; set; }
    public bool AvisosWhatsappActivos { get; set; }

    /// <summary>Paleta del color principal (rosa | lavanda | coral | salvia | celeste).</summary>
    public string ColorPrincipal { get; set; } = null!;
}
