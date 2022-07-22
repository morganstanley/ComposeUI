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
namespace ReceiveTextViewModelPlugin;

[Implements(typeof(IPlugin), partKey: nameof(ReceiveTextViewModel), isSingleton: true)]
public class ReceiveTextViewModel : VMBase, IPlugin
{
    ITextService? _textService;

    // ITextService implementation
    [Part(typeof(ITextService))]
    public ITextService? TheTextService
    {
        get => _textService;
        private set
        {
            if (_textService == value)
                return;

            if (_textService != null)
            {
                // disconnect old service's SentTextEvent
                _textService.SentTextEvent -= _textService_SentTextEvent;
            }

            _textService = value;

            if (_textService != null)
            {   // connect the handler to the service's
                // SentTextEvent
                _textService.SentTextEvent += _textService_SentTextEvent;
            }
        }
    }

    // set Text property when receives it from TheTextService
    // via SentTextEvent
    private void _textService_SentTextEvent(string text)
    {
        Text = text;
    }

    #region Text Property
    private string? _text;
    // notifiable property
    public string? Text
    {
        get
        {
            return this._text;
        }
        private set
        {
            if (this._text == value)
            {
                return;
            }

            this._text = value;
            this.OnPropertyChanged(nameof(Text));
        }
    }
    #endregion Text Property
}