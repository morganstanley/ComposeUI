using NP.IoCy;
using System.Windows;

namespace SimpleWpfApp
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

            Container.MapSingleton<ViewModel, ViewModel>();

            Container.CompleteConfiguration();

            MainWindow = new MainWindow();

            MainWindow.DataContext = Container.Resolve<ViewModel>();

            MainWindow.Show();
        }
    }
}
