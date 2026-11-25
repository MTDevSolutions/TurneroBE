using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Tests.Fakes;

/// <summary>
/// IHttpResponseFeature que dice que la respuesta ya empezó a enviarse. DefaultHttpContext
/// siempre reporta HasStarted = false, así que sin esto no se puede probar el caso del
/// streaming (comprobante PDF) que ya escribió headers cuando salta la excepción.
/// </summary>
public class ResponseYaEmpezadaFeature : IHttpResponseFeature
{
    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;

    public bool HasStarted => true;

    public void OnStarting(Func<object, Task> callback, object state) { }
    public void OnCompleted(Func<object, Task> callback, object state) { }
}
