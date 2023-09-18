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

using Microsoft.Extensions.Options;
using MorganStanley.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

/// <summary>
/// Configuration options for <see cref="AppDirectory"/>
/// </summary>
public sealed class AppDirectoryOptions : IOptions<AppDirectoryOptions>
{
    /// <summary>
    /// Gets or sets the source URL of a static JSON file.
    /// </summary>
    /// <remarks>
    /// Supported schemes are <c>file</c>, <c>http</c> and <c>https</c>.
    /// The static file must contain a single array of <see cref="Fdc3App"/> objects,
    /// using the schema defined by the FDC3 AppDirectory API specification (https://fdc3.finos.org/schemas/2.0/app-directory.html#tag/Application/paths/~1v2~1apps~1%7BappId%7D/get)
    /// UTF8 encoding is assumed unless a byte order mark or encoding header is present.
    /// </remarks>
    public Uri? Source { get; set; }

    // TODO: Implement fetching from AppD REST API
    ///// <summary>
    ///// Gets or sets the name of the <see cref="HttpClient"/> that is used to fetch
    ///// the application definitions from a REST API defined by the FDC3 AppDirectory API specification (https://fdc3.finos.org/schemas/2.0/app-directory.html)
    ///// </summary>
    ///// <remarks>
    ///// This property is only used when <see cref="Source"/> is set to <c>null</c>.
    ///// When this property is set, an <see cref="IHttpClientFactory"/> instance must be provided as a constructor parameter
    ///// for <see cref="AppDirectory"/>, with a matching HTTP client configured.
    ///// </remarks>
    //public string? HttpClientName { get; set; }

    /// <inheritdoc/>
    public AppDirectoryOptions Value => this;
}