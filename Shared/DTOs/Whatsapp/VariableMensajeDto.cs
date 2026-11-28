namespace Shared.DTOs.Whatsapp;

public class VariableMensajeDto
{
    /// <summary>Nombre sin llaves (ej. "paciente"); en el texto va como {paciente}.</summary>
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}
