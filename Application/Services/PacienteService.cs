using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Paciente;
using Shared.DTOs.Paginacion;

namespace Application.Services;

public class PacienteService(IPacienteRepository repository, IUnitOfWork uow, TimeProvider reloj) : IPacienteService
{
    private const string MensajeCelularEnUso = "Ya hay un paciente con ese celular.";

    public async Task<PaginaDto<PacienteDto>> ListarAsync(PaginacionQuery query, string? buscar, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = query.Normalizado();
        var (items, total) = await repository.ListarAsync(page, pageSize, buscar, incluirInactivos, cancellationToken);
        return new PaginaDto<PacienteDto>
        {
            Items = items.Select(Mapear).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PacienteDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => Mapear(await ObtenerEntidadAsync(id, cancellationToken));

    public async Task<PacienteDto> CrearAsync(PacienteGuardarDto dto, CancellationToken cancellationToken = default)
    {
        var celular = NormalizarCelular(dto.Celular);
        if (await repository.CelularEnUsoAsync(celular, excluirId: null, cancellationToken))
            throw AppException.Conflicto(MensajeCelularEnUso);

        var paciente = new Paciente { FechaAlta = reloj.GetUtcNow().UtcDateTime, Activo = true };
        Aplicar(paciente, dto, celular);
        repository.Agregar(paciente);

        // El pre-check da el mensaje en el caso normal; el índice único cubre la carrera.
        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.PacienteCelular, MensajeCelularEnUso, cancellationToken);
        return Mapear(paciente);
    }

    public async Task<PacienteDto> ActualizarAsync(int id, PacienteGuardarDto dto, CancellationToken cancellationToken = default)
    {
        var paciente = await ObtenerEntidadAsync(id, cancellationToken);

        var celular = NormalizarCelular(dto.Celular);
        if (await repository.CelularEnUsoAsync(celular, excluirId: id, cancellationToken))
            throw AppException.Conflicto(MensajeCelularEnUso);

        Aplicar(paciente, dto, celular);
        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.PacienteCelular, MensajeCelularEnUso, cancellationToken);
        return Mapear(paciente);
    }

    public async Task<PacienteDto> CambiarActivoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var paciente = await ObtenerEntidadAsync(id, cancellationToken);
        paciente.Activo = activo;
        await uow.GuardarAsync(cancellationToken);
        return Mapear(paciente);
    }

    private async Task<Paciente> ObtenerEntidadAsync(int id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken) ?? throw AppException.RegistroNoExiste();

    // Un celular mal cargado manda los avisos a otra persona: se rechaza lo que no se puede normalizar.
    private static string NormalizarCelular(string celular)
        => TelefonoWhatsappHelper.Normalizar(celular)
           ?? throw AppException.Validacion("El celular no es válido. Cargalo con código de área, sin 0 ni 15 (ej. 11 2345-6789).");

    private static void Aplicar(Paciente paciente, PacienteGuardarDto dto, string celular)
    {
        paciente.Nombre = dto.Nombre.Trim();
        paciente.Apellido = dto.Apellido.Trim();
        paciente.Celular = celular;
        paciente.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        paciente.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
    }

    internal static PacienteDto Mapear(Paciente p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        Celular = p.Celular,
        Email = p.Email,
        Notas = p.Notas,
        Activo = p.Activo
    };
}
