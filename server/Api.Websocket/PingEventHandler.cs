using Fleck;
using WebSocketBoilerplate;

namespace Api.Websocket;

public class Ping : BaseDto;

public class Pong : BaseDto;

public class PingEventHandler : BaseEventHandler<Ping>
{
    public override Task Handle(Ping dto, IWebSocketConnection socket)
    {
        socket.SendDto(new Pong());
        return Task.CompletedTask;
    }
}