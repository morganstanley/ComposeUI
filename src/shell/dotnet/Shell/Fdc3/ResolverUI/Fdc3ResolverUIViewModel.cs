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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI.Pages;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

internal class Fdc3ResolverUIViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Page _advancedResolverUIPage;
    private readonly Size _advancedResolverUISize = new(width: 800, height: 600);
    private readonly List<ResolverUIAppData> _appData = new();
    private readonly Page _simpleResolverUIPage;
    private readonly Size _simpleResolverUISize = new(width: 500, height: 400);
    private readonly CancellationTokenSource _userCancellationTokenSource;
    private double _currentHeight;
    private Page _currentPage;
    private double _currentWidth;

    public Fdc3ResolverUIViewModel(IEnumerable<IAppMetadata> apps)
    {
        _userCancellationTokenSource = new CancellationTokenSource();

        foreach (var app in apps)
        {
            _appData.Add(
                new ResolverUIAppData
                {
                    AppMetadata = app,
                    Icon = app.Icons.FirstOrDefault() //First Icon from the array will be shown on the ResolverUI
                });
        }

        _simpleResolverUIPage = new SimpleResolverUIPage(_appData);
        _advancedResolverUIPage = new AdvancedResolverUIPage(_appData);
        _currentPage = _simpleResolverUIPage;
        SetCurrentSize(_simpleResolverUISize);

        OpenSimpleViewCommand = new RelayCommand(SetCurrentPageToSimpleView);
        OpenAdvancedViewCommand = new RelayCommand(SetCurrentPageToAdvancedView);
    }

    internal CancellationToken UserCancellationToken => _userCancellationTokenSource.Token;

    public Page CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    public ICommand OpenSimpleViewCommand { get; }
    public ICommand OpenAdvancedViewCommand { get; }

    public double CurrentWidth
    {
        get => _currentWidth;
        set
        {
            _currentWidth = value;
            OnPropertyChanged(nameof(CurrentWidth));
        }
    }

    public double CurrentHeight
    {
        get => _currentHeight;
        set
        {
            _currentHeight = value;
            OnPropertyChanged(nameof(CurrentHeight));
        }
    }

    public void Dispose()
    {
        _userCancellationTokenSource.Cancel();
        _userCancellationTokenSource.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetCurrentSize(Size size)
    {
        CurrentWidth = size.Width;
        CurrentHeight = size.Height;
    }

    private void SetCurrentPageToSimpleView()
    {
        CurrentPage = _simpleResolverUIPage;

        SetCurrentSize(_simpleResolverUISize);
    }

    private void SetCurrentPageToAdvancedView()
    {
        CurrentPage = _advancedResolverUIPage;
        SetCurrentSize(_advancedResolverUISize);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void CancelDialog()
    {
        _userCancellationTokenSource.Cancel();
    }
}