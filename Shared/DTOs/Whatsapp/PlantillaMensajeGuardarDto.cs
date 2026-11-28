using System.ComponentModel.DataAnnotations;
using Shared.Constantes;

namespace Shared.DTOs.Whatsapp;

public class PlantillaMensajeGuardarDto
{
    [Required, MaxLength(CtePlantillasMensaje.MaxLargoTexto)]
    public string Texto { get; set; } = null!;
}
