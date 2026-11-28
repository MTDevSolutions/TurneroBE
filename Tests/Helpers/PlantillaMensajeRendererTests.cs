using Application.Helpers;
using Shared.Constantes;

namespace Tests.Helpers;

public class PlantillaMensajeRendererTests
{
    private static readonly VariablesMensaje Valores = new("Ana", "Ruiz", "Lic. Pérez", "jueves 3/12", "15:00", "Consultorio (Gorriti 1234)");

    [Fact]
    public void Reemplaza_todas_las_variables()
    {
        var texto = PlantillaMensajeRenderer.Renderizar(
            "{paciente} {apellido}, {profesional} te espera el {fecha} a las {hora} en {lugar}.", Valores);

        Assert.Equal("Ana Ruiz, Lic. Pérez te espera el jueves 3/12 a las 15:00 en Consultorio (Gorriti 1234).", texto);
    }

    [Fact]
    public void Una_variable_desconocida_queda_tal_cual()
        => Assert.Equal("Hola {nombre}", PlantillaMensajeRenderer.Renderizar("Hola {nombre}", Valores));

    [Theory]
    [InlineData("Hola {nombre}", "nombre")]
    [InlineData("Hola {Paciente}", "Paciente")]
    [InlineData("Hola {fecha del turno}", "fecha del turno")]
    public void Detecta_variables_desconocidas(string texto, string desconocida)
        => Assert.Equal([desconocida], PlantillaMensajeRenderer.VariablesDesconocidas(texto));

    [Fact]
    public void Los_textos_de_fabrica_solo_usan_variables_validas()
        => Assert.All(CtePlantillasMensaje.Todas,
            d => Assert.Empty(PlantillaMensajeRenderer.VariablesDesconocidas(d.TextoPorDefecto)));

    [Fact]
    public void Armar_agrega_el_pie_fijo_aunque_el_texto_propio_no_lo_tenga()
    {
        var aviso = CtePlantillasMensaje.Buscar(CtePlantillasMensaje.Aviso)!;

        var texto = PlantillaMensajeRenderer.Armar(aviso, "Hola {paciente}, turno el {fecha}.", Valores);

        Assert.Equal($"Hola Ana, turno el jueves 3/12.\n\n{CtePlantillasMensaje.PiePedidoConfirmacion}", texto);
    }

    [Fact]
    public void Los_mensajes_que_piden_respuesta_tienen_pie_fijo()
    {
        foreach (var clave in new[] { CtePlantillasMensaje.Aviso, CtePlantillasMensaje.Reprogramado, CtePlantillasMensaje.Recordatorio })
            Assert.Equal(CtePlantillasMensaje.PiePedidoConfirmacion, CtePlantillasMensaje.Buscar(clave)!.Pie);
        Assert.Equal(CtePlantillasMensaje.PiePedidoCancelacion,
            CtePlantillasMensaje.Buscar(CtePlantillasMensaje.RecordatorioConfirmado)!.Pie);
    }
}
