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

using System;
using System.IO;
using System.Reflection;

namespace MorganStanley.ComposeUI.Utilities;

public static class ResourceReader
{
    /// <summary>
    /// Reads the contents of an embedded resource as a string.
    /// </summary>
    /// <param name="resourcePath">The fully qualified name of the embedded resource.</param>
    /// <returns>The contents of the resource as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the resource is not found in the calling assembly.</exception>
    public static string ReadResource(string resourcePath)
    {
        var assembly = Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new InvalidOperationException("Resource not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
