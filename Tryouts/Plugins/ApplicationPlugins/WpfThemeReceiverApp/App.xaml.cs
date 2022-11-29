using NP.IoCy;
using System.Windows;

namespace WpfThemeReceiverApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IoCContainer Container { get; } = new IoCContainer();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Container.InjectPluginsFromSubFolders("Plugins/Services");
            Container.InjectPluginsFromSubFolders("Plugins/WpfServices");

            Container.MapSingleton<MainWindow, MainWindow>();

            Container.CompleteConfiguration();

            MainWindow = Container.Resolve<MainWindow>();
            MainWindow.Show();
        }
    }
}
