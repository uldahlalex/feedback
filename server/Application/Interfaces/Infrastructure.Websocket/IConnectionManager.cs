using System.Collections.Concurrent;

namespace Application.Interfaces.Infrastructure.Websocket;

public interface IConnectionManager
{
    // Generic connection storage - application layer doesn't know the concrete type
    ConcurrentDictionary<string, object> ConnectionIdToSocket { get; }
    ConcurrentDictionary<string, string> SocketToConnectionId { get; }

    // Topic management
    ConcurrentDictionary<string, HashSet<string>> TopicMembers { get; }
    ConcurrentDictionary<string, HashSet<string>> MemberTopics { get; }

    // Connection management - generic socket type in method parameters
    Task OnOpen<T>(T socket, string clientId);
    Task OnClose<T>(T socket, string clientId);

    // Topic management methods
    Task AddToTopic(string topic, string memberId);
    Task RemoveFromTopic(string topic, string memberId);
    Task<List<string>> GetMembersFromTopicId(string topic);
    Task<List<string>> GetTopicsFromMemberId(string memberId);

    // Messaging
    Task BroadcastToTopic<TMessage>(string topic, TMessage message) where TMessage : class;

    // Queries for state information
    Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers();
    Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics();
    Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId();
    Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId();
    Task<string> GetClientIdFromSocketId(string socketId);

    // Optional - state logging
    Task LogCurrentState();
}