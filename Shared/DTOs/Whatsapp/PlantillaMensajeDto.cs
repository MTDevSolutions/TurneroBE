namespace Shared.DTOs.Whatsapp;

public class PlantillaMensajeDto
{
    public string Clave { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;

    /// <summary>paciente | profesional</summary>
    public string Destinatario { get; set; } = null!;

    /// <summary>Texto vigente: el de la cuenta o el de fábrica.</summary>
    public string Texto { get; set; } = null!;

    public string TextoPorDefecto { get; set; } = null!;

    /// <summary>Línea fija que se agrega al final y no se edita (qué responder). Null si no tiene.</summary>
    public string? Pie { get; set; }

    /// <summary>true = la cuenta no lo cambió, es el de fábrica.</summary>
    public bool EsDefault { get; set; }
}
