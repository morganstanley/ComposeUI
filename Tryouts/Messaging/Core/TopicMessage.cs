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

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
///     Represents a message received from a topic.
///     This type encapsulates the payload and any additional information available about the message.
/// </summary>
public sealed class TopicMessage
{
    /// <summary>
    /// Creates a new <see cref="TopicMessage"/> instance.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="context"></param>
    public TopicMessage(string topic, MessageBuffer? payload, MessageContext context)
    {
        Topic = topic;
        Payload = payload;
        Context = context;
    }

    /// <summary>
    ///     The topic name of the message.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    ///     The payload of the message. The format of the message is arbitrary and should
    ///     be defined and documented with the message definition.
    /// </summary>
    public MessageBuffer? Payload { get; }

    /// <summary>
    /// Gets contextual information about the message.
    /// </summary>
    public MessageContext Context { get; }
}
