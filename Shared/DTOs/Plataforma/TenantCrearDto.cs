using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Plataforma;

/// <summary>Alta de una cuenta: tenant + su primer consultorio + el profesional como admin.</summary>
public class TenantCrearDto
{
    /// <summary>Nombre de la cuenta (ej. "Lic. Ana Pérez" o el nombre del consultorio).</summary>
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = null!;

    /// <summary>fonoaudiologia | psicologia | medicina_general | otra</summary>
    [Required, MaxLength(30)]
    public string Especialidad { get; set; } = null!;

    public string? Plan { get; set; }

    [Required, MaxLength(150)]
    public string ConsultorioNombre { get; set; } = null!;

    [MaxLength(250)]
    public string? ConsultorioDireccion { get; set; }

    [Required, MaxLength(150)]
    public string AdminNombre { get; set; } = null!;

    [Required, EmailAddress, MaxLength(255)]
    public string AdminEmail { get; set; } = null!;

    [Required, MinLength(8)]
    public string AdminPassword { get; set; } = null!;

    /// <summary>Celular personal del profesional (texto libre, se normaliza a formato WhatsApp AR).</summary>
    [Required, MaxLength(30)]
    public string AdminCelular { get; set; } = null!;
}
