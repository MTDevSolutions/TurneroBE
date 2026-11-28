namespace Domain.Entities;

/// <summary>
/// Cuenta cliente del SaaS: un profesional (o su consultorio). Entidad de plataforma: NO lleva
/// tenant_id ni implementa ITenantEntity (queda fuera del global query filter).
/// </summary>
public class Tenant
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;

    /// <summary>Especialidad del profesional (ver CteEspecialidades).</summary>
    public string Especialidad { get; set; } = null!;

    /// <summary>activo | suspendido | cancelado</summary>
    public string Estado { get; set; } = "activo";

    public string? Plan { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaProximoPago { get; set; }

    /// <summary>Hasta cuándo puede seguir operando impago antes de suspenderse.</summary>
    public DateTime? FechaFinGracia { get; set; }

    /// <summary>Id del Telefono en Wabix Gateway por el que salen los avisos. Lo carga el superadmin.
    /// Null = el profesional todavía no vinculó su número.</summary>
    public int? WhatsappTelefonoId { get; set; }

    /// <summary>Último estado de la sesión de WhatsApp que avisó el gateway (CteSesionWhatsapp).
    /// Null = todavía no llegó ningún aviso para el teléfono actual.</summary>
    public string? WhatsappEstadoSesion { get; set; }

    /// <summary>Desde cuándo la sesión está en <see cref="WhatsappEstadoSesion"/> (UTC).</summary>
    public DateTime? WhatsappEstadoDesde { get; set; }

    /// <summary>Id del último aviso de sesión aplicado: uno con id menor llegó tarde y se ignora.</summary>
    public long? WhatsappUltimoAvisoId { get; set; }

    /// <summary>Versión de los términos de uso que aceptó la cuenta. Null = todavía no aceptó.
    /// Si no coincide con <c>CteTerminos.VersionVigente</c>, la API pide aceptarlos de nuevo.</summary>
    public string? TerminosVersionAceptada { get; set; }

    public DateTime? TerminosAceptadosEn { get; set; }

    /// <summary>Usuario que aceptó los términos (el admin de la cuenta).</summary>
    public int? TerminosAceptadosPor { get; set; }

    public ICollection<PagoSuscripcion> Pagos { get; set; } = new List<PagoSuscripcion>();
}
