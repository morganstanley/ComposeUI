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

using Infragistics.Windows.DockManager;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Utilities;

internal static class XamDockManagerExtensions
{
    /// <summary>
    /// Places the created <see cref="ContentPane"/> currently via <see cref="App.CreateWebContent(object[])"/> into the <see cref="XamDockManager"/>'s container based on the ModuleCatalog configuration for each module.
    /// </summary>
    /// <param name="xamDockManager">The dock manager which handles the created <see cref="ContentPane"/>'s docking.</param>
    /// <param name="webContentPane">The created <see cref="ContentPane"/>.</param>
    /// <param name="options">Options to set the created <see cref="ContentPane"/>'s intial configuration, like: location, size etc.</param>
    public static void OpenLocatedWebContentPane(
        this XamDockManager xamDockManager,
        ContentPane contentPane)
    {
        var splitPane = new SplitPane();

        WebWindowOptions? options = null;
        if (contentPane is WebContentPane webContentPane 
            && webContentPane.WebContent.Options != null)
        {
            options = webContentPane.WebContent.Options;
        }

        splitPane.SetSplitPaneFloatingLocation(options?.Coordinates);
        splitPane.SetSplitPaneFloatingSize(options?.Width, options?.Height);

        //TODO: By default we dock to the left.
        splitPane.SetValue(
            XamDockManager.InitialLocationProperty,
            options?.InitialModulePostion != null
                ? ((InitialModulePosition) options.InitialModulePostion).ConvertPaneLocation()
                : InitialPaneLocation.DockedLeft);

        splitPane.Panes.Add(contentPane);
        xamDockManager.Panes.Add(splitPane);
        return;
    }
}
