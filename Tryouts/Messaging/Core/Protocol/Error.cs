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

namespace MorganStanley.ComposeUI.Messaging.Protocol;

// TODO: Define well-known, language-agnostic error codes and use them consistently
// in the Error object and in MessageRouterException

/// <summary>
///     Represents an error that can be marshaled between Message Router clients.
/// </summary>
public record Error
{
    /// <summary>
    /// Creates a new, empty <see cref="Error"/> object (used only when deserializing)
    /// </summary>
    public Error()
    {
    }

    /// <summary>
    ///     Creates a new <see cref="Error"/> object.
    /// </summary>
    /// <param name="type">
    ///     The type of the error. This can be any value that uniquely identifies an error type.
    /// </param>
    /// <param name="message">
    ///     The error message, if any.
    /// </param>
    public Error(string type, string? message)
    {
        Type = type;
        Message = message;
    }

    /// <summary>
    ///     Creates a new instance from an <see cref="Exception" /> object.
    /// </summary>
    /// <param name="exception"></param>
    public Error(Exception exception) : this(exception.GetType().FullName ?? nameof(Exception), exception.Message) { }

    /// <summary>
    ///     The type of the error. This can be any value that uniquely identifies an error type.
    /// </summary>
    public string Type { get; init; } = null!;

    /// <summary>
    ///     The error message, if any.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Deconstructs the object into type and message.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message"></param>
    public void Deconstruct(out string type, out string? message)
    {
        type = Type;
        message = Message;
    }
}
