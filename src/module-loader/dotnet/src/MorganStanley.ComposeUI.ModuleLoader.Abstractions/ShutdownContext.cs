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

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Provides contextual information and property storage for a module during its shutdown process.
/// </summary>
public sealed class ShutdownContext
{
    private readonly object _lock = new();
    private readonly List<object> _properties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownContext"/> class.
    /// </summary>
    /// <param name="moduleInstance">The module instance being shut down.</param>
    public ShutdownContext(IModuleInstance moduleInstance)
    {
        ModuleInstance = moduleInstance;
    }

    /// <summary>
    /// Gets the module instance associated with this shutdown context.
    /// </summary>
    public IModuleInstance ModuleInstance { get; }

    /// <summary>
    /// Adds a property to the shutdown context.
    /// </summary>
    /// <typeparam name="T">The type of the property to add.</typeparam>
    /// <param name="value">The property value to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public void AddProperty<T>(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        lock (_lock)
        {
            _properties.Add(value);
        }
    }

    /// <summary>
    /// Gets all properties added to the shutdown context.
    /// </summary>
    /// <returns>An enumerable collection of all properties.</returns>
    public IEnumerable<object> GetProperties()
    {
        lock (_lock)
        {
            return _properties.AsReadOnly();
        }
    }
}

/// <summary>
/// Provides extension methods for working with <see cref="ShutdownContext"/> properties.
/// </summary>
public static class ShutdownContextExtensions
{
    /// <summary>
    /// Gets the first property of the specified type from the shutdown context, or <c>null</c> if none is found.
    /// </summary>
    /// <typeparam name="T">The type of the property to retrieve.</typeparam>
    /// <param name="shutdownContext">The shutdown context instance.</param>
    /// <returns>The first property of type <typeparamref name="T"/>, or <c>null</c> if not found.</returns>
    public static T? GetProperty<T>(this ShutdownContext shutdownContext)
    {
        return shutdownContext.GetProperties<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets all properties of the specified type from the shutdown context.
    /// </summary>
    /// <typeparam name="T">The type of the properties to retrieve.</typeparam>
    /// <param name="shutdownContext">The shutdown context instance.</param>
    /// <returns>An enumerable collection of properties of type <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> GetProperties<T>(this ShutdownContext shutdownContext)
    {
        return shutdownContext.GetProperties().OfType<T>();
    }
}
