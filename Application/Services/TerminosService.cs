using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Security_Context;
using Shared.Constantes;
using Shared.DTOs.Terminos;

namespace Application.Services;

/// <summary>
/// Aceptación de los términos de uso. Los acepta la cuenta (el tenant), y solo puede hacerlo su
/// admin: es el profesional quien asume el riesgo del canal no oficial.
/// </summary>
public class TerminosService(
    ITenantRepository tenantRepository,
    ITenantContext tenantContext,
    ISecurityContextService currentUser,
    ITenantEstadoService tenantEstadoService,
    IUnitOfWork uow) : ITerminosService
{
    public async Task<TerminosDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(TenantIdActual(), cancellationToken)
            ?? throw AppException.RegistroNoExiste();

        return Vigentes(tenant.TerminosVersionAceptada == CteTerminos.VersionVigente);
    }

    public async Task<TerminosDto> AceptarAsync(TerminosAceptarDto dto, CancellationToken cancellationToken = default)
    {
        if (tenantContext.Rol != CteRoles.Admin)
            throw AppException.SinPermiso("Solo el titular de la cuenta puede aceptar los términos.");

        if (dto.Version != CteTerminos.VersionVigente)
            throw AppException.Conflicto("Los términos cambiaron. Volvé a leerlos antes de aceptarlos.");

        var tenantId = TenantIdActual();
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw AppException.RegistroNoExiste();

        tenant.TerminosVersionAceptada = CteTerminos.VersionVigente;
        tenant.TerminosAceptadosEn = DateTime.UtcNow;
        tenant.TerminosAceptadosPor = currentUser.Id;
        await uow.GuardarAsync(cancellationToken);

        // Sin esto, el middleware seguiría bloqueando hasta que venza el caché.
        tenantEstadoService.InvalidarCache(tenantId);

        return Vigentes(aceptados: true);
    }

    private int TenantIdActual()
        => tenantContext.TenantId ?? throw AppException.SinPermiso("La sesión no pertenece a una cuenta.");

    private static TerminosDto Vigentes(bool aceptados) => new()
    {
        Version = CteTerminos.VersionVigente,
        Texto = CteTerminos.Texto,
        Aceptados = aceptados
    };
}
