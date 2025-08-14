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
/// Represents a request to stop a running module instance, including its identifier and optional properties.
/// </summary>
public sealed class StopRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopRequest"/> class.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the module instance to stop.</param>
    /// <param name="properties">Optional properties to be associated with the stop request.</param>
    public StopRequest(Guid instanceId, List<object>? properties = null)
    {
        InstanceId = instanceId;
        Properties = properties;
    }

    /// <summary>
    /// Gets the unique identifier of the module instance to stop.
    /// </summary>
    public Guid InstanceId { get; }

    /// <summary>
    /// Gets the collection of optional properties associated with the stop request.
    /// </summary>
    public List<object>? Properties { get; }
}
