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
public sealed class ShutdownContext
{
    private readonly object _lock = new object();
    private readonly List<object> _properties = new();

    public ShutdownContext(IModuleInstance moduleInstance)
    {
        ModuleInstance = moduleInstance;
    }

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
            return _properties.AsReadOnly();
        }
    }
}

public static class ShutdownContextExtensions
{

    public static T? GetProperty<T>(this ShutdownContext shutdownContext)
    {
        return shutdownContext.GetProperties<T>().FirstOrDefault();
    }

    public static IEnumerable<T> GetProperties<T>(this ShutdownContext shutdownContext)
    {
        return shutdownContext.GetProperties().OfType<T>();
    }
}
