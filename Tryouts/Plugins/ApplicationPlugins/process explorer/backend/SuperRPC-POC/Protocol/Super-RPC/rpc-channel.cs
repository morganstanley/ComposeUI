using System;
namespace Super.RPC;

public interface IRPCChannel { }
public interface IRPCSendSyncChannel : IRPCChannel
{
    /**
    * Sends a message and returns the response synchronously.
    */
    object? SendSync(RPC_Message message);
}

public interface IRPCSendAsyncChannel : IRPCChannel
{
    /**
    * Sends a message asnychronously. The response will come via the `receive` callback function.
    */
    void SendAsync(RPC_Message message);
}

public record MessageReceivedEventArgs(RPC_Message message, IRPCChannel? replyChannel = null, object? context = null);

public interface IRPCReceiveChannel : IRPCChannel
{
    /**
    * Event for when an async message arrives.
    */
    event EventHandler<MessageReceivedEventArgs> MessageReceived;
}

/*** Helper implementations ***/
public record RPCSendAsyncChannel(Action<RPC_Message> sendAsync) : IRPCSendAsyncChannel
{
    public void SendAsync(RPC_Message message)
    {
        sendAsync(message);
    }
}

public record RPCReceiveChannel : IRPCReceiveChannel
{
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public void Received(RPC_Message message, IRPCChannel? replyChannel = null, object? context = null, object? sender = null)
    {
        MessageReceived?.Invoke(sender ?? this, new MessageReceivedEventArgs(message, replyChannel, context));
    }
}

public record RPCSendAsyncAndReceiveChannel(Action<RPC_Message> sendAsync) : RPCReceiveChannel, IRPCSendAsyncChannel
{
    public void SendAsync(RPC_Message message)
    {
        sendAsync(message);
    }
}

public record RPCSendSyncAndReceiveChannel(Func<RPC_Message, object?> sendSync) : RPCReceiveChannel, IRPCSendSyncChannel
{
    public object? SendSync(RPC_Message message)
    {
        return sendSync(message);
    }
}

public record RPCSendSyncAsyncReceiveChannel(Func<RPC_Message, object?> sendSync, Action<RPC_Message> sendAsync) : RPCSendAsyncAndReceiveChannel(sendAsync), IRPCSendSyncChannel
{
    public object? SendSync(RPC_Message message)
    {
        return sendSync(message);
    }
}
