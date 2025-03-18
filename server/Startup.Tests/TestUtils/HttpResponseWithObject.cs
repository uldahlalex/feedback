namespace Startup.Tests.TestUtils;

public class HttpResponseWithObject<T>
{
    public HttpResponseMessage HttpResponseMessage { get; set; } = null!;
    public T? Object { get; set; }
}