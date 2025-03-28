using System.Collections.Concurrent;

namespace Application.Interfaces.Infrastructure.Websocket;

public interface IConnectionManager
{
    Task OnOpen(object socket, string clientId);
    Task OnClose(object socket, string clientId);
    Task AddToTopic(string topic, string memberId);
    Task RemoveFromTopic(string topic, string memberId);
    Task BroadcastToTopic<TMessage>(string topic, TMessage message) where TMessage : class;
    Task<List<string>> GetMembersFromTopicId(string topic);
    Task<List<string>> GetTopicsFromMemberId(string memberId);
}