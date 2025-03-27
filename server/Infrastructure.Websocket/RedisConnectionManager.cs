using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;
using Application.Interfaces.Infrastructure.Websocket;
using Fleck;
using Infrastructure.Websocket.ConnectionMangerScopedModels;
using Microsoft.Extensions.Logging;
using WebSocketBoilerplate;

public class RedisConnectionManager<TConnection, TMessageBase> : IConnectionManager
    where TConnection : class
    where TMessageBase : class
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisConnectionManager<TConnection, TMessageBase>> _logger;
    private readonly ConcurrentDictionary<string, TConnection> _connectionIdToSocket = new();
    private readonly string _instanceId;

    public RedisConnectionManager(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionManager<TConnection, TMessageBase>> logger)
    {
        _redis = redis;
        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _instanceId = Guid.NewGuid().ToString();
        
        // Subscribe to broadcast messages
        _subscriber.Subscribe($"broadcast:{_instanceId}", OnBroadcastMessage);
    }

    public ConcurrentDictionary<string, object> ConnectionIdToSocket =>
        new(_connectionIdToSocket.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value));

    public ConcurrentDictionary<string, string> SocketToConnectionId { get; } = new();

    public ConcurrentDictionary<string, HashSet<string>> TopicMembers => 
        GetTopicMembersFromRedis().Result;

    public ConcurrentDictionary<string, HashSet<string>> MemberTopics =>
        GetMemberTopicsFromRedis().Result;

    public List<object> GetAllSockets()
    {
        return _connectionIdToSocket.Values.ToList<object>();
    }

    public async Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers()
    {
        return await GetTopicMembersFromRedis();
    }

    public async Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics()
    {
        return await GetMemberTopicsFromRedis();
    }

    public async Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId()
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in _connectionIdToSocket)
        {
            result[kvp.Key] = GetSocketId(kvp.Value);
        }
        return result;
    }

    public async Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId()
    {
        return SocketToConnectionId.ToDictionary(k => k.Key, v => v.Value);
    }

    public async Task<string> GetClientIdFromSocketId(string socketId)
    {
        if (SocketToConnectionId.TryGetValue(socketId, out var connectionId))
        {
            return connectionId;
        }
        throw new KeyNotFoundException($"Could not find client ID for socket ID {socketId}");
    }

    public async Task OnOpen<T>(T socket, string clientId)
    {
        if (socket is not TConnection typedSocket)
        {
            throw new ArgumentException($"Expected socket of type {typeof(TConnection).Name} but got {typeof(T).Name}");
        }

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
        {
            throw new ArgumentException($"Expected socket of type {typeof(TConnection).Name} but got {typeof(T).Name}");
        }

        var socketId = GetSocketId(typedSocket);
        _logger.LogInformation($"OnClose called with clientId: {clientId} and socketId: {socketId}");

        _connectionIdToSocket.TryRemove(clientId, out _);
        SocketToConnectionId.TryRemove(socketId, out _);

        var memberTopics = await GetTopicsFromMemberId(clientId);
        foreach (var topic in memberTopics)
        {
            await RemoveFromTopic(topic, clientId);
            var members = await GetMembersFromTopicId(topic);
            if (members.Count > 0)
            {
                await NotifyMemberLeft(topic, clientId);
            }
        }

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member:{clientId}");
        await db.SetRemoveAsync("members", clientId);

        await LogCurrentState();
    }

    public async Task AddToTopic(string topic, string memberId)
    {
        var db = _redis.GetDatabase();
        
        await db.SetAddAsync("topics", topic);
        await db.SetAddAsync("members", memberId);
        await db.SetAddAsync($"topic:{topic}", memberId);
        await db.SetAddAsync($"member:{memberId}", topic);
        
        await LogCurrentState();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var db = _redis.GetDatabase();
        
        await db.SetRemoveAsync($"topic:{topic}", memberId);
        await db.SetRemoveAsync($"member:{memberId}", topic);
        
        if (await db.SetLengthAsync($"topic:{topic}") == 0)
        {
            await db.KeyDeleteAsync($"topic:{topic}");
            await db.SetRemoveAsync("topics", topic);
        }
        
        if (await db.SetLengthAsync($"member:{memberId}") == 0)
        {
            await db.KeyDeleteAsync($"member:{memberId}");
            await db.SetRemoveAsync("members", memberId);
        }
        
        await LogCurrentState();
    }

    public async Task<List<string>> GetMembersFromTopicId(string topic)
    {
        var db = _redis.GetDatabase();
        var members = await db.SetMembersAsync($"topic:{topic}");
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<List<string>> GetTopicsFromMemberId(string memberId)
    {
        var db = _redis.GetDatabase();
        var topics = await db.SetMembersAsync($"member:{memberId}");
        return topics.Select(t => t.ToString()).ToList();
    }

    public async Task BroadcastToTopic<TMessage>(string topic, TMessage message) where TMessage : class
    {
        var messageWrapper = new BroadcastMessage
        {
            Topic = topic,
            Message = JsonSerializer.Serialize(message),
            MessageType = typeof(TMessage).AssemblyQualifiedName
        };

        var serializedMessage = JsonSerializer.Serialize(messageWrapper);
        await _subscriber.PublishAsync($"broadcast:{_instanceId}", serializedMessage);
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

    private async Task<ConcurrentDictionary<string, HashSet<string>>> GetTopicMembersFromRedis()
    {
        var db = _redis.GetDatabase();
        var result = new ConcurrentDictionary<string, HashSet<string>>();
        var topics = await db.SetMembersAsync("topics");

        foreach (var topic in topics)
        {
            var members = await db.SetMembersAsync($"topic:{topic}");
            result[topic.ToString()] = new HashSet<string>(members.Select(m => m.ToString()));
        }

        return result;
    }

    private async Task<ConcurrentDictionary<string, HashSet<string>>> GetMemberTopicsFromRedis()
    {
        var db = _redis.GetDatabase();
        var result = new ConcurrentDictionary<string, HashSet<string>>();
        var members = await db.SetMembersAsync("members");

        foreach (var member in members)
        {
            var topics = await db.SetMembersAsync($"member:{member}");
            result[member.ToString()] = new HashSet<string>(topics.Select(t => t.ToString()));
        }

        return result;
    }

    private void OnBroadcastMessage(RedisChannel channel, RedisValue message)
    {
        try
        {
            var broadcastMessage = JsonSerializer.Deserialize<BroadcastMessage>(message.ToString());
            var messageType = Type.GetType(broadcastMessage.MessageType);
            var deserializedMessage = JsonSerializer.Deserialize(broadcastMessage.Message, messageType);

            var members = GetMembersFromTopicId(broadcastMessage.Topic).Result;
            foreach (var memberId in members)
            {
                if (_connectionIdToSocket.TryGetValue(memberId, out var socket))
                {
                    SendToSocket(socket, deserializedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing broadcast message");
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

    protected virtual string GetSocketId(TConnection socket)
    {
        if (socket is IWebSocketConnection webSocket)
        {
            return webSocket.ConnectionInfo.Id.ToString();
        }
        return socket.GetHashCode().ToString();
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
                "is not implemented. Override SendToSocket in a derived class.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to socket {GetSocketId(socket)}");
            throw;
        }
    }

    private class BroadcastMessage
    {
        public string Topic { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
    }
}