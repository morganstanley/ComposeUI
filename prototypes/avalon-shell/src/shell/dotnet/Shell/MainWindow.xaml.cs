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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.ImageSource;
using MorganStanley.ComposeUI.Shell.Layout;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : RibbonWindow
{
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly ImageSourceProvider _iconProvider;

    public MainWindow(
        IModuleCatalog moduleCatalog,
        IModuleLoader moduleLoader,
        IImageSourcePolicy? imageSourcePolicy = null)
    {
        _moduleCatalog = moduleCatalog;
        _moduleLoader = moduleLoader;
        _iconProvider = new ImageSourceProvider(imageSourcePolicy ?? new DefaultImageSourcePolicy());

        InitializeComponent();

        DockManager.AnchorableHidden += (sender, args) =>
        {
            args.Anchorable.Close();
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        SaveLayout();
    }

    private void SaveLayout()
    {
        var fileName = GetLayoutFileName();
        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        using var stream = File.Create(fileName);
        DockingHelper.SaveLayout(stream);
    }

    private string GetLayoutFileName()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Morgan Stanley",
            "ComposeUI-AvalonDock",
            "layout.xml");
    }

    private async void RibbonWindow_Initialized(object sender, System.EventArgs e)
    {
        var moduleIds = await _moduleCatalog.GetModuleIds();

        var modules = new List<ModuleViewModel>();
        foreach (var moduleId in moduleIds)
        {
            var manifest = await _moduleCatalog.GetManifest(moduleId);
            modules.Add(new ModuleViewModel(manifest, _iconProvider));
        }

        ViewModel = new MainWindowViewModel
        {
            Modules = new ObservableCollection<ModuleViewModel>(modules)
        };

        LoadLayout();
    }

    private void LoadLayout()
    {
        var fileName = GetLayoutFileName();

        if (!File.Exists(fileName))
            return;

        using var backupStream = new MemoryStream();
        DockingHelper.SaveLayout(backupStream);

        try
        {
            using var stream = File.OpenRead(fileName);
            DockingHelper.LoadLayout(stream);
        }
        catch (Exception e)
        {
            backupStream.Position = 0;
            DockingHelper.LoadLayout(backupStream);
        }
    }

    internal MainWindowViewModel ViewModel
    {
        get => (MainWindowViewModel) DataContext;
        private set => DataContext = value;
    }

    private async void StartModule_Click(object sender, RoutedEventArgs e)
    {
        // I ❤️ C#
        if (sender is FrameworkElement
            {
                DataContext: ModuleViewModel module
            })
        {
            await _moduleLoader.StartModule(new StartRequest(module.Manifest.Id));
        }
    }

    internal sealed class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<ModuleViewModel> Modules
        {
            get => _modules;
            set => SetProperty(ref _modules, value);
        }

        private ObservableCollection<ModuleViewModel> _modules = new();
    }

    internal sealed class ModuleViewModel
    {
        public ModuleViewModel(IModuleManifest manifest, ImageSourceProvider imageSourceProvider)
        {
            Manifest = manifest;

            if (manifest.TryGetDetails<WebManifestDetails>(out var webManifestDetails))
            {
                if (webManifestDetails.IconUrl != null)
                {
                    ImageSource = imageSourceProvider.GetImageSource(
                        webManifestDetails.IconUrl,
                        webManifestDetails.Url);
                }
            }
            else if (manifest.TryGetDetails<NativeManifestDetails>(out var nativeManifestDetails))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(Path.GetFullPath(nativeManifestDetails.Path.ToString()));

                ImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                new Int32Rect { Width = icon.Width, Height = icon.Height },
                BitmapSizeOptions.FromEmptyOptions());
            }
        }

        public IModuleManifest Manifest { get; }

        public System.Windows.Media.ImageSource? ImageSource { get; }
    }
}