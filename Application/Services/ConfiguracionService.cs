using Application.Interfaces;
using Application.Exceptions;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Configuracion;

namespace Application.Services;

public class ConfiguracionService(IConfiguracionRepository repository, IUnitOfWork uow) : IConfiguracionService
{
    public async Task<ConfiguracionDto> ObtenerAsync(CancellationToken cancellationToken = default)
        => Mapear(await ObtenerOCrearAsync(cancellationToken));

    public async Task<ConfiguracionDto> ActualizarAsync(ConfiguracionActualizarDto dto, CancellationToken cancellationToken = default)
    {
        var configuracion = await ObtenerOCrearAsync(cancellationToken);

        configuracion.DuracionTurnoMinutos = dto.DuracionTurnoMinutos;
        configuracion.HorasRecordatorio = dto.HorasRecordatorio;
        configuracion.AvisosWhatsappActivos = dto.AvisosWhatsappActivos;

        await uow.GuardarAsync(cancellationToken);
        return Mapear(configuracion);
    }

    public async Task<ConfiguracionDto> ActualizarColorAsync(string color, CancellationToken cancellationToken = default)
    {
        var normalizado = color?.Trim().ToLowerInvariant();
        if (normalizado is null || !CteColores.Todos.Contains(normalizado))
            throw AppException.Validacion("Ese color no está entre las opciones.");

        var configuracion = await ObtenerOCrearAsync(cancellationToken);
        configuracion.ColorPrincipal = normalizado;

        await uow.GuardarAsync(cancellationToken);
        return Mapear(configuracion);
    }

    public async Task<string> ObtenerColorAsync(int tenantId, CancellationToken cancellationToken = default)
        => await repository.ObtenerColorAsync(tenantId, cancellationToken) ?? CteColores.PorDefecto;

    // El alta de la cuenta ya la crea; esto cubre cuentas creadas por otra vía (seed, datos viejos).
    private async Task<Configuracion> ObtenerOCrearAsync(CancellationToken cancellationToken)
    {
        var configuracion = await repository.ObtenerAsync(cancellationToken);
        if (configuracion is not null)
            return configuracion;

        configuracion = new Configuracion();
        repository.Agregar(configuracion);
        await uow.GuardarAsync(cancellationToken);
        return configuracion;
    }

    private static ConfiguracionDto Mapear(Configuracion c) => new()
    {
        DuracionTurnoMinutos = c.DuracionTurnoMinutos,
        HorasRecordatorio = c.HorasRecordatorio,
        AvisosWhatsappActivos = c.AvisosWhatsappActivos,
        ColorPrincipal = c.ColorPrincipal
    };
}
