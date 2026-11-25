using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Plataforma;

public class RegistrarPagoDto
{
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    [Required]
    public DateTime PeriodoDesde { get; set; }

    [Required]
    public DateTime PeriodoHasta { get; set; }
}
