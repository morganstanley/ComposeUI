// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System.ComponentModel.DataAnnotations;

namespace MorganStanley.ComposeUI.Shell;

public sealed class WebWindowOptions
{
    [Display(Description = "Set the height of the window. Default: 450")]
    public double? Height { get; set; }

    [Display(Description = $"Set the title of the window. Default: {DefaultTitle}")]
    public string? Title { get; set; }

    [Display(Description = $"Set the url for the web view. Default: {DefaultUrl}")]
    public string? Url { get; set; }

    [Display(Name = "icon", Description = $"Set the icon url for the window.")]
    public string? IconUrl { get; set; }

    [Display(Description = $"Set the width of the window. Default: 800")]
    public double? Width { get; set; }

    public const double DefaultHeight = 450;
    public const string DefaultTitle = "Compose Web Container";
    public const string DefaultUrl = "about:blank";
    public const double DefaultWidth = 800;
    public const string ParameterName = nameof(WebWindowOptions);
}