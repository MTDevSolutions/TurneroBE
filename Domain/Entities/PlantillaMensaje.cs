using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Texto de un mensaje de WhatsApp reescrito por la cuenta. Entidad de dominio: se filtra por tenant.
/// Solo existe la fila si la cuenta cambió el de fábrica (ver CtePlantillasMensaje); borrarla vuelve
/// al original. Igual que PlantillaMensaje de CellFix.
/// </summary>
public class PlantillaMensaje : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Una clave de CtePlantillasMensaje (ej. "aviso").</summary>
    public string Clave { get; set; } = null!;

    public string Texto { get; set; } = null!;

    public DateTime ActualizadoEn { get; set; }
}
