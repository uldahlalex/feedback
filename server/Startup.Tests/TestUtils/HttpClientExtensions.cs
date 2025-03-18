using System.Net.Http.Json;
using System.Text.Json;

namespace Startup.Tests.TestUtils;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseWithObject<T>> GetFromJsonAsync<T>(this HttpClient httpClient,
        string requestUri)
    {
        var httpResponse = await httpClient.GetAsync(requestUri);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var obj = await httpResponse.Content.ReadFromJsonAsync<T>(options);

        return new HttpResponseWithObject<T>
        {
            HttpResponseMessage = httpResponse,
            Object = obj
        };
    }

    public static async Task<HttpResponseWithObject<T?>> PostAsJsonAsync<T>(this HttpClient httpClient,
        string requestUri, object content)
    {
        var httpResponse = await httpClient.PostAsJsonAsync(requestUri, content);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var obj = await httpResponse.Content.ReadFromJsonAsync<T>(options);

        return new HttpResponseWithObject<T?>
        {
            HttpResponseMessage = httpResponse,
            Object = obj
        };
    }

    public static async Task<HttpResponseWithObject<T?>> PutAsJsonAsync<T>(this HttpClient httpClient,
        string requestUri,
        object content)
    {
        var httpResponse = await httpClient.PutAsJsonAsync(requestUri, content);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var obj = await httpResponse.Content.ReadFromJsonAsync<T>(options);

        return new HttpResponseWithObject<T?>
        {
            HttpResponseMessage = httpResponse,
            Object = obj
        };
    }

    public static async Task<HttpResponseWithObject<T?>> PatchAsJsonAsync<T>(this HttpClient httpClient,
        string requestUri, object content)
    {
        var httpResponse = await httpClient.PatchAsync(requestUri, JsonContent.Create(content));

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var obj = await httpResponse.Content.ReadFromJsonAsync<T>(options);

        return new HttpResponseWithObject<T?>
        {
            HttpResponseMessage = httpResponse,
            Object = obj
        };
    }
}