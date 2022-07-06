namespace ComposeUI.Messaging.Core.Messages;

public enum MessageType : int
{
    /// <summary>
    ///     Client wants to connect
    /// </summary>
    Connect,

    /// <summary>
    ///     Server accepted the connection
    /// </summary>
    ConnectResponse,

    /// <summary>
    ///     Client subscribes to a topic
    /// </summary>
    Subscribe,

    /// <summary>
    ///     Client unsubscribes from a topic
    /// </summary>
    Unsubscribe,

    /// <summary>
    ///     Client publishes a message to a topic
    /// </summary>
    Publish,

    /// <summary>
    ///     Server notifies client of a message from a subscribed topic
    /// </summary>
    Update,

    /// <summary>
    ///     Client registers an invokable service
    /// </summary>
    RegisterService,

    /// <summary>
    ///     Server responds to a RegisterServiceRequest message
    /// </summary>
    RegisterServiceResponse,

    /// <summary>
    ///     Client invokes a service, or the server notifies the registered service of an invocation.
    /// </summary>
    Invoke,

    /// <summary>
    ///     Service sends the response of an invocation to server, or server sends the result of a service invocation to the
    ///     caller
    /// </summary>
    InvokeResponse
}