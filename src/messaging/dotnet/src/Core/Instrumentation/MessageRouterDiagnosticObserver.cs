// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging.Instrumentation;

/// <summary>
/// Provides the mechanisms to wait for asynchronous background operations raised by messaging components deterministically.
/// </summary>
public class MessageRouterDiagnosticObserver : IDisposable, IObserver<KeyValuePair<string, object?>>
{
    /// <param name="sender">An optional object which will be used as a filter on the sender of the observed events.</param>
    public MessageRouterDiagnosticObserver(object? sender = null)
    {
        _sender = sender;
        _subscription = MessageRouterDiagnosticSource.Log.Subscribe(this);
    }

    /// <summary>
    ///     Asynchronously waits until all outstanding background operations complete.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Background operations are considered done when the sender has no more work to do, eg.
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 for an incoming <see cref="TopicMessage" />, all subscribers were notified via
    ///                 <see cref="IAsyncObserver{T}.OnNextAsync" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 for <see cref="IAsyncDisposable.DisposeAsync" />, the connection has been disposed and all subscribers
    ///                 were
    ///                 notified via <see cref="IAsyncObserver{T}.OnErrorAsync" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 for <see cref="IMessageRouter.InvokeAsync" />, the response has arrived and the task was completed.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     This method will also wait for a <see cref="MessageRouterEventTypes.RequestStop" /> event for any message
    ///     registered using <see cref="RegisterRequest{TMessage}" />.
    /// </remarks>
    public Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        return _outstandingEvents.WaitAsync(cancellationToken);
    }

    /// <summary>
    ///     Asynchronously waits until all outstanding background operations complete.
    /// </summary>
    /// <param name="timeout">A timeout after which this method will throw a <see cref="TimeoutException" /></param>
    /// <returns></returns>
    /// <inheritdoc cref="WaitForCompletionAsync(System.Threading.CancellationToken)" />
    public async Task WaitForCompletionAsync(TimeSpan timeout)
    {
        if (Debugger.IsAttached)
        {
            timeout = Timeout.InfiniteTimeSpan;
        }

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await WaitForCompletionAsync(cts.Token);
        }
        catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
        {
            lock (_lock)
            {
                throw new TimeoutException(
                    $"The operation has timed out.\nOutstanding events:\n"
                    + string.Join(separator: '\n', _expectedEvents.Select(exp => exp.Description)),
                    e);
            }
        }
    }

    /// <summary>
    ///     Registers a message that is expected to be processed by the sender.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="sender"></param>
    /// <returns>
    ///     The unchanged message.
    /// </returns>
    /// <remarks>
    ///     For each message registered with this method, or logged by the sender using the
    ///     <see cref="MessageRouterEventTypes.RequestStart" /> event type, <see cref="o:WaitForCompletionAsync" /> will wait
    ///     for
    ///     a matching <see cref="MessageRouterEventTypes.RequestStop" /> event.
    /// </remarks>
    public TMessage RegisterRequest<TMessage>(TMessage message, object? sender = null) where TMessage : Message
    {
        sender = ValidateSender(sender);

        lock (_lock)
        {
            var reg = new RegisteredRequest(sender, message);

            if (!_registeredRequests.Add(reg))
            {
                return message;
            }

            AddExpectedEvent(
                evt =>
                {
                    var result = evt.Type == MessageRouterEventTypes.RequestStop
                                 && evt.Sender == sender
                                 && evt.Message == message;

                    if (result)
                    {
                        _registeredRequests.Remove(reg);
                    }

                    return result;
                },
                $"{MessageRouterEventTypes.RequestStop}: {typeof(TMessage).Name}");
        }

        return message;
    }

    /// <summary>
    ///     Registers a message that is expected to be sent by the source.
    /// </summary>
    /// <param name="predicate">A predicate used for recognising the messages</param>
    /// <param name="sender">The expected sender, if it differs from the one provided in the constructor of the current object</param>
    /// <remarks>
    ///     <see cref="o:WaitForCompletionAsync" /> will wait for expected messages to be sent by the source.
    /// </remarks>
    public void ExpectMessage(Predicate<Message> predicate, object? sender = null)
    {
        sender = ValidateSender(sender);

        AddExpectedEvent(
            evt => evt is {Type: MessageRouterEventTypes.MessageSent, Message: not null}
                   && evt.Sender == sender
                   && predicate(evt.Message),
            $"{MessageRouterEventTypes.MessageSent}: <predicate>");
    }

    /// <summary>
    ///     Registers a message of type <typeparamref name="TMessage" /> that is expected to be sent by the source.
    /// </summary>
    /// <param name="predicate">A predicate used for recognising the messages</param>
    /// <param name="sender"></param>
    /// <remarks>
    ///     <see cref="o:WaitForCompletionAsync" /> will wait for expected messages to be sent by the source.
    /// </remarks>
    public void ExpectMessage<TMessage>(Predicate<TMessage>? predicate = null, object? sender = null) where TMessage : Message
    {
        sender = ValidateSender(sender);

        Predicate<MessageRouterEvent> innerPredicate =
            predicate is null
                ? evt => evt is {Type: MessageRouterEventTypes.MessageSent, Message: TMessage} 
                         && evt.Sender == sender
                : evt => evt is {Type: MessageRouterEventTypes.MessageSent, Message: TMessage}
                         && evt.Sender == sender
                         && predicate((TMessage) evt.Message);

        AddExpectedEvent(innerPredicate, $"{MessageRouterEventTypes.MessageSent}: {typeof(TMessage).Name}");
    }

    /// <summary>
    ///     Adds an expected event to wait for when <see cref="o:WaitForCompletionAsync" /> is called.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="sender"></param>
    public void ExpectEvent(string eventType, object? sender = null)
    {
        sender = ValidateSender(sender);

        AddExpectedEvent(
            evt => evt.Sender == sender && evt.Type == eventType,
            eventType);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subscription.Dispose();
    }

    void IObserver<KeyValuePair<string, object?>>.OnCompleted()
    {
        lock (_lock)
        {
            _outstandingEvents.Signal(_outstandingEvents.CurrentCount);
        }
    }

    void IObserver<KeyValuePair<string, object?>>.OnError(Exception error)
    {
        lock (_lock)
        {
            _outstandingEvents.Signal(_outstandingEvents.CurrentCount);
        }
    }

    void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value is not MessageRouterEvent evt)
        {
            return;
        }

        lock (_lock)
        {
            if (TryRemoveExpectedEvent(evt))
            {
                return;
            }

            switch (evt.Type)
            {
                case MessageRouterEventTypes.RequestStart:
                    ArgumentNullException.ThrowIfNull(evt.Message);
                    RegisterRequest(evt.Message, evt.Sender);
                    break;

                case MessageRouterEventTypes.CloseStart:
                    ExpectEvent(MessageRouterEventTypes.CloseStop, evt.Sender);
                    break;

                case MessageRouterEventTypes.ConnectStart:
                    ExpectEvent(MessageRouterEventTypes.ConnectStop, evt.Sender);
                    break;
            }
        }
    }

    private readonly object? _sender;

    private object ValidateSender(object? sender, [CallerMemberName] string? callerName = null) => 
        sender ?? _sender
        ?? throw new ArgumentNullException(
            nameof(sender),
            $"sender must be specified either for the constructor of {nameof(MessageRouterDiagnosticObserver)} or {callerName}");

    private void AddExpectedEvent(Predicate<MessageRouterEvent> predicate, string description)
    {
        lock (_lock)
        {
            _expectedEvents.Add(new Expectation(predicate, description));
            _outstandingEvents.AddCount();
        }
    }

    private bool TryRemoveExpectedEvent(MessageRouterEvent evt)
    {
        lock (_lock)
        {
            var index = _expectedEvents.FindIndex(expectation => expectation.Predicate(evt));
            if (index < 0)
            {
                return false;
            }

            _expectedEvents.RemoveAt(index);
            _outstandingEvents.Signal();

            return true;
        }
    }

    private readonly object _lock = new();
    private readonly IDisposable _subscription;
    private readonly AsyncCountdownEvent _outstandingEvents = new(0);
    private readonly HashSet<RegisteredRequest> _registeredRequests = [];
    private readonly List<Expectation> _expectedEvents = [];

    private sealed record RegisteredRequest(object Sender, Message Message);

    private sealed class Expectation(Predicate<MessageRouterEvent> predicate, string description)
    {
        public Predicate<MessageRouterEvent> Predicate { get; } = predicate;
        public string Description { get; } = description;
    }
}