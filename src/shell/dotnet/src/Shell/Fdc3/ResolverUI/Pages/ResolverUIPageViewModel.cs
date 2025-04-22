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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI.Pages;

internal class ResolverUIPageViewModel : INotifyPropertyChanged
{
    private readonly IEnumerable<ResolverUIAppData> _apps;
    private readonly RelayCommand<string> _openAppCommand;
    private readonly IPageService _pageService;
    private ResolverUIAppData? _selectedApp;

    public ResolverUIPageViewModel(
        IPageService pageService,
        IEnumerable<ResolverUIAppData> apps)
    {
        _pageService = pageService;
        Apps = apps.OrderBy(x => x.AppMetadata.InstanceId == null);
        SelectAppMetadata = new RelayCommand<ResolverUIAppData>(DoubleClickListBox);
        OpenApp = new RelayCommand<string>(ExecuteOpenApp);
    }

    public ResolverUIAppData? SelectedApp
    {
        get => _selectedApp;
        set
        {
            _selectedApp = value;
            OnPropertyChanged(nameof(SelectedApp));
        }
    }

    public IEnumerable<ResolverUIAppData> Apps
    {
        get => _apps;

        private init
        {
            _apps = value;
            OnPropertyChanged(nameof(Apps));
        }
    }

    public RelayCommand<string> OpenApp
    {
        get => _openAppCommand;
        private init
        {
            _openAppCommand = value;
            OnPropertyChanged(nameof(OpenApp));
        }
    }

    public ICommand SelectAppMetadata { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void DoubleClickListBox(ResolverUIAppData? appMetadata)
    {
        if (appMetadata == null)
        {
            return;
        }

        SelectedApp = appMetadata;
        _pageService.ClosePage(SelectedApp.AppMetadata);
    }

    private void ExecuteOpenApp(string? appId)
    {
        if (appId == null)
        {
            return;
        }

        var app = Apps.FirstOrDefault(
            x => x.AppMetadata.AppId == appId && string.IsNullOrEmpty(x.AppMetadata.InstanceId));
        if (app == default)
        {
            return;
        }

        _pageService.ClosePage(app.AppMetadata);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}