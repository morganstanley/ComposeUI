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
/// Provides contextual information and property storage for a module during its startup process.
/// </summary>
public sealed class StartupContext
{
    private readonly object _lock = new();
    private readonly List<object> _properties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupContext"/> class.
    /// </summary>
    /// <param name="startRequest">The request containing the module identifier and startup parameters.</param>
    /// <param name="moduleInstance">The module instance being started.</param>
    public StartupContext(StartRequest startRequest, IModuleInstance moduleInstance)
    {
        StartRequest = startRequest;
        ModuleInstance = moduleInstance;
    }

    /// <summary>
    /// Gets the request containing the module identifier and startup parameters.
    /// </summary>
    public StartRequest StartRequest { get; }

    /// <summary>
    /// Gets the module instance associated with this startup context.
    /// </summary>
    public IModuleInstance ModuleInstance { get; }

    /// <summary>
    /// Adds a property to the startup context.
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
    /// Gets all properties added to the startup context.
    /// </summary>
    /// <returns>An enumerable collection of all properties.</returns>
    public IEnumerable<object> GetProperties()
    {
        lock (_lock)
        {
            return _properties.ToList().AsReadOnly();
        }
    }
}

/// <summary>
/// Provides extension methods for working with <see cref="StartupContext"/> properties.
/// </summary>
public static class StartupContextExtensions
{
    /// <summary>
    /// Gets all properties of the specified type from the startup context.
    /// </summary>
    /// <typeparam name="T">The type of the properties to retrieve.</typeparam>
    /// <param name="startupContext">The startup context instance.</param>
    /// <returns>An enumerable collection of properties of type <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> GetProperties<T>(this StartupContext startupContext)
    {
        return startupContext.GetProperties().OfType<T>();
    }

    /// <summary>
    /// Gets the first property of the specified type from the startup context, or adds a new one using the provided factory if none exists.
    /// </summary>
    /// <typeparam name="T">The type of the property to retrieve or add.</typeparam>
    /// <param name="startupContext">The startup context instance.</param>
    /// <param name="newValueFactory">A factory function to create a new property if one does not exist.</param>
    /// <returns>The existing or newly added property of type <typeparamref name="T"/>.</returns>
    public static T GetOrAddProperty<T>(this StartupContext startupContext, Func<StartupContext, T> newValueFactory)
    {
        var property = startupContext.GetProperties<T>().FirstOrDefault();

        if (property == null)
        {
            property = newValueFactory(startupContext);
            startupContext.AddProperty(property);
        }

        return property;
    }

    /// <summary>
    /// Gets the first property of the specified type from the startup context, or adds a new one using the provided factory if none exists.
    /// </summary>
    /// <typeparam name="T">The type of the property to retrieve or add.</typeparam>
    /// <param name="startupContext">The startup context instance.</param>
    /// <param name="newValueFactory">A factory function to create a new property if one does not exist.</param>
    /// <returns>The existing or newly added property of type <typeparamref name="T"/>.</returns>
    public static T GetOrAddProperty<T>(this StartupContext startupContext, Func<T> newValueFactory)
    {
        return GetOrAddProperty<T>(startupContext, _ => newValueFactory());
    }

    /// <summary>
    /// Gets the first property of the specified type from the startup context, or adds a new instance if none exists.
    /// </summary>
    /// <typeparam name="T">The type of the property to retrieve or add. Must have a parameterless constructor.</typeparam>
    /// <param name="startupContext">The startup context instance.</param>
    /// <returns>The existing or newly added property of type <typeparamref name="T"/>.</returns>
    public static T GetOrAddProperty<T>(this StartupContext startupContext) where T : class, new()
    {
        return GetOrAddProperty<T>(startupContext, _ => new T());
    }
}
