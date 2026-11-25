using Application.Helpers;

namespace Tests.Helpers;

public class InterpreteRespuestaTests
{
    [Theory]
    [InlineData("Sí")]
    [InlineData("si")]
    [InlineData("SI!!")]
    [InlineData("Siii")]
    [InlineData("sí, ahí estaré")]
    [InlineData("Confirmo")]
    [InlineData("ok")]
    [InlineData("Dale, gracias")]
    [InlineData("👍")]
    [InlineData("  sí  ")]
    public void Entiende_confirmaciones(string texto)
        => Assert.Equal(IntencionRespuesta.Confirmar, InterpreteRespuesta.Interpretar(texto));

    [Theory]
    [InlineData("No")]
    [InlineData("no puedo")]
    [InlineData("NO.")]
    [InlineData("Cancelo")]
    [InlineData("nop, perdón")]
    [InlineData("👎")]
    public void Entiende_cancelaciones(string texto)
        => Assert.Equal(IntencionRespuesta.Cancelar, InterpreteRespuesta.Interpretar(texto));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("gracias")]
    [InlineData("¿puedo ir más tarde?")]
    [InlineData("no sé si llego")]
    [InlineData("hola")]
    [InlineData("sino otro día")]
    public void Ante_la_duda_no_toca_el_turno(string? texto)
        => Assert.Equal(IntencionRespuesta.NoEntendida, InterpreteRespuesta.Interpretar(texto));
}
