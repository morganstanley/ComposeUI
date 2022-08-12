using NP.IoCy;
using System.Windows;
using System.Windows.Navigation;

namespace WpfThemeChooserApp
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
            this.MainWindow.DataContext = Container.Resolve<ViewModel>();
            MainWindow.Show();
        }
    }
}
