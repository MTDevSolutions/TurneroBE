namespace Shared.DTOs.Sucursal;

public class SucursalDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Direccion { get; set; }
    public bool Activa { get; set; }
}
