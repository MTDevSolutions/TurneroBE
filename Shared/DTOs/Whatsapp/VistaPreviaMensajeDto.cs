using System.ComponentModel.DataAnnotations;
using Shared.Constantes;

namespace Shared.DTOs.Whatsapp;

public class VistaPreviaMensajeDto
{
    /// <summary>Qué mensaje es: define el pie fijo que se agrega.</summary>
    [Required, MaxLength(40)]
    public string Clave { get; set; } = null!;

    [Required, MaxLength(CtePlantillasMensaje.MaxLargoTexto)]
    public string Texto { get; set; } = null!;
}

public class VistaPreviaMensajeResultadoDto
{
    public string Texto { get; set; } = null!;
}
