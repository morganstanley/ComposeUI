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
///     Identifies a messaging scope. Scopes determine which clients receive a published message
///     or an invocation where the target is explicitly defined by the caller.
/// </summary>
/// <remarks>
///     The default value of this type identifies the application-level scope (<see cref="MessageScope.Default" />.
/// </remarks>
// Internal notes
// The fields and semantics of this type can change over time. At the moment, it is just a string value,
// but later we might add more detail to make dispatching more efficient, or if we define a different internal format.
// Applications should handle these values as opaque objects representing scopes where equality and ordering is defined
// (ordering is merely for efficiency in case the scope is used in a data structure that demands sortable keys).
// It is a struct so that default values can be added as optional parameters and passed around efficiently.
public readonly record struct MessageScope : IEquatable<MessageScope>, IComparable<MessageScope>
{
    /// <summary>
    ///     Returns a string representation of the value that can be passed to <see cref="Parse" />.
    /// </summary>
    /// <returns></returns>
    public string AsString()
    {
        return _name ?? "";
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _name ?? "";
    }

    /// <inheritdoc />
    public int CompareTo(MessageScope other)
    {
        return string.Compare(_name, other._name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a bool indicating if the scope is a specific client ID.
    /// </summary>
    public bool IsClientId => _name != null && _name.StartsWith('@');

    /// <summary>
    /// Gets the client ID from the scope, or <value>null</value> if it is not a client ID.
    /// </summary>
    /// <returns></returns>
    public string? GetClientId() => IsClientId ? _name![1..] : null;

    /// <summary>
    ///     Identifies the default, application-level scope. This is equivalent of using <code>default(MessageScope)</code>
    /// </summary>
    public static readonly MessageScope Default = default;

    /// <summary>
    ///     Alias for <see cref="Default" />.
    /// </summary>
    public static readonly MessageScope Application = Default;

    /// <summary>
    /// Creates a scope from a specific client ID.
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public static MessageScope FromClientId(string clientId)
    {
        return new MessageScope("@" + clientId);
    }

    /// <summary>
    ///     Parses a messaging scope serialized to string.
    /// </summary>
    /// <param name="stringValue"></param>
    /// <returns></returns>
    /// <remarks>
    /// <value>null</value> and the empty string are both parsed as the default scope.
    /// </remarks>
    public static MessageScope Parse(string stringValue)
    {
        return string.IsNullOrEmpty(stringValue) ? Default : new MessageScope(stringValue);
    }

    private readonly string? _name;

    private MessageScope(string name)
    {
        _name = string.IsNullOrEmpty(name) ? null : name;
    }
}


