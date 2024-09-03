/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

namespace MorganStanley.ComposeUI.LayoutPersistence.Tests;

public class LayoutData
{
    public List<Window> Windows { get; set; } = new List<Window>();

    public override bool Equals(object? obj)
    {
        if (obj is LayoutData other)
        {
            return Windows.SequenceEqual(other.Windows);
        }
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Windows.Aggregate(0, (hash, window) => (hash * 397) ^ (window?.GetHashCode() ?? 0));
        }
    }
}
