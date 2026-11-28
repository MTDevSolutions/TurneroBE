using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.UseCases.Security_Context;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;

namespace Application.Services;

/// <summary>
/// Textos de los mensajes de WhatsApp de la cuenta: lo reescrito pisa al de fábrica y restablecer
/// vuelve a él. Lo que ya está en la cola no cambia: el texto se congela al encolarse.
/// </summary>
public class PlantillaMensajeService(
    IPlantillaMensajeRepository plantillas,
    ITenantContext tenantContext,
    ITenantRepository tenants,
    IUnitOfWork uow,
    TimeProvider reloj) : IPlantillaMensajeService
{
    public async Task<PlantillasMensajeDto> ListarAsync(CancellationToken cancellationToken = default)
    {
        var propias = (await plantillas.ListarAsync(cancellationToken)).ToDictionary(p => p.Clave);
        return new PlantillasMensajeDto
        {
            Variables = PlantillaMensajeRenderer.Variables
                .Select(v => new VariableMensajeDto { Nombre = v.Nombre, Descripcion = v.Descripcion }).ToList(),
            Plantillas = CtePlantillasMensaje.Todas.Select(d => Mapear(d, propias.GetValueOrDefault(d.Clave))).ToList(),
            MaxLargoTexto = CtePlantillasMensaje.MaxLargoTexto,
        };
    }

    public async Task<PlantillaMensajeDto> GuardarAsync(string clave, PlantillaMensajeGuardarDto dto, CancellationToken cancellationToken = default)
    {
        ExigirAdmin();
        var definicion = CtePlantillasMensaje.Buscar(clave) ?? throw AppException.RegistroNoExiste();
        var texto = Validar(dto.Texto);

        var plantilla = await plantillas.ObtenerAsync(clave, cancellationToken);

        // Guardar el mismo texto de fábrica es restablecer: no deja una fila que después no se actualice.
        if (texto == definicion.TextoPorDefecto)
        {
            if (plantilla is not null)
            {
                plantillas.Eliminar(plantilla);
                await uow.GuardarAsync(cancellationToken);
            }
            return Mapear(definicion, null);
        }

        if (plantilla is null)
        {
            plantilla = new PlantillaMensaje { Clave = clave };
            plantillas.Agregar(plantilla);
        }
        plantilla.Texto = texto;
        plantilla.ActualizadoEn = reloj.GetUtcNow().UtcDateTime;

        // Dos pestañas guardando el mismo mensaje nuevo a la vez: la segunda choca con el índice único.
        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.PlantillaMensajeClave,
            "El mensaje se modificó en otra pestaña. Volvé a intentar.", cancellationToken);
        return Mapear(definicion, plantilla);
    }

    public async Task<PlantillaMensajeDto> RestablecerAsync(string clave, CancellationToken cancellationToken = default)
    {
        ExigirAdmin();
        var definicion = CtePlantillasMensaje.Buscar(clave) ?? throw AppException.RegistroNoExiste();

        var plantilla = await plantillas.ObtenerAsync(clave, cancellationToken);
        if (plantilla is not null)
        {
            plantillas.Eliminar(plantilla);
            await uow.GuardarAsync(cancellationToken);
        }
        return Mapear(definicion, null);
    }

    public async Task<VistaPreviaMensajeResultadoDto> VistaPreviaAsync(VistaPreviaMensajeDto dto, CancellationToken cancellationToken = default)
    {
        var definicion = CtePlantillasMensaje.Buscar(dto.Clave) ?? throw AppException.RegistroNoExiste();
        var texto = Validar(dto.Texto);

        var tenantId = tenantContext.TenantId ?? throw AppException.SinPermiso("La sesión no pertenece a una cuenta.");
        var profesional = (await tenants.WhatsappInfoAsync(tenantId, cancellationToken))?.Nombre;
        var ejemplo = string.IsNullOrWhiteSpace(profesional)
            ? VariablesMensaje.Ejemplo
            : VariablesMensaje.Ejemplo with { Profesional = profesional };

        return new VistaPreviaMensajeResultadoDto { Texto = PlantillaMensajeRenderer.Armar(definicion, texto, ejemplo) };
    }

    /// <summary>Texto recortado; 400 si queda vacío o usa variables que no existen (ej. "{nombre}").</summary>
    private static string Validar(string? textoCrudo)
    {
        var texto = (textoCrudo ?? "").Trim();
        if (texto.Length == 0)
            throw AppException.Validacion("El texto del mensaje es obligatorio.");
        if (texto.Length > CtePlantillasMensaje.MaxLargoTexto)
            throw AppException.Validacion($"El mensaje puede tener hasta {CtePlantillasMensaje.MaxLargoTexto} caracteres.");

        var desconocidas = PlantillaMensajeRenderer.VariablesDesconocidas(texto);
        if (desconocidas.Count > 0)
            throw AppException.Validacion(
                $"Variables desconocidas: {string.Join(", ", desconocidas.Select(v => "{" + v + "}"))}.");
        return texto;
    }

    private void ExigirAdmin()
    {
        if (tenantContext.Rol != CteRoles.Admin)
            throw AppException.SinPermiso("Solo el titular de la cuenta puede cambiar los mensajes.");
    }

    private static PlantillaMensajeDto Mapear(CtePlantillasMensaje.Definicion d, PlantillaMensaje? propia) => new()
    {
        Clave = d.Clave,
        Nombre = d.Nombre,
        Descripcion = d.Descripcion,
        Destinatario = d.Destinatario,
        Texto = propia?.Texto ?? d.TextoPorDefecto,
        TextoPorDefecto = d.TextoPorDefecto,
        Pie = d.Pie,
        EsDefault = propia is null,
    };
}
