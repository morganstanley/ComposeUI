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

using System.Text.Json.Serialization;

namespace ComposeUI.Messaging.Core.Messages;

public abstract class Message
{
    [JsonPropertyOrder(0)]
    public abstract MessageType Type { get; }

    public static Type ResolveMessageType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Connect => typeof(ConnectRequest),
            MessageType.ConnectResponse => typeof(ConnectResponse),
            MessageType.Subscribe => typeof(SubscribeMessage),
            MessageType.Unsubscribe => typeof(UnsubscribeMessage),
            MessageType.Publish => typeof(PublishMessage),
            MessageType.Update => typeof(UpdateMessage),
            MessageType.Invoke => typeof(InvokeRequest),
            MessageType.RegisterService => typeof(RegisterServiceRequest),
            MessageType.InvokeResponse => typeof(InvokeResponse),
            MessageType.RegisterServiceResponse => typeof(RegisterServiceResponse),
            MessageType.UnregisterService => typeof(UnregisterServiceMessage),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}