using NP.IoCy;
using System.Windows;

namespace WpfThemeReceiverApp
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

            //Thread.Sleep(30000);

            Container.InjectPluginsFromSubFolders("Plugins/Services");
            Container.InjectPluginsFromSubFolders("Plugins/WpfServices");

            Container.CompleteConfiguration();
        }
    }
}
