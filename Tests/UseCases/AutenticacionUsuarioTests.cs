using Application.Exceptions;
using Application.UseCases.Auth;
using Domain.Entities;
using Tests.Fakes;

namespace Tests.UseCases;

/// <summary>
/// Login de usuario de negocio. Lo que se protege acá es que el resultado NO permita
/// enumerar cuentas: sin la password correcta, un email existente y uno inexistente
/// tienen que ser indistinguibles (mismo código y mismo costo de cómputo).
/// </summary>
public class AutenticacionUsuarioTests
{
    private const string Email = "juan@consultorio.com";
    private const string Password = "Secreta123!";

    private static Usuario UsuarioDe(bool activo = true) => new()
    {
        Id = 1,
        TenantId = 7,
        SucursalId = 1,
        Nombre = "Juan",
        Email = Email,
        Rol = "admin",
        PasswordHash = PasswordServiceFake.Hash(Password),
        Activo = activo,
    };

    private static (AutenticacionUsuario Caso, PasswordServiceFake Passwords) Armar(
        Usuario? usuario, string estadoTenant = "activo")
    {
        var passwords = new PasswordServiceFake();
        var caso = new AutenticacionUsuario(
            new UsuarioRepositoryFake(usuario),
            passwords,
            new TenantEstadoServiceFake(estadoTenant));
        return (caso, passwords);
    }

    [Fact]
    public async Task Devuelve_el_usuario_con_credenciales_validas()
    {
        var (caso, _) = Armar(UsuarioDe());

        var usuario = await caso.ExecuteAsync(Email, Password);

        Assert.Equal(Email, usuario.Email);
    }

    [Fact]
    public async Task Email_inexistente_da_401_y_nivela_el_timing()
    {
        var (caso, passwords) = Armar(usuario: null);

        var ex = await Assert.ThrowsAsync<AppException>(() => caso.ExecuteAsync("nadie@x.com", Password));

        Assert.Equal(401, ex.StatusCode);
        Assert.True(passwords.VerificarDummyLlamado, "sin VerificarDummy el email inexistente responde más rápido");
    }

    [Fact]
    public async Task Usuario_inactivo_con_password_incorrecta_da_401_como_cualquier_otro()
    {
        var (caso, _) = Armar(UsuarioDe(activo: false));

        var ex = await Assert.ThrowsAsync<AppException>(() => caso.ExecuteAsync(Email, "no-es-la-password"));

        // Si acá saliera 403, bastaría con probar emails para descubrir cuáles son cuentas reales.
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Usuario_inactivo_con_password_correcta_da_403_y_lo_dice()
    {
        var (caso, _) = Armar(UsuarioDe(activo: false));

        var ex = await Assert.ThrowsAsync<AppException>(() => caso.ExecuteAsync(Email, Password));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("desactivado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_suspendido_con_password_correcta_da_403()
    {
        var (caso, _) = Armar(UsuarioDe(), estadoTenant: "suspendido");

        var ex = await Assert.ThrowsAsync<AppException>(() => caso.ExecuteAsync(Email, Password));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("suspendida", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Password_incorrecta_no_llega_a_mirar_el_estado_del_tenant()
    {
        var (caso, _) = Armar(UsuarioDe(), estadoTenant: "suspendido");

        var ex = await Assert.ThrowsAsync<AppException>(() => caso.ExecuteAsync(Email, "no-es-la-password"));

        Assert.Equal(401, ex.StatusCode);
    }
}
