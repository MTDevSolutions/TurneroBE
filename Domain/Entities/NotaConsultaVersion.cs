using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Versión anterior de una <see cref="NotaConsulta"/>: el texto tal como estaba antes de una edición.
/// Solo se inserta; un trigger de la base impide modificarla o borrarla.
/// </summary>
public class NotaConsultaVersion : ITenantEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public int NotaConsultaId { get; set; }

    /// <summary>Número de versión que tenía este texto.</summary>
    public int Version { get; set; }

    public string Texto { get; set; } = null!;

    /// <summary>Cuándo se había guardado este texto (alta o edición anterior), en UTC.</summary>
    public DateTime GuardadaEn { get; set; }

    /// <summary>Id del usuario que había guardado este texto.</summary>
    public int GuardadaPor { get; set; }
}
