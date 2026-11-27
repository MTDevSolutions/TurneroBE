using System.ComponentModel.DataAnnotations;
using Shared.Constantes;

namespace Shared.DTOs.NotaConsulta;

/// <summary>Alta de una nota. Sin turno = nota suelta.</summary>
public class NotaConsultaCrearDto
{
    [Required, MaxLength(CteNotasConsulta.MaxTexto)]
    public string Texto { get; set; } = null!;

    /// <summary>Turno atendido del mismo paciente.</summary>
    public int? TurnoId { get; set; }
}

/// <summary>Edición dentro de la ventana. <see cref="Version"/> es la que se estaba viendo: si
/// otra pestaña guardó antes, la API responde 409.</summary>
public class NotaConsultaEditarDto
{
    [Required, MaxLength(CteNotasConsulta.MaxTexto)]
    public string Texto { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Version { get; set; }
}

public class NotaConsultaAnularDto
{
    [Required, MaxLength(CteNotasConsulta.MaxMotivoAnulacion)]
    public string Motivo { get; set; } = null!;
}

/// <summary>Un texto que tuvo la nota. La lista incluye la versión vigente.</summary>
public class NotaConsultaVersionDto
{
    public int Version { get; set; }
    public string Texto { get; set; } = null!;
    public DateTimeOffset GuardadaEn { get; set; }
    public bool Vigente { get; set; }
}
