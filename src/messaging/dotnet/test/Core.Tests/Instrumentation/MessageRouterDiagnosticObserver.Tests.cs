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

using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

namespace MorganStanley.ComposeUI.Messaging.Instrumentation;

using TopicMessage = Protocol.Messages.TopicMessage;

public class MessageRouterDiagnosticObserverTests
{
    [Fact]
    public async Task Wait_in_initial_state()
    {
        await _observer.WaitForCompletionAsync().WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_ConnectStop_after_ConnectStart()
    {
        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.ConnectStart));
        
        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();
        
        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.ConnectStop));
        await task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_CloseStop_after_CloseStart()
    {
        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.CloseStart));

        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.CloseStop));
        await task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_RequestStop_after_RequestStart()
    {
        var message = new TopicMessage();
        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStart, message));

        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStop, message));
        await task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_RequestStop_after_calling_RegisterRequest()
    {
        var message1 = new TopicMessage();
        _observer.RegisterRequest(message1);
        var message2 = new TopicMessage();
        _observer.RegisterRequest(message2);

        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStart, message1));
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStart, message2));
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStop, message1));
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.RequestStop, message2));
        
        await task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_MessageSent_after_calling_ExpectMessage()
    {
        var message = new InvokeRequest();
        _observer.ExpectMessage(msg => msg == message);

        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent, message));

        await task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task Wait_for_expected_events_to_be_written()
    {
        _observer.ExpectEvent(MessageRouterEventTypes.MessageSent);
        _observer.ExpectEvent(MessageRouterEventTypes.MessageSent);

        var task = _observer.WaitForCompletionAsync();
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent));
        task.IsCompleted.Should().BeFalse();

        WriteEvent(new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent));

        await task.WaitAsync(TestTimeout);
    }

    // TODO: Add test cases when the sender does not match the one specified in constructor

    public MessageRouterDiagnosticObserverTests()
    {
        _observer = new MessageRouterDiagnosticObserver(this);
    }

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(1);
    private readonly MessageRouterDiagnosticObserver _observer;

    private void WriteEvent(MessageRouterEvent evt)
    {
        MessageRouterDiagnosticSource.Log.Write(evt.Type, evt);
    }
}