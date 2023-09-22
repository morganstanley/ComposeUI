using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

/// <summary>
/// Configuration root for FDC3 features. This object is configured under the <c>FDC3</c> section.
/// </summary>
public class Fdc3Options : IOptions<Fdc3Options>
{
    /// <summary>
    /// When set to <value>true</value>, it will enable Fdc3 backend service.
    /// </summary>
    public bool EnableFdc3 { get; set; }

    /// <summary>
    /// Options for the FDC3 Desktop Agent
    /// </summary>
    public Fdc3DesktopAgentOptions DesktopAgent { get; set; } = new();

    /// <summary>
    /// Options for the FDC3 App Directory
    /// </summary>
    public AppDirectoryOptions AppDirectory { get; set; } = new();

    /// <inheritdoc />
    public Fdc3Options Value => this;
}