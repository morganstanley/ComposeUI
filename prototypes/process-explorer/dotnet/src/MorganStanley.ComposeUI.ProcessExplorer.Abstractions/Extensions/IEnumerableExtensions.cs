﻿// Morgan Stanley makes this available to you under the Apache License,
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

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Extensions;

public static class IEnumerableExtensions
{
    public static int IndexOf<T>(this IEnumerable<T> source, T value, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var index = source
            .Select((scopeValue, indexOf) => new { value = scopeValue, indexOf })
            .FirstOrDefault(nextValue => comparer.Equals(nextValue.value, value));

        return index?.indexOf ?? -1;
    }

    public static IEnumerable<T> Replace<T>(this IEnumerable<T> enumerable, int index, T value)
    {
        return enumerable.Select((x, i) => index == i ? value : x);
    }
}
