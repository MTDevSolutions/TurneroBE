namespace Shared.DTOs.Whatsapp;

public class PlantillasMensajeDto
{
    public List<VariableMensajeDto> Variables { get; set; } = [];
    public List<PlantillaMensajeDto> Plantillas { get; set; } = [];
    public int MaxLargoTexto { get; set; }
}
