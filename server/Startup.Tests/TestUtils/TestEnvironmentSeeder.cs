using Infrastructure.Postgres;

namespace Startup.Tests.TestUtils;

public class TestEnvironmentSeeder : ISeeder
{
    public Task Seed()
    {
        return Task.CompletedTask;
    }
}