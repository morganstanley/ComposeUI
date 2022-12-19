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

namespace MorganStanley.ComposeUI.Messaging.Protocol.Messages;

public sealed class PublishMessage : Message
{
    public PublishMessage()
    {
    }

    public PublishMessage(string topic, Utf8Buffer? payload, MessageScope scope)
    {
        Topic = topic;
        Payload = payload;
        Scope = scope;
    }

    public override MessageType Type => MessageType.Publish;
    public string Topic { get; init; } = null!;
    public Utf8Buffer? Payload { get; init; }
    public MessageScope Scope { get; init; }
}