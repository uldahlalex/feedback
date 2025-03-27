using System.Collections.Concurrent;
using System.Text.Json;
using Application.Interfaces.Infrastructure.Websocket;
using Fleck;
using Infrastructure.Websocket.ConnectionMangerScopedModels;
using Microsoft.Extensions.Logging;
using WebSocketBoilerplate;

namespace Infrastructure.Websocket;

public class WebSocketConnectionManager<TConnection, TMessageBase> : IConnectionManager
    where TConnection : class
    where TMessageBase : class
{
    private readonly ConcurrentDictionary<string /* Client ID */, TConnection> _connectionIdToSocket = new();
    private readonly ILogger<WebSocketConnectionManager<TConnection, TMessageBase>> _logger;

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager<TConnection, TMessageBase>> logger)
    {
        _logger = logger;
    }

    public List<object> GetAllSockets()
    {
        return _connectionIdToSocket.Values.ToList<object>();
    }

    public ConcurrentDictionary<string, object> ConnectionIdToSocket =>
        new(_connectionIdToSocket.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value));

    public ConcurrentDictionary<string, string> SocketToConnectionId { get; } = new();

    public ConcurrentDictionary<string, HashSet<string>> TopicMembers { get; set; } = new();
    public ConcurrentDictionary<string, HashSet<string>> MemberTopics { get; set; } = new();

    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers()
    {
        return Task.FromResult(TopicMembers);
    }

    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics()
    {
        return Task.FromResult(MemberTopics);
    }

    public Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId()
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in _connectionIdToSocket) result[kvp.Key] = GetSocketId(kvp.Value);
        return Task.FromResult(result);
    }

    public Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId()
    {
        return Task.FromResult(SocketToConnectionId.ToDictionary(k => k.Key, v => v.Value));
    }
    public async Task AddToTopic(string topic, string memberId)
    {
        _logger.LogInformation("Adding "+memberId+" to topic "+topic);
        // Add to TopicMembers
        TopicMembers.AddOrUpdate(
            topic,
            new HashSet<string> { memberId },
            (_, existing) =>
            {
                var newSet = new HashSet<string>(existing); // Create new set
                newSet.Add(memberId);
                return newSet;
            });

        // Add to MemberTopics
        MemberTopics.AddOrUpdate(
            memberId,
            new HashSet<string> { topic },
            (_, existing) =>
            {
                var newSet = new HashSet<string>(existing); // Create new set
                newSet.Add(topic);
                return newSet;
            });

        await LogCurrentState();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        // Remove from TopicMembers
        TopicMembers.AddOrUpdate(
            topic,
            new HashSet<string>(), // Should never happen as topic should exist
            (_, existing) =>
            {
                var newSet = new HashSet<string>(existing);
                newSet.Remove(memberId);
                return newSet;
            });

        // Clean up empty topic
        if (TopicMembers.TryGetValue(topic, out var members) && !members.Any())
        {
            TopicMembers.TryRemove(topic, out _);
        }

        // Remove from MemberTopics
        MemberTopics.AddOrUpdate(
            memberId,
            new HashSet<string>(), // Should never happen as member should exist
            (_, existing) =>
            {
                var newSet = new HashSet<string>(existing);
                newSet.Remove(topic);
                return newSet;
            });

        // Clean up empty member
        if (MemberTopics.TryGetValue(memberId, out var topics) && !topics.Any())
        {
            MemberTopics.TryRemove(memberId, out _);
        }

        await LogCurrentState();
    }

    public Task<List<string>> GetMembersFromTopicId(string topic)
    {
        return Task.FromResult(
            TopicMembers.TryGetValue(topic, out var members)
                ? new List<string>(members)
                : new List<string>());
    }

    public Task<List<string>> GetTopicsFromMemberId(string memberId)
    {
        return Task.FromResult(
            MemberTopics.TryGetValue(memberId, out var topics)
                ? new List<string>(topics)
                : new List<string>());
    }

    public Task<string> GetClientIdFromSocketId(string socketId)
    {
        if (SocketToConnectionId.TryGetValue(socketId, out var connectionId)) return Task.FromResult(connectionId);

        throw new KeyNotFoundException($"Could not find client ID for socket ID {socketId}");
    }

    public async Task OnOpen<T>(T socket, string clientId)
    {
        if (socket is not TConnection typedSocket)
            throw new ArgumentException($"Expected socket of type {typeof(TConnection).Name} but got {typeof(T).Name}");

        _logger.LogDebug($"OnOpen called with clientId: {clientId}");

        if (_connectionIdToSocket.TryRemove(clientId, out var oldSocket))
        {
            var oldSocketId = GetSocketId(oldSocket);
            SocketToConnectionId.TryRemove(oldSocketId, out _);
            _logger.LogInformation($"Removed old connection {oldSocketId} for client {clientId}");
        }

        _connectionIdToSocket[clientId] = typedSocket;
        SocketToConnectionId[GetSocketId(typedSocket)] = clientId;

        _logger.LogInformation($"Added new connection {GetSocketId(typedSocket)} for client {clientId}");
        await LogCurrentState();
    }

    public async Task OnClose<T>(T socket, string clientId)
    {
        if (socket is not TConnection typedSocket)
            throw new ArgumentException($"Expected socket of type {typeof(TConnection).Name} but got {typeof(T).Name}");

        var socketId = GetSocketId(typedSocket);
        _logger.LogInformation($"OnClose called with clientId: {clientId} and socketId: {socketId}");


        _connectionIdToSocket.TryRemove(clientId, out _);
        _logger.LogInformation($"Removed connection for client {clientId}");
        SocketToConnectionId.TryRemove(socketId, out _);

        if (MemberTopics.TryGetValue(clientId, out var topics))
            foreach (var topic in new List<string>(topics))
            {
                await RemoveFromTopic(topic, clientId);
                if (TopicMembers.TryGetValue(topic, out var members) && members.Count > 0)
                    await NotifyMemberLeft(topic, clientId);
            }

        MemberTopics.TryRemove(clientId, out _);
        await LogCurrentState();
    }

    public async Task BroadcastToTopic<TMessage>(string topic, TMessage message) where TMessage : class
    {
        await LogCurrentState();

        if (!TopicMembers.TryGetValue(topic, out var members))
        {
            _logger.LogWarning($"No topic found: {topic}");
            return;
        }

        var membersList = new List<string>(members);
        foreach (var memberId in membersList) await BroadcastToMember(topic, memberId, message);
    }


    public async Task LogCurrentState()
    {
        try
        {
            _logger.LogInformation(JsonSerializer.Serialize(new
            {
                ConnectionIdToSocket = await GetAllConnectionIdsWithSocketId(),
                SocketToConnectionId = await GetAllSocketIdsWithConnectionId(),
                TopicsWithMembers = TopicMembers,
                MembersWithTopics = MemberTopics
            }, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging current state");
        }
    }

    protected virtual async Task NotifyMemberLeft(string topic, string memberId)
    {
        var dto = new MemberLeftNotification
        {
            ClientId = memberId,
            Topic = topic
        };

        await BroadcastToTopic(topic, dto);
    }

    private async Task BroadcastToMember<TMessage>(string topic, string memberId, TMessage message)
        where TMessage : class
    {
        if (!_connectionIdToSocket.TryGetValue(memberId, out var socket))
        {
            _logger.LogWarning($"No socket found for member: {memberId}");
            await RemoveFromTopic(topic, memberId);
            return;
        }

        if (!IsSocketAvailable(socket))
        {
            _logger.LogWarning($"Socket not available for {memberId}");
            await RemoveFromTopic(topic, memberId);
            return;
        }

        try
        {
            SendToSocket(socket, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to {memberId}");
            await RemoveFromTopic(topic, memberId);
        }
    }

    protected virtual string GetSocketId(TConnection socket)
    {
        if (socket is IWebSocketConnection webSocket) return webSocket.ConnectionInfo.Id.ToString();

        return socket.GetHashCode().ToString();
    }

    protected virtual bool IsSocketAvailable(TConnection socket)
    {
        if (socket is IWebSocketConnection webSocket) return webSocket.IsAvailable;

        return true;
    }

    protected virtual void SendToSocket<TMessage>(TConnection socket, TMessage message)
        where TMessage : class
    {
        try
        {
            if (socket is IWebSocketConnection webSocket)
            {
                if (message is TMessageBase baseMessage && typeof(TMessageBase) == typeof(BaseDto))
                {
                    dynamic dynamicMessage = baseMessage;
                    webSocket.SendDto((BaseDto)dynamicMessage);
                    _logger.LogInformation($"Sent message to socket {GetSocketId(socket)}");
                    return;
                }

                var json = JsonSerializer.Serialize(message,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                webSocket.Send(json);
                _logger.LogInformation($"Sent JSON message to socket {GetSocketId(socket)}");
                return;
            }

            throw new NotImplementedException(
                $"Sending messages to {typeof(TConnection).Name} with message type {typeof(TMessage).Name} " +
                $"is not implemented. Override SendToSocket in a derived class.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to socket {GetSocketId(socket)}");
            throw;
        }
    }
}