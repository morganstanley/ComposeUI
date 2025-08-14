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
/// Represents a base class for events related to the lifetime of a module instance.
/// </summary>
public abstract class LifetimeEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LifetimeEvent"/> class.
    /// </summary>
    /// <param name="instance">The module instance associated with the event.</param>
    protected LifetimeEvent(IModuleInstance instance)
    {
        Instance = instance;
    }

    /// <summary>
    /// Gets the module instance associated with the event.
    /// </summary>
    public IModuleInstance Instance { get; }

    /// <summary>
    /// Gets the type of the lifetime event.
    /// </summary>
    public abstract LifetimeEventType EventType { get; }

    /// <summary>
    /// Represents an event indicating that a module instance is starting.
    /// </summary>
    public sealed class Starting : LifetimeEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Starting"/> class.
        /// </summary>
        /// <param name="instance">The module instance that is starting.</param>
        public Starting(IModuleInstance instance) : base(instance)
        {
        }

        /// <inheritdoc/>
        public override LifetimeEventType EventType => LifetimeEventType.Starting;
    }

    /// <summary>
    /// Represents an event indicating that a module instance has started.
    /// </summary>
    public sealed class Started : LifetimeEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Started"/> class.
        /// </summary>
        /// <param name="instance">The module instance that has started.</param>
        public Started(IModuleInstance instance) : base(instance)
        {
        }

        /// <inheritdoc/>
        public override LifetimeEventType EventType => LifetimeEventType.Started;
    }

    /// <summary>
    /// Represents an event indicating that a module instance is stopping.
    /// </summary>
    public sealed class Stopping : LifetimeEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Stopping"/> class.
        /// </summary>
        /// <param name="instance">The module instance that is stopping.</param>
        public Stopping(IModuleInstance instance) : base(instance)
        {
        }

        /// <inheritdoc/>
        public override LifetimeEventType EventType => LifetimeEventType.Stopping;
    }

    /// <summary>
    /// Represents an event indicating that a module instance has stopped.
    /// </summary>
    public sealed class Stopped : LifetimeEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Stopped"/> class.
        /// </summary>
        /// <param name="instance">The module instance that has stopped.</param>
        /// <param name="isExpected">Indicates whether the stop was expected (default is true).</param>
        public Stopped(
            IModuleInstance instance,
            bool isExpected = true) : base(instance)
        {
            IsExpected = isExpected;
        }

        /// <inheritdoc/>
        public override LifetimeEventType EventType => LifetimeEventType.Stopped;

        /// <summary>
        /// Gets a value indicating whether the stop was expected.
        /// </summary>
        public bool IsExpected { get; }
    }
}

/// <summary>
/// Specifies the type of a module lifetime event.
/// </summary>
public enum LifetimeEventType
{
    /// <summary>
    /// The module instance is starting.
    /// </summary>
    Starting,
    /// <summary>
    /// The module instance has started.
    /// </summary>
    Started,
    /// <summary>
    /// The module instance is stopping.
    /// </summary>
    Stopping,
    /// <summary>
    /// The module instance has stopped.
    /// </summary>
    Stopped
}
