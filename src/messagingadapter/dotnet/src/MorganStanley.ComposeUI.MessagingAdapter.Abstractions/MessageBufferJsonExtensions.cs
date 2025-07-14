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

using System.Text;
using System.Text.Json;

namespace MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

/// <summary>
/// Extensions methods for handling JSON data in <see cref="MessageBuffer"/> objects.
/// </summary>
public static class StringBufferJsonExtensions
{
    /// <summary>
    /// Deserializes the JSON content of the buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buffer"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static T? ReadJson<T>(this string buffer, JsonSerializerOptions? options = null)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(buffer));

        return JsonSerializer.Deserialize<T>(ref reader, options);
    }
}