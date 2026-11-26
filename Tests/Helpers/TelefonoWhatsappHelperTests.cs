using Application.Helpers;

namespace Tests.Helpers;

public class TelefonoWhatsappHelperTests
{
    [Theory]
    [InlineData("11 2345-6789", "5491123456789")]
    [InlineData("1123456789", "5491123456789")]
    [InlineData("+54 9 11 2345 6789", "5491123456789")]
    [InlineData("5491123456789", "5491123456789")]
    [InlineData("54 11 2345 6789", "5491123456789")]
    [InlineData("353 412-3456", "5493534123456")]
    [InlineData("(0353) 4123456", null)]       // con 0: ambiguo, no se adivina
    [InlineData("011 15 2345 6789", null)]     // con 0 y 15
    [InlineData("15 2345 6789", null)]         // 15 sin código de área
    [InlineData("4123456", null)]              // sin código de área
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normaliza_segun_las_reglas_de_Argentina(string? entrada, string? esperado)
        => Assert.Equal(esperado, TelefonoWhatsappHelper.Normalizar(entrada));

}
