namespace Shared.DTOs.Plataforma;

public class TenantDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Especialidad { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string? Plan { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaProximoPago { get; set; }
    public DateTime? FechaFinGracia { get; set; }
    public int? WhatsappTelefonoId { get; set; }

    /// <summary>Versión de términos aceptada; null si la cuenta todavía no los aceptó.</summary>
    public string? TerminosVersionAceptada { get; set; }
    public DateTime? TerminosAceptadosEn { get; set; }
}
