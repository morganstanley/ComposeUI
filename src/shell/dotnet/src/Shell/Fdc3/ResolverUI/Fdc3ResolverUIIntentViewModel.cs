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
using System.Threading;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

public class Fdc3ResolverUIIntentViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CancellationTokenSource _userCancellationTokenSource;
    private readonly ObservableCollection<ResolverUIIntentModel> _intents = new();

    public ObservableCollection<ResolverUIIntentModel> Intents => _intents;

    public Fdc3ResolverUIIntentViewModel(IEnumerable<string> intents)
    {
        _userCancellationTokenSource = new CancellationTokenSource();
        foreach (var intent in intents)
        {
            _intents.Add(new ResolverUIIntentModel() { IntentName = intent });
        }

        CancelCommand = new RelayCommand(CancelDialog);
    }

    public ICommand CancelCommand { get; }
    public ResolverUIIntentModel? SelectedIntent { get; set; }
    public CancellationToken UserCancellationToken { get; internal set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _userCancellationTokenSource.Cancel();
        _userCancellationTokenSource.Dispose();
    }

    internal void CancelDialog()
    {
        _userCancellationTokenSource.Cancel();
    }
}
