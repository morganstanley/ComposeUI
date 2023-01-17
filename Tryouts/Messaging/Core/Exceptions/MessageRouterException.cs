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

using System.Reflection;
using System.Runtime.Serialization;
using MorganStanley.ComposeUI.Messaging.Protocol;

namespace MorganStanley.ComposeUI.Messaging.Exceptions;

// TODO: Get rid of derived types and declare an enumeration for
// well known errors instead. This is easier to implement in other languages.
// Also add client-side error codes and throw respectively on disconnect, etc.

public class MessageRouterException : Exception
{
    public static MessageRouterException FromProtocolError(Error error)
    {
        var exceptionType = Assembly.GetExecutingAssembly().GetType(error.Type);

        if (exceptionType is { IsAbstract: false }
            && typeof(MessageRouterException).IsAssignableFrom(exceptionType))
        {
            return (MessageRouterException)Activator.CreateInstance(exceptionType, error)!;
        }

        return new MessageRouterException(error);
    }

    public MessageRouterException() { }

    protected MessageRouterException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public MessageRouterException(string? message) : base(message) { }

    public MessageRouterException(string? message, Exception? innerException) : base(message, innerException) { }

    public MessageRouterException(Error error) : base(
        $"An exception of type '{error.Type}' was thrown by a remote client: {error.Message}") { }
}
