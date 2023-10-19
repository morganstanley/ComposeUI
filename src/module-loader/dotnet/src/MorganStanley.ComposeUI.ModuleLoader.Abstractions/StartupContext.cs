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

public sealed class StartupContext
{
    private readonly object _lock = new();
    private readonly List<object> _properties = new();

    public StartupContext(StartRequest startRequest, IModuleInstance moduleInstance)
    {
        StartRequest = startRequest;
        ModuleInstance = moduleInstance;
    }

    public StartRequest StartRequest { get; }

    public IModuleInstance ModuleInstance { get; }

    public void AddProperty<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        lock (_lock)
        {
            _properties.Add(value);
        }
    }

    public IEnumerable<object> GetProperties()
    {
        lock (_lock)
        {
            return _properties.ToList().AsReadOnly();
        }
    }
}

public static class StartupContextExtensions
{
    public static IEnumerable<T> GetProperties<T>(this StartupContext startupContext)
    {
        return startupContext.GetProperties().OfType<T>();
    }

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

    public static T GetOrAddProperty<T>(this StartupContext startupContext, Func<T> newValueFactory)
    {
        return GetOrAddProperty<T>(startupContext, _ => newValueFactory());
    }

    public static T GetOrAddProperty<T>(this StartupContext startupContext) where T : class, new()
    {
        return GetOrAddProperty<T>(startupContext, _ => new T());
    }
}
