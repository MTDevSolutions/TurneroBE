using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Turnos;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Turno;

namespace Application.Services;

/// <summary>
/// Agenda de la cuenta. Reglas: no se cargan turnos en el pasado, dos turnos vivos no se
/// superponen, y mover un turno de horario lo vuelve a "pendiente" (la confirmación era para
/// el horario anterior).
/// </summary>
public class TurnoService(
    ITurnoRepository turnos,
    IPacienteRepository pacientes,
    ISucursalRepository sucursales,
    IConfiguracionRepository configuracion,
    IUnitOfWork uow,
    TimeProvider reloj) : ITurnoService
{
    /// <summary>Tope del rango del calendario: alcanza para una vista mensual con bordes.</summary>
    public const int MaxDiasRango = 62;

    /// <summary>Margen para cargar un turno que "acaba de empezar" sin que lo rechace por pasado.</summary>
    private static readonly TimeSpan ToleranciaPasado = TimeSpan.FromMinutes(5);

    private const string MensajeSolapamiento = "Ese horario se superpone con otro turno.";

    public async Task<List<TurnoDto>> ListarAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken cancellationToken = default)
    {
        if (hasta <= desde)
            throw AppException.Validacion("El rango de fechas es inválido.");
        if ((hasta - desde).TotalDays > MaxDiasRango)
            throw AppException.Validacion($"El rango no puede superar los {MaxDiasRango} días.");

        var lista = await turnos.ListarRangoAsync(desde.UtcDateTime, hasta.UtcDateTime, cancellationToken);
        return lista.Select(Mapear).ToList();
    }

    public async Task<TurnoDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => Mapear(await ObtenerEntidadAsync(id, cancellationToken));

    public async Task<TurnoDto> CrearAsync(TurnoGuardarDto dto, CancellationToken cancellationToken = default)
    {
        var turno = new Turno
        {
            Estado = CteEstadosTurno.Pendiente,
            FechaAlta = reloj.GetUtcNow().UtcDateTime
        };

        await AplicarAsync(turno, dto, cancellationToken);
        turnos.Agregar(turno);

        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.TurnoSinSolapamiento, MensajeSolapamiento, cancellationToken);
        return Mapear(turno);
    }

    public async Task<TurnoDto> ReprogramarAsync(int id, TurnoGuardarDto dto, CancellationToken cancellationToken = default)
    {
        var turno = await ObtenerEntidadAsync(id, cancellationToken);
        if (!MaquinaEstadosTurno.EsEditable(turno.Estado))
            throw AppException.Conflicto($"Un turno {turno.Estado} no se puede modificar.");

        var (inicioAnterior, finAnterior) = (turno.Inicio, turno.Fin);
        await AplicarAsync(turno, dto, cancellationToken);

        if (turno.Inicio != inicioAnterior || turno.Fin != finAnterior)
            turno.Estado = CteEstadosTurno.Pendiente;

        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.TurnoSinSolapamiento, MensajeSolapamiento, cancellationToken);
        return Mapear(turno);
    }

    public async Task<TurnoDto> CambiarEstadoAsync(int id, TurnoEstadoDto dto, CancellationToken cancellationToken = default)
    {
        var hacia = (dto.Estado ?? string.Empty).Trim().ToLowerInvariant();
        if (!MaquinaEstadosTurno.EsEstadoConocido(hacia))
            throw AppException.Validacion("Estado de turno desconocido.");

        var turno = await ObtenerEntidadAsync(id, cancellationToken);
        if (turno.Estado == hacia)
            return Mapear(turno);

        if (!MaquinaEstadosTurno.EsTransicionValida(turno.Estado, hacia))
            throw AppException.Conflicto($"Un turno {turno.Estado} no puede pasar a {hacia}.");

        if (MaquinaEstadosTurno.RequiereTurnoEmpezado(hacia) && turno.Inicio > reloj.GetUtcNow().UtcDateTime)
            throw AppException.Conflicto($"Un turno que todavía no empezó no puede marcarse como {hacia}.");

        turno.Estado = hacia;
        if (hacia == CteEstadosTurno.Cancelado)
        {
            turno.CanceladoPor = CteEstadosTurno.CanceladoPorProfesional;
            turno.MotivoCancelacion = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim();
        }

        await uow.GuardarAsync(cancellationToken);
        return Mapear(turno);
    }

    private async Task<Turno> ObtenerEntidadAsync(int id, CancellationToken cancellationToken)
        => await turnos.GetByIdAsync(id, cancellationToken) ?? throw AppException.RegistroNoExiste();

    /// <summary>Valida y aplica paciente, consultorio, horario y notas (alta y reprogramación).</summary>
    private async Task AplicarAsync(Turno turno, TurnoGuardarDto dto, CancellationToken cancellationToken)
    {
        var paciente = await pacientes.GetByIdAsync(dto.PacienteId, cancellationToken)
            ?? throw AppException.Validacion("El paciente no existe.");
        if (!paciente.Activo && paciente.Id != turno.PacienteId)
            throw AppException.Validacion("El paciente está desactivado.");

        var sucursal = await ResolverConsultorioAsync(dto.SucursalId, cancellationToken);

        var duracion = dto.DuracionMinutos
            ?? (await configuracion.ObtenerAsync(cancellationToken))?.DuracionTurnoMinutos
            ?? new Configuracion().DuracionTurnoMinutos;

        var inicio = dto.Inicio.UtcDateTime;
        var fin = inicio.AddMinutes(duracion);

        var horarioCambia = inicio != turno.Inicio || fin != turno.Fin;
        if (horarioCambia && inicio < reloj.GetUtcNow().UtcDateTime - ToleranciaPasado)
            throw AppException.Validacion("No se puede dar un turno en el pasado.");

        if (horarioCambia && await turnos.HaySolapamientoAsync(inicio, fin, turno.Id == 0 ? null : turno.Id, cancellationToken))
            throw AppException.Conflicto(MensajeSolapamiento);

        turno.PacienteId = paciente.Id;
        turno.Paciente = paciente;
        turno.SucursalId = sucursal.Id;
        turno.Sucursal = sucursal;
        turno.Inicio = inicio;
        turno.Fin = fin;
        turno.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
    }

    // Sin consultorio explícito se usa el único activo; con varios, hay que elegir.
    private async Task<Sucursal> ResolverConsultorioAsync(int? sucursalId, CancellationToken cancellationToken)
    {
        if (sucursalId is int id)
            return await sucursales.GetActivaAsync(id, cancellationToken)
                ?? throw AppException.Validacion("El consultorio no existe o está inactivo.");

        var activas = await sucursales.ListarActivasAsync(cancellationToken);
        return activas.Count switch
        {
            1 => activas[0],
            0 => throw AppException.Conflicto("La cuenta no tiene consultorios activos."),
            _ => throw AppException.Validacion("Elegí el consultorio del turno.")
        };
    }

    private static TurnoDto Mapear(Turno t) => new()
    {
        Id = t.Id,
        PacienteId = t.PacienteId,
        PacienteNombre = $"{t.Paciente.Nombre} {t.Paciente.Apellido}",
        PacienteCelular = t.Paciente.Celular,
        SucursalId = t.SucursalId,
        SucursalNombre = t.Sucursal.Nombre,
        Inicio = new DateTimeOffset(DateTime.SpecifyKind(t.Inicio, DateTimeKind.Utc)),
        Fin = new DateTimeOffset(DateTime.SpecifyKind(t.Fin, DateTimeKind.Utc)),
        Estado = t.Estado,
        CanceladoPor = t.CanceladoPor,
        MotivoCancelacion = t.MotivoCancelacion,
        Notas = t.Notas
    };
}
