using NP.IoCy;
using System.Windows;

namespace WpfThemeChooserApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal IoCContainer Container { get; } = new IoCContainer();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Container.InjectPluginsFromSubFolders("Plugins/Services");

            Container.CompleteConfiguration();
        }
    }
}
