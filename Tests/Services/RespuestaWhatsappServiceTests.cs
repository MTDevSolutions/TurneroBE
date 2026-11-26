using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;
using Shared.Settings;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// El "Sí" confirma y el "No" cancela el próximo turno del paciente, siempre dentro de la cuenta
/// que dice la etiqueta del teléfono. Lo demás se registra sin tocar la agenda.
/// </summary>
public class RespuestaWhatsappServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
    private const string Celular = "5491123456789";

    private readonly TenantRepositoryFake _tenants = new();
    private readonly RespuestaWhatsappRepositoryFake _datos = new();
    private readonly MensajeEntranteRepositoryFake _entrantes = new();
    private readonly MensajeWhatsappRepositoryFake _salientes = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly Tenant _cuenta;
    private readonly Turno _turno;
    private long _proximoId = 100;

    public RespuestaWhatsappServiceTests()
    {
        _cuenta = _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Uno", Especialidad = "psicologia", Estado = "activo", WhatsappTelefonoId = 7, TerminosVersionAceptada = CteTerminos.VersionVigente });
        _tenants.Agregar(new Tenant { Id = 2, Nombre = "Lic. Dos", Especialidad = "psicologia", Estado = "activo", WhatsappTelefonoId = 8, TerminosVersionAceptada = CteTerminos.VersionVigente });

        var ana = new Paciente { Id = 10, TenantId = 1, Nombre = "Ana", Apellido = "Ruiz", Celular = Celular, Activo = true };
        _datos.Pacientes.Add(ana);
        _turno = new Turno
        {
            Id = 50, TenantId = 1, PacienteId = 10, Paciente = ana, Sucursal = new Sucursal { Nombre = "Consultorio" },
            Inicio = Ahora.UtcDateTime.AddDays(1), Fin = Ahora.UtcDateTime.AddDays(1).AddMinutes(45),
            Estado = CteEstadosTurno.Pendiente,
        };
        _datos.Turnos.Add(_turno);
        _datos.Usuarios.Add(new Usuario { Id = 1, TenantId = 1, Rol = CteRoles.Admin, Activo = true, Celular = "5491199998888", Nombre = "Laura", Email = "l@x", PasswordHash = "x" });
    }

    private RespuestaWhatsappService Servicio(string secreto = "s3cr3to") => new(
        Options.Create(new WhatsappGatewaySettings { WebhookSecret = secreto }),
        _tenants, _datos, _entrantes, _salientes, _uow, new RelojFake(Ahora),
        NullLogger<RespuestaWhatsappService>.Instance);

    private WebhookEntranteDto Mensaje(string texto, string etiqueta = "1", string numero = Celular, long? id = null) => new()
    {
        Id = id ?? _proximoId++,
        Numero = numero,
        Texto = texto,
        RecibidoEn = Ahora.UtcDateTime,
        Etiqueta = etiqueta,
    };

    [Fact]
    public async Task Si_confirma_el_turno_y_le_responde_al_paciente()
    {
        await Servicio().ProcesarAsync(Mensaje("Sí, ahí estaré"));

        Assert.Equal(CteEstadosTurno.Confirmado, _turno.Estado);
        var respuesta = Assert.Single(_salientes.Mensajes);
        Assert.Equal(CteMensajesWhatsapp.TipoConfirmacionRecibida, respuesta.Tipo);
        Assert.Equal(Celular, respuesta.Destino);
        Assert.Equal(1, respuesta.TenantId);
        var entrante = Assert.Single(_entrantes.Mensajes);
        Assert.Equal(CteRespuestasWhatsapp.Confirmo, entrante.Resultado);
        Assert.Equal(_turno.Id, entrante.TurnoId);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task No_cancela_libera_el_horario_y_avisa_al_profesional()
    {
        await Servicio().ProcesarAsync(Mensaje("No puedo, perdón"));

        Assert.Equal(CteEstadosTurno.Cancelado, _turno.Estado);
        Assert.Equal(CteEstadosTurno.CanceladoPorPaciente, _turno.CanceladoPor);
        Assert.Equal(
            [(CteMensajesWhatsapp.TipoCancelacionRecibida, Celular), (CteMensajesWhatsapp.TipoAvisoProfesional, "5491199998888")],
            _salientes.Mensajes.Select(m => (m.Tipo, m.Destino)).ToArray());
        Assert.Contains("Ana Ruiz canceló", _salientes.Mensajes[1].Texto);
    }

    [Fact]
    public async Task Otra_cosa_no_toca_el_turno_ni_contesta()
    {
        await Servicio().ProcesarAsync(Mensaje("¿Puedo llevar a mi hija?"));

        Assert.Equal(CteEstadosTurno.Pendiente, _turno.Estado);
        Assert.Empty(_salientes.Mensajes);
        Assert.Equal(CteRespuestasWhatsapp.NoEntendido, Assert.Single(_entrantes.Mensajes).Resultado);
    }

    [Fact]
    public async Task El_mismo_numero_en_otra_cuenta_no_toca_este_turno()
    {
        // El paciente existe en la cuenta 1, pero el mensaje llegó al teléfono de la cuenta 2.
        await Servicio().ProcesarAsync(Mensaje("no", etiqueta: "2"));

        Assert.Equal(CteEstadosTurno.Pendiente, _turno.Estado);
        Assert.Empty(_salientes.Mensajes);
        var entrante = Assert.Single(_entrantes.Mensajes);
        Assert.Equal(2, entrante.TenantId);
        Assert.Equal(CteRespuestasWhatsapp.PacienteDesconocido, entrante.Resultado);
    }

    [Fact]
    public async Task Paciente_sin_turnos_proximos_queda_registrado()
    {
        _turno.Inicio = Ahora.UtcDateTime.AddHours(-2);

        await Servicio().ProcesarAsync(Mensaje("si"));

        Assert.Equal(CteRespuestasWhatsapp.SinTurno, Assert.Single(_entrantes.Mensajes).Resultado);
        Assert.Empty(_salientes.Mensajes);
    }

    [Fact]
    public async Task Un_reintento_del_gateway_no_se_procesa_dos_veces()
    {
        await Servicio().ProcesarAsync(Mensaje("si", id: 7));
        await Servicio().ProcesarAsync(Mensaje("si", id: 7));

        Assert.Single(_entrantes.Mensajes);
        Assert.Single(_salientes.Mensajes);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Etiqueta_invalida_o_cuenta_suspendida_se_ignoran()
    {
        await Servicio().ProcesarAsync(Mensaje("no", etiqueta: "dixit-3"));
        _cuenta.Estado = CteEstadosTenant.Suspendido;
        await Servicio().ProcesarAsync(Mensaje("no"));

        Assert.Empty(_entrantes.Mensajes);
        Assert.Equal(CteEstadosTurno.Pendiente, _turno.Estado);
        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Carrera_con_otro_reintento_da_409_para_que_el_gateway_reintente()
    {
        _uow.ProximoDuplicadoEnIndice = CteIndices.MensajeEntranteGateway;

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().ProcesarAsync(Mensaje("si")));

        Assert.Equal(409, ex.StatusCode);
    }

    [Theory]
    [InlineData("s3cr3to", true)]
    [InlineData("otro", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Valida_el_secreto(string? recibido, bool valido)
        => Assert.Equal(valido, Servicio().SecretoValido(recibido));

    [Fact]
    public void Sin_secreto_configurado_rechaza_todo()
        => Assert.False(Servicio(secreto: "").SecretoValido(""));

    [Fact]
    public async Task Cuenta_sin_whatsapp_vinculado_no_toca_turnos()
    {
        _cuenta.WhatsappTelefonoId = null;

        await Servicio().ProcesarAsync(Mensaje("No"));

        Assert.Equal(CteEstadosTurno.Pendiente, _turno.Estado);
        Assert.Empty(_entrantes.Mensajes);
        Assert.Empty(_salientes.Mensajes);
    }

    [Fact]
    public async Task Numero_y_texto_nulos_no_rompen_el_webhook()
    {
        var mensaje = Mensaje("x");
        mensaje.Numero = null;
        mensaje.Texto = null;

        await Servicio().ProcesarAsync(mensaje);

        var entrante = Assert.Single(_entrantes.Mensajes);
        Assert.Equal(CteRespuestasWhatsapp.PacienteDesconocido, entrante.Resultado);
        Assert.Equal(CteEstadosTurno.Pendiente, _turno.Estado);
    }
}
