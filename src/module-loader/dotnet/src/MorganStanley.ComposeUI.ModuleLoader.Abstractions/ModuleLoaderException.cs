/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using System.Runtime.Serialization;

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Represents errors that occur during module loading operations.
/// </summary>
public class ModuleLoaderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoaderException"/> class.
    /// </summary>
    public ModuleLoaderException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoaderException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The object that holds the serialized object data.</param>
    /// <param name="context">The contextual information about the source or destination.</param>
    protected ModuleLoaderException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoaderException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ModuleLoaderException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoaderException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ModuleLoaderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
