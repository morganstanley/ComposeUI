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

namespace MorganStanley.ComposeUI.Messaging.Abstractions.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a duplicate endpoint registration is attempted in the Message Router client.
/// </summary>
public class DuplicateServiceNameException : MessagingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingAdapterDuplicateEndpointException"/> class with a specified error name and message.
    /// </summary>
    /// <param name="name">The name of the error.</param>
    /// <param name="message">The message that describes the error.</param>
    public DuplicateServiceNameException(string name, string message)
        : base(name, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingAdapterDuplicateEndpointException"/> class with a specified error name, message, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="name">The name of the error.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DuplicateServiceNameException(string name, string message, Exception innerException)
        : base(name, message, innerException)
    {
    }
}
