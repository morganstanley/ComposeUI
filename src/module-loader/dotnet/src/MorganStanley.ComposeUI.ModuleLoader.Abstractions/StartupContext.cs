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

using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MorganStanley.ComposeUI.ModuleLoader;

public sealed class StartupContext
{
    private readonly ConcurrentDictionary<Type, ImmutableList<object>> _properties = new();

    public StartupContext(StartRequest startRequest)
    {
        StartRequest = startRequest;
    }

    public StartRequest StartRequest { get; }

    public void AddProperty<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        _properties.AddOrUpdate(typeof(T),
            (_) => ImmutableList<object>.Empty.Add(value),
            (_, list) => list.Add(value));
    }

    public IEnumerable<object> GetProperties()
    {
        return _properties.Values.SelectMany(values => values).ToImmutableList();
    }

    public IEnumerable<T> GetProperties<T>()
    {
        if (!_properties.TryGetValue(typeof(T), out var list))
        {
            return Enumerable.Empty<T>();
        }

        return list.OfType<T>();
    }
}
