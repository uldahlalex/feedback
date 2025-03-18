using System.Net.Http.Json;
using System.Text.Json;
using Api.Rest.Controllers;
using Api.Websocket;
using Application.Models;
using Application.Models.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Startup.Tests.TestUtils;
using WebSocketBoilerplate;

namespace Startup.Tests.EventTests;

public class Multi_Client_Multi_Api : ApiTestBase
{
    [Test]
    public async Task AlexGetsBroadcastWhenClientsSendMessage()
    {
        var client2Id = Guid.NewGuid().ToString();
        var wsPort = Environment.GetEnvironmentVariable("PORT");
        var url = "ws://localhost:" + wsPort + "?id=" + client2Id;
        var wsClient2 = new WsRequestClient(
            new[] { typeof(ServerSendsErrorMessage).Assembly },
            url
        );
        await wsClient2.ConnectAsync();
        await Task.Delay(1000);
        var pass = Services.GetRequiredService<IOptionsMonitor<AppOptions>>().CurrentValue.Pass;
        var alexSignsInResult = await HttpClient.PostAsJsonAsync<AuthResponseDto>(
            AuthController.LoginRoute, new AuthRequestDto() { ClientId = client2Id, Password = pass });

        var studentSendsMessageResult = await HttpClient.PostAsJsonAsync(QuestionController.AddQuestionRoute,
            new CreateQuestionDto() { QuestionText = "what is the meaning of life?" });
        await Task.Delay(100);
        var checkForBroadcast = wsClient2.ReceivedMessagesAsJsonStrings
            .Select(msg => JsonSerializer.Deserialize<BaseDto>(msg))
            .Where(d => d.eventType.Equals(nameof(BroadcastToAlex)));
        wsClient2.ReceivedMessagesAsJsonStrings.ForEach(e => Logger.LogInformation(e));
       
        if (checkForBroadcast.Count() != 1)
            throw new Exception(
                "We expected alex to get exactly one broadcast. However, this is the entire received messages state for alex: " +
                JsonSerializer.Serialize(wsClient2.ReceivedMessages));
    }
}