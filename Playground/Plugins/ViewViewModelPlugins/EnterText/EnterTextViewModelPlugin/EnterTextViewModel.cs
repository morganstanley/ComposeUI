/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Playground.Interfaces;
using NP.Utilities;
using NP.Utilities.Attributes;
using NP.Utilities.PluginUtils;
namespace EnterTextViewModelPlugin;

[Implements(typeof(IPlugin), partKey: nameof(EnterTextViewModel), isSingleton: true)]
public class EnterTextViewModel : VMBase, IPlugin
{
    // ITextService implementation
    [Part(typeof(ITextService))]
    public ITextService? TheTextService { get; private set; }

    #region Text Property
    private string? _text;

    // notifiable property
    public string? Text
    {
        get
        {
            return this._text;
        }
        set
        {
            if (this._text == value)
            {
                return;
            }

            this._text = value;
            this.OnPropertyChanged(nameof(Text));
            this.OnPropertyChanged(nameof(CanSendText));
        }
    }
    #endregion Text Property

    // change notified the Text changes
    public bool CanSendText => !string.IsNullOrWhiteSpace(this._text);

    // method to send the text via TextService
    public void SendText()
    {
        if (!CanSendText)
        {
            throw new Exception("Cannost send text, this method should not have been called.");
        }

        TheTextService!.Send(Text!);
    }
}
