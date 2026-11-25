using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Plataforma;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = "Superadmin")]
[Tags("Tenants (plataforma)")]
public class TenantController(ITenantAdminService tenantAdminService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Listar tenants", Description = "Todas las cuentas de profesionales de la plataforma (solo superadmin).")]
    public async Task<ActionResult<BaseResponse<List<TenantDto>>>> Listar(CancellationToken cancellationToken)
        => Ok(BaseResponse<List<TenantDto>>.Ok(await tenantAdminService.ListarAsync(cancellationToken)));

    [HttpPost]
    [SwaggerOperation(Summary = "Alta de cuenta", Description = "Crea el tenant + su primer consultorio + el profesional como admin + su configuración.")]
    public async Task<ActionResult<BaseResponse<TenantDto>>> Crear([FromBody] TenantCrearDto dto, CancellationToken cancellationToken)
    {
        var creado = await tenantAdminService.CrearAsync(dto, cancellationToken);
        return Ok(BaseResponse<TenantDto>.Ok(creado, $"Cuenta '{creado.Nombre}' creada.", true));
    }

    [HttpPost("{id:int}/suspender")]
    [SwaggerOperation(Summary = "Suspender tenant", Description = "Bloquea el acceso de la cuenta (sin borrar datos).")]
    public async Task<ActionResult<BaseResponse<string>>> Suspender(int id, CancellationToken cancellationToken)
    {
        await tenantAdminService.SuspenderAsync(id, cancellationToken);
        return Ok(BaseResponse<string>.Ok("suspendido", "Tenant suspendido.", true));
    }

    [HttpPost("{id:int}/reactivar")]
    [SwaggerOperation(Summary = "Reactivar tenant", Description = "Restaura el acceso de la cuenta.")]
    public async Task<ActionResult<BaseResponse<string>>> Reactivar(int id, CancellationToken cancellationToken)
    {
        await tenantAdminService.ReactivarAsync(id, cancellationToken);
        return Ok(BaseResponse<string>.Ok("activo", "Tenant reactivado.", true));
    }

    [HttpPost("{id:int}/pagos")]
    [SwaggerOperation(Summary = "Registrar cobro", Description = "Registra un pago de suscripción del tenant.")]
    public async Task<ActionResult<BaseResponse<PagoSuscripcionDto>>> RegistrarPago(int id, [FromBody] RegistrarPagoDto dto, CancellationToken cancellationToken)
    {
        var pago = await tenantAdminService.RegistrarPagoAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<PagoSuscripcionDto>.Ok(pago, "Cobro registrado.", true));
    }

}
