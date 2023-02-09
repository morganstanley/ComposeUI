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

using MorganStanley.ComposeUI.Messaging.Exceptions;

namespace MorganStanley.ComposeUI.Messaging.Protocol;

/// <summary>
/// Contains logic around endpoints.
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Checks if the provided string is a valid endpoint name.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public static bool IsValidEndpoint(string endpoint) => !string.IsNullOrWhiteSpace(endpoint);

    /// <summary>
    /// Throws an exception if the provided string is not a valid endpoint name.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <exception cref="InvalidEndpointException"></exception>
    public static void Validate(string endpoint)
    {
        if (!IsValidEndpoint(endpoint))
            throw new InvalidEndpointException(endpoint);
    }
}
