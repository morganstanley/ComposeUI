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
/// Contains logic around topics.
/// </summary>
public static class Topic
{
    /// <summary>
    /// Checks if the provided string is a valid topic name.
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public static bool IsValidTopicName(string topic) => !string.IsNullOrWhiteSpace(topic);

    public static void Validate(string topic)
    {
        if (!IsValidTopicName(topic))
            throw new InvalidTopicException(topic);
    }

}
