using Application.UseCases.Turnos;
using Shared.Constantes;

namespace Tests.UseCases;

public class MaquinaEstadosTurnoTests
{
    [Theory]
    [InlineData(CteEstadosTurno.Pendiente, CteEstadosTurno.Confirmado)]
    [InlineData(CteEstadosTurno.Pendiente, CteEstadosTurno.Cancelado)]
    [InlineData(CteEstadosTurno.Confirmado, CteEstadosTurno.Pendiente)]
    [InlineData(CteEstadosTurno.Confirmado, CteEstadosTurno.Ausente)]
    public void Transiciones_validas(string desde, string hacia)
        => Assert.True(MaquinaEstadosTurno.EsTransicionValida(desde, hacia));

    [Theory]
    [InlineData(CteEstadosTurno.Cancelado, CteEstadosTurno.Pendiente)]
    [InlineData(CteEstadosTurno.Atendido, CteEstadosTurno.Ausente)]
    [InlineData(CteEstadosTurno.Ausente, CteEstadosTurno.Atendido)]
    [InlineData("inventado", CteEstadosTurno.Confirmado)]
    public void Los_estados_finales_no_se_mueven(string desde, string hacia)
        => Assert.False(MaquinaEstadosTurno.EsTransicionValida(desde, hacia));

    [Fact]
    public void Solo_pendiente_y_confirmado_son_editables()
    {
        Assert.True(MaquinaEstadosTurno.EsEditable(CteEstadosTurno.Pendiente));
        Assert.True(MaquinaEstadosTurno.EsEditable(CteEstadosTurno.Confirmado));
        Assert.False(MaquinaEstadosTurno.EsEditable(CteEstadosTurno.Cancelado));
        Assert.False(MaquinaEstadosTurno.EsEditable(CteEstadosTurno.Atendido));
    }
}
