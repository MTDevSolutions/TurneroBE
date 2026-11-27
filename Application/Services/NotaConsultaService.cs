using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Security_Context;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.NotaConsulta;

namespace Application.Services;

/// <summary>
/// Notas de consulta del profesional sobre sus pacientes. Reglas:
/// solo las ve y escribe el rol admin (el profesional); una nota con turno exige que sea un turno
/// atendido del mismo paciente; se edita solo dentro de <see cref="CteNotasConsulta.HorasEdicion"/>
/// horas guardando la versión anterior; después solo se anula (con motivo) y nunca se borra.
/// </summary>
public class NotaConsultaService(
    INotaConsultaRepository notas,
    IPacienteRepository pacientes,
    ITurnoRepository turnos,
    ITenantContext tenant,
    ISecurityContextService usuario,
    IUnitOfWork uow,
    TimeProvider reloj) : INotaConsultaService
{
    private static readonly TimeSpan VentanaEdicion = TimeSpan.FromHours(CteNotasConsulta.HorasEdicion);

    public async Task<List<NotaConsultaDto>> ListarAsync(int pacienteId, bool incluirAnuladas, CancellationToken cancellationToken = default)
    {
        ExigirProfesional();
        await ObtenerPacienteAsync(pacienteId, cancellationToken);

        var lista = await notas.ListarPorPacienteAsync(pacienteId, incluirAnuladas, cancellationToken);
        var ahora = Ahora();
        return lista.Select(n => Mapear(n, ahora)).ToList();
    }

    public async Task<NotaConsultaDto> CrearAsync(int pacienteId, NotaConsultaCrearDto dto, CancellationToken cancellationToken = default)
    {
        ExigirProfesional();
        var paciente = await ObtenerPacienteAsync(pacienteId, cancellationToken);
        var texto = ValidarTexto(dto.Texto);

        Turno? turno = null;
        if (dto.TurnoId is int turnoId)
        {
            turno = await turnos.GetByIdAsync(turnoId, cancellationToken);
            if (turno is null || turno.PacienteId != paciente.Id)
                throw AppException.Validacion("El turno no es de este paciente.");
            if (turno.Estado != CteEstadosTurno.Atendido)
                throw AppException.Validacion("Solo se puede escribir la nota de un turno atendido.");
        }

        var nota = new NotaConsulta
        {
            PacienteId = paciente.Id,
            TurnoId = turno?.Id,
            Turno = turno,
            Texto = texto,
            Version = 1,
            CreadaEn = Ahora(),
            CreadaPor = usuario.Id
        };
        notas.Agregar(nota);

        await uow.GuardarAsync(cancellationToken);
        return Mapear(nota, nota.CreadaEn);
    }

    public async Task<NotaConsultaDto> EditarAsync(int id, NotaConsultaEditarDto dto, CancellationToken cancellationToken = default)
    {
        ExigirProfesional();
        var nota = await ObtenerNotaAsync(id, cancellationToken);
        var ahora = Ahora();

        if (nota.Anulada)
            throw AppException.Conflicto("La nota está anulada: no se puede editar.");
        if (!EsEditable(nota, ahora))
            throw AppException.Conflicto(
                $"La nota ya no se puede editar (pasaron más de {CteNotasConsulta.HorasEdicion} horas). Agregá otra nota como aclaración.");
        if (dto.Version != nota.Version)
            throw AppException.Conflicto("La nota cambió mientras la editabas. Volvé a abrirla para ver la última versión.");

        var texto = ValidarTexto(dto.Texto);
        if (texto == nota.Texto)
            return Mapear(nota, ahora);

        // La versión anterior queda guardada tal cual: quién y cuándo la había escrito.
        notas.AgregarVersion(new NotaConsultaVersion
        {
            TenantId = nota.TenantId,
            NotaConsultaId = nota.Id,
            Version = nota.Version,
            Texto = nota.Texto,
            GuardadaEn = nota.EditadaEn ?? nota.CreadaEn,
            GuardadaPor = nota.EditadaPor ?? nota.CreadaPor
        });

        nota.Texto = texto;
        nota.Version++;
        nota.EditadaEn = ahora;
        nota.EditadaPor = usuario.Id;

        await uow.GuardarAsync(cancellationToken);
        return Mapear(nota, ahora);
    }

    public async Task<NotaConsultaDto> AnularAsync(int id, NotaConsultaAnularDto dto, CancellationToken cancellationToken = default)
    {
        ExigirProfesional();
        var motivo = (dto.Motivo ?? string.Empty).Trim();
        if (motivo.Length == 0)
            throw AppException.Validacion("Contá por qué se anula la nota.");
        if (motivo.Length > CteNotasConsulta.MaxMotivoAnulacion)
            throw AppException.Validacion($"El motivo no puede superar los {CteNotasConsulta.MaxMotivoAnulacion} caracteres.");

        var nota = await ObtenerNotaAsync(id, cancellationToken);
        if (nota.Anulada)
            throw AppException.Conflicto("La nota ya está anulada.");

        var ahora = Ahora();
        nota.Anulada = true;
        nota.AnuladaEn = ahora;
        nota.AnuladaPor = usuario.Id;
        nota.MotivoAnulacion = motivo;

        await uow.GuardarAsync(cancellationToken);
        return Mapear(nota, ahora);
    }

    public async Task<List<NotaConsultaVersionDto>> VersionesAsync(int id, CancellationToken cancellationToken = default)
    {
        ExigirProfesional();
        var nota = await ObtenerNotaAsync(id, cancellationToken);
        var anteriores = await notas.ListarVersionesAsync(id, cancellationToken);

        var vigente = new NotaConsultaVersionDto
        {
            Version = nota.Version,
            Texto = nota.Texto,
            GuardadaEn = Utc(nota.EditadaEn ?? nota.CreadaEn),
            Vigente = true
        };
        return [vigente, .. anteriores.Select(v => new NotaConsultaVersionDto
        {
            Version = v.Version,
            Texto = v.Texto,
            GuardadaEn = Utc(v.GuardadaEn),
            Vigente = false
        })];
    }

    // Hoy cada cuenta tiene un solo usuario (el profesional), pero el rol secretaria ya está
    // reservado: el día que exista, maneja la agenda y no ve las notas.
    private void ExigirProfesional()
    {
        if (tenant.Rol != CteRoles.Admin)
            throw AppException.SinPermiso("Las notas de consulta solo las ve el profesional.");
    }

    private async Task<Paciente> ObtenerPacienteAsync(int id, CancellationToken cancellationToken)
        => await pacientes.GetByIdAsync(id, cancellationToken) ?? throw AppException.RegistroNoExiste();

    private async Task<NotaConsulta> ObtenerNotaAsync(int id, CancellationToken cancellationToken)
        => await notas.GetByIdAsync(id, cancellationToken) ?? throw AppException.RegistroNoExiste();

    private static string ValidarTexto(string? texto)
    {
        var limpio = (texto ?? string.Empty).Trim();
        if (limpio.Length == 0)
            throw AppException.Validacion("La nota no puede estar vacía.");
        if (limpio.Length > CteNotasConsulta.MaxTexto)
            throw AppException.Validacion($"La nota no puede superar los {CteNotasConsulta.MaxTexto} caracteres.");
        return limpio;
    }

    private DateTime Ahora() => reloj.GetUtcNow().UtcDateTime;

    private static bool EsEditable(NotaConsulta n, DateTime ahora)
        => !n.Anulada && ahora < n.CreadaEn + VentanaEdicion;

    private static DateTimeOffset Utc(DateTime d) => new(DateTime.SpecifyKind(d, DateTimeKind.Utc));

    internal static NotaConsultaDto Mapear(NotaConsulta n, DateTime ahora) => new()
    {
        Id = n.Id,
        PacienteId = n.PacienteId,
        TurnoId = n.TurnoId,
        TurnoInicio = n.Turno is null ? null : Utc(n.Turno.Inicio),
        Texto = n.Texto,
        Version = n.Version,
        CreadaEn = Utc(n.CreadaEn),
        EditadaEn = n.EditadaEn is DateTime e ? Utc(e) : null,
        EditableHasta = Utc(n.CreadaEn + VentanaEdicion),
        Editable = EsEditable(n, ahora),
        Anulada = n.Anulada,
        AnuladaEn = n.AnuladaEn is DateTime a ? Utc(a) : null,
        MotivoAnulacion = n.MotivoAnulacion
    };
}
