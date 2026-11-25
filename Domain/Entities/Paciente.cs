using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Paciente de la cuenta. Entidad de dominio: se filtra por tenant.
/// No guarda datos clínicos: solo lo necesario para agendar y avisar por WhatsApp.
/// </summary>
public class Paciente : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;

    /// <summary>Celular normalizado para WhatsApp (549 + área + número). Único dentro de la cuenta:
    /// es la clave con la que se asocia una respuesta Sí/No al paciente.</summary>
    public string Celular { get; set; } = null!;

    public string? Email { get; set; }

    /// <summary>Notas administrativas (ej. "prefiere la tarde"). No es historia clínica.</summary>
    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}
