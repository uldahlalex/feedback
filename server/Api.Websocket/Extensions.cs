using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;
using Application.Interfaces.Infrastructure.Websocket;
using Fleck;
using WebSocketBoilerplate;

namespace Api.Websocket;

public static class Extensions
{
    public static IServiceCollection RegisterWebsocketApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        var assembly = typeof(Extensions).Assembly;
        services.InjectEventHandlers(assembly);
        return services;
    }

    public static async Task<WebApplication> ConfigureWebsocketApi(this WebApplication app, int wsPort = 8181)
    {
        app.UseRouting();
        var port = GetAvailablePort(wsPort);
        Environment.SetEnvironmentVariable("PORT", port.ToString());
        var url = $"ws://0.0.0.0:{port}";
        var logger = app.Services.GetRequiredService<ILogger<NonStaticWsExtensionClassForLogger>>();
        logger.LogInformation("WS running on url: " + url);
        var server = new WebSocketServer(url);
        var manager = app.Services.GetRequiredService<IConnectionManager>();

        Action<IWebSocketConnection> config = ws =>
        {
            var queryString = ws.ConnectionInfo.Path.Split('?').Length > 1
                ? ws.ConnectionInfo.Path.Split('?')[1]
                : "";

            var id = HttpUtility.ParseQueryString(queryString)["id"] ??
                     throw new Exception("Please specify ID query param for websocket connection");
      
            ws.OnOpen = () => manager.OnOpen(ws, id);
            ws.OnClose = () => manager.OnClose(ws, id);
            ws.OnError = ex => ws.SendDto(new ServerSendsErrorMessage { Message = ex.Message });
            ws.OnMessage = async message =>
            {
                try
                {
                    await app.CallEventHandler(ws, message);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error in handling message: {message}", message);
                    BaseDto baseDto = null;
                    try
                    {
                        baseDto = JsonSerializer.Deserialize<BaseDto>(message, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch
                    {
                       baseDto= new BaseDto
                        {
                            eventType = nameof(ServerSendsErrorMessage)
                        };
                    }
                    
                    var resp = new ServerSendsErrorMessage
                        { Message = e.Message, requestId = baseDto.requestId ?? "" };
                    ws.SendDto(resp);
                }
            };
        };

        server.Start(config);
        return app;
    }
    


    private static int GetAvailablePort(int startPort)
    {
        var port = startPort;
        var isPortAvailable = false;

        do
        {
            try
            {
                var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();
                isPortAvailable = true;
            }
            catch (SocketException)
            {
                port++;
            }
        } while (!isPortAvailable);

        return port;
    }
}

public class ServerSendsPing : BaseDto;

public class NonStaticWsExtensionClassForLogger;