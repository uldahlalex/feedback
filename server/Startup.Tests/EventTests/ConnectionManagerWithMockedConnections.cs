using Fleck;
using Moq;
using NUnit.Framework;
using Startup.Tests.TestUtils;

namespace Startup.Tests.EventTests;

public class ConnectionManagerWithMockedConnections : ApiTestBase
{
    [Test]
    public async Task OnConnect_Can_Add_Socket_And_Client_To_Storage()
    {
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;

        // act
        await ConnectionManager.OnOpen(ws, connectionId);

        // assert
        if (!ConnectionManager.GetAllConnectionIdsWithSocketId().Result.Values
                .Contains(ws.ConnectionInfo.Id.ToString()))
            throw new Exception("The dictionary should contain the websocket with guid " + ws.ConnectionInfo.Id +
                                " as the first value");
        if (!ConnectionManager.GetAllSocketIdsWithConnectionId().Result.Values.Contains(connectionId))
            throw new Exception("The dictionary " + nameof(ConnectionManager.GetAllSocketIdsWithConnectionId) +
                                " should contain the connectionId with guid " + connectionId +
                                " as the first value");
    }

    [Test]
    public async Task OnClose_Can_Remove_Socket_And_Client_From_Storage()
    {
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        await ConnectionManager.OnOpen(ws, connectionId);

        // act
        await ConnectionManager.OnClose(ws, connectionId);

        // assert
        if (ConnectionManager.GetAllConnectionIdsWithSocketId().Result.Values
            .Contains(ws.ConnectionInfo.Id.ToString()))
            throw new Exception("The dictionary should not contain the websocket with guid " + ws.ConnectionInfo.Id);
        if (ConnectionManager.GetAllSocketIdsWithConnectionId().Result.Values.Contains(connectionId))
            throw new Exception("The dictionary should not contain the connectionId with guid " + connectionId);
    }

    [Test]
    public async Task AddToTopic_Correctly_Adds_Member_To_Topic()
    {
        var randomTopic = Guid.NewGuid().ToString();
        var randomUser = Guid.NewGuid().ToString();

        await ConnectionManager.AddToTopic(randomTopic, randomUser);

        var members = await ConnectionManager.GetMembersFromTopicId(randomTopic);
        if (!members.Contains(randomUser))
            throw new Exception("The topic " + randomTopic + " should contain the user " + randomUser);

        var topics = await ConnectionManager.GetTopicsFromMemberId(randomUser);
        if (!topics.Contains(randomTopic))
            throw new Exception("The user " + randomUser + " should be in the topic " + randomTopic);
    }

    [Test]
    public async Task RemoveFromTopic_Correctly_Removes_Member_From_Topic()
    {
        var randomTopic = Guid.NewGuid().ToString();
        var randomUser = Guid.NewGuid().ToString();

        await ConnectionManager.AddToTopic(randomTopic, randomUser);
        await ConnectionManager.RemoveFromTopic(randomTopic, randomUser);

        var members = await ConnectionManager.GetMembersFromTopicId(randomTopic);
        if (members.Contains(randomUser))
            throw new Exception("The topic " + randomTopic + " should not contain the user " + randomUser);
        var topicsFromMemberId = await ConnectionManager.GetTopicsFromMemberId(randomUser);
        if (topicsFromMemberId.Contains(randomTopic))
            throw new Exception("The user " + randomUser + " should not be in the topic " + randomTopic);
    }
}