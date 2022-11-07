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

namespace LocalCollector.Connections;

public record ConnectionInfo
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? LocalEndpoint { get; set; }
    public string? RemoteEndpoint { get; set; }
    public string? RemoteApplication { get; set; }
    public string? RemoteHostname { get; set; }
    public ConcurrentDictionary<string, string>? ConnectionInformation { get; set; }
    public string? Status { get; set; }
}
