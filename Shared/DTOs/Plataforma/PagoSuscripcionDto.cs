namespace Shared.DTOs.Plataforma;

public class PagoSuscripcionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public decimal Monto { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? RegistradoPor { get; set; }
}
