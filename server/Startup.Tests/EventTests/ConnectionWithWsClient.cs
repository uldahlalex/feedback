using System.Text.Json;
using NUnit.Framework;
using Startup.Tests.TestUtils;

namespace Startup.Tests.EventTests;

public class ConnectionWithWsClientSt : ApiTestBase
{
    [Theory]
    public async Task Api_Can_Successfully_Add_Connection()
    {
        var pairForClientId = ConnectionManager.GetAllConnectionIdsWithSocketId().Result
            .First(pair => pair.Key == WsClientId);
        if (pairForClientId.Key != WsClientId && pairForClientId.Value.Length > 5)
            throw new Exception("ConnectionIdToSocket should have client ID key and a socket ID, but state was: " +
                                "" + JsonSerializer.Serialize(
                                    await ConnectionManager.GetAllConnectionIdsWithSocketId()));
        if (ConnectionManager.GetAllSocketIdsWithConnectionId().Result.Keys.Count != 1)
            throw new Exception("SocketToConnectionId should have 1 value, but state was: " +
                                "" + JsonSerializer.Serialize(
                                    await ConnectionManager.GetAllSocketIdsWithConnectionId()));
    }
}