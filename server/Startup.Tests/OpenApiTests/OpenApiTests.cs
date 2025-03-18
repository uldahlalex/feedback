using NSwag;
using NUnit.Framework;
using Startup.Tests.TestUtils;

namespace Startup.Tests.OpenApiTests;

public class OpenApiTests : ApiTestBase
{
    [Test]
    public async Task CanGetJsonResponseFromOpenApi()
    {
        var response = await CreateClient().GetAsync("/openapi/v1.json");
        var document = await OpenApiDocument.FromJsonAsync(await response.Content.ReadAsStringAsync());
        if (document.Paths.Count == 0)
            throw new Exception("Expected paths to be present in the open api document");
    }
}