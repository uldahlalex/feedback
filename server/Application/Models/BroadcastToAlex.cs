using Core.Domain.Entities;

namespace Application.Models;

public class BroadcastToAlex
{
    public string EventType { get; set; } = nameof(BroadcastToAlex);
    public Question Question { get; set; }
}