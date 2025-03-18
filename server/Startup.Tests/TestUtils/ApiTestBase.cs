using Api.Websocket;
using Application.Interfaces.Infrastructure.Websocket;
using Infrastructure.Postgres;
using Infrastructure.Postgres.Scaffolding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PgCtx;
using Startup.Proxy;
using WebSocketBoilerplate;

namespace Startup.Tests.TestUtils;

public class ApiTestBase(ApiTestBaseConfig? apiTestBaseConfig = null)
    : WebApplicationFactory<Program>
{
    private readonly ApiTestBaseConfig _apiTestBaseConfig = apiTestBaseConfig ?? new ApiTestBaseConfig();
    private readonly PgCtxSetup<MyDbContext> _pgCtxSetup = new();
    public IConnectionManager ConnectionManager;
    public MyDbContext DbContext;
    public HttpClient HttpClient;
    public ILogger<ApiTestBase> Logger;
    public IServiceScope Scope;
    public WsRequestClient WsClient;
    public string WsClientId;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(ConfigureTestServices);
    }


    private void ConfigureTestServices(WebHostBuilderContext context, IServiceCollection services)
    {
        if (_apiTestBaseConfig.UseTestContainer)
        {
            RemoveExistingService<DbContextOptions<MyDbContext>>(services);
            services.AddDbContext<MyDbContext>(opt =>
            {
                opt.UseNpgsql(_pgCtxSetup._postgres.GetConnectionString());
                opt.EnableSensitiveDataLogging();
                opt.LogTo(_ => { });
            });
        }

        if (_apiTestBaseConfig.MockProxyConfig)
        {
            RemoveExistingService<IProxyConfig>(services);
            var mockProxy = new Mock<IProxyConfig>();
            services.AddSingleton(mockProxy.Object);
        }

        if (_apiTestBaseConfig.UseCustomSeeder)
        {
            RemoveExistingService<ISeeder>(services);
            services.AddSingleton<ISeeder, TestEnvironmentSeeder>();
        }

        if (_apiTestBaseConfig.MockWebSocketService)
        {
            var mockWsService = new Mock<IConnectionManager>();
            services.AddSingleton(mockWsService.Object);
        }
    }

    private void RemoveExistingService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
    }

    [SetUp]
    public async Task Setup()
    {
        HttpClient = CreateClient();

        //Singletons
        Logger = Services.GetRequiredService<ILogger<ApiTestBase>>();
        ConnectionManager = Services.GetRequiredService<IConnectionManager>();

        //Scoped services
        using var scope = Services.CreateScope();
        {
            Scope = Services.CreateScope();
            DbContext = Scope.ServiceProvider.GetRequiredService<MyDbContext>();
        }

        var wsPort = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrEmpty(wsPort)) throw new Exception("Environment variable PORT is not set");
        WsClientId = Guid.NewGuid().ToString();
        var url = "ws://localhost:" + wsPort + "?id=" + WsClientId;
        WsClient = new WsRequestClient(
            new[] { typeof(ServerSendsErrorMessage).Assembly },
            url
        );
        await WsClient.ConnectAsync();
        await Task.Delay(1000);
    }
}