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

using MorganStanley.ComposeUI.Messaging.Protocol;

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
///     Represents an exception thrown by the Message Router client. Errors thrown by other
///     modules as well as the server can also manifest in this exception type on the client side.
/// </summary>
public class MessageRouterException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="MessageRouterException"/> with the provided error name and message.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    public MessageRouterException(string name, string message) : base(message)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new instance of <see cref="MessageRouterException"/> from the provided <see cref="Error"/> object.
    /// </summary>
    /// <param name="error"></param>
    public MessageRouterException(Error error) : this(error.Name, error.Message ?? error.Name) { }

    /// <summary>
    ///     Gets the machine-friendly name that identifies the error.
    /// </summary>
    /// <remarks>
    ///     Predefined error names are kept in the <see cref="MessageRouterErrors" /> class.
    /// </remarks>
    public string Name { get; }
}
