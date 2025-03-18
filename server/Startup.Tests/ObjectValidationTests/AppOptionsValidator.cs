using System.ComponentModel.DataAnnotations;
using Application.Models;
using NUnit.Framework;
using Startup.Tests.TestUtils;

namespace Startup.Tests.ObjectValidationTests;

public class AppOptionsValidator() : ApiTestBase(new ApiTestBaseConfig
{
    UseTestContainer = false,
    MockWebSocketService = true
})
{
    [Test]
    public Task AppOptionsValidatorThrowsException()
    {
        var opts = new AppOptions();
        var context = new ValidationContext(opts, null, null);
        Assert.Throws<ValidationException>(() => Validator.ValidateObject(opts, context));
        return Task.CompletedTask;
    }

    [Test]
    public Task AppOptionsValidatorAcceptsValidAppOptions()
    {
        var opts = new AppOptions
        {
            DbConnectionString = "abc",
            JwtSecret = "abc",
            Seed = true
        };
        var context = new ValidationContext(opts, null, null);
        Validator.ValidateObject(opts, context); //Does not throw
        return Task.CompletedTask;
    }
}