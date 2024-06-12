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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;



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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI.Pages;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

internal class Fdc3ResolverUIViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CancellationTokenSource _userCancellationTokenSource;
    private readonly List<ResolverUIAppData> _appData = new();
    private readonly Page _simpleResolverUIPage;
    private readonly Size _simpleResolverUISize = new(500, 400);
    private readonly Page _advancedResolverUIPage;
    private readonly Size _advancedResolverUISize = new(800, 600);
    private Page _currentPage;
    private Size _currentSize;

    internal CancellationToken UserCancellationToken => _userCancellationTokenSource.Token;

    public Fdc3ResolverUIViewModel(IEnumerable<IAppMetadata> apps)
    {
        _userCancellationTokenSource = new();

        foreach (var app in apps)
        {
            _appData.Add(new()
            {
                AppId = app.AppId,
                AppMetadata = app,
                Icon = app.Icons.FirstOrDefault() //First Icon from the array will be shown on the ResolverUi
            });
        }

        _simpleResolverUIPage = new SimpleResolverUIPage(_appData);
        _advancedResolverUIPage = new AdvancedResolverUIPage(_appData);
        _currentPage = _simpleResolverUIPage;
        SetSize(_simpleResolverUISize);
        
        OpenSimpleViewCommand = new RelayCommand(_ => SetCurrentPageToSimpleView());
        OpenAdvancedViewCommand = new RelayCommand(_ => SetCurrentPageToAdvancedView());
    }

    private void SetSize(Size size)
    {
        _currentSize = size;
        SizeChanged?.Invoke(this, new PageSizeChangedEventArgs(_currentSize));
    }

    public Page CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    private void SetCurrentPageToSimpleView()
    {
        CurrentPage = _simpleResolverUIPage;
        SetSize(_simpleResolverUISize);
    }

    private void SetCurrentPageToAdvancedView()
    {
        CurrentPage = _advancedResolverUIPage;
        SetSize(_advancedResolverUISize);
    }

    public ICommand OpenSimpleViewCommand { get; }
    public ICommand OpenAdvancedViewCommand { get; }


    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PageSizeChangedEventArgs>? SizeChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void CancelDialog()
    {
        _userCancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        _userCancellationTokenSource?.Cancel();
        _userCancellationTokenSource?.Dispose();
    }
}
