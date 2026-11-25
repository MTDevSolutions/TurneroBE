namespace Application.UseCases.Whatsapp;

/// <summary>Lo que el envío necesita saber de la cuenta (entidad de plataforma).</summary>
public sealed record TenantWhatsappInfo(
    int TenantId, string Nombre, string Estado, int? WhatsappTelefonoId, string? TerminosVersionAceptada);
