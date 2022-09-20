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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MorganStanley.ComposeUI.Tryouts.Visuals.Windows.VisualUtils
{
    public class EmbeddedWebView : NativeControlHost
    {

        #region CommunicationsObjects Styled Avalonia Property
        public IDictionary<string, object> CommunicationsObjects
        {
            get { return GetValue(CommunicationsObjectsProperty); }
            set { SetValue(CommunicationsObjectsProperty, value); }
        }

        public static readonly StyledProperty<IDictionary<string, object>> CommunicationsObjectsProperty =
            AvaloniaProperty.Register<EmbeddedWebView, IDictionary<string, object>>
            (
                nameof(CommunicationsObjects)
            );
        #endregion CommunicationsObjects Styled Avalonia Property


        #region IsReady Direct Avalonia Property
        private bool _IsReady = false;

        public static readonly DirectProperty<EmbeddedWebView, bool> IsReadyProperty =
            AvaloniaProperty.RegisterDirect<EmbeddedWebView, bool>
            (
                nameof(IsReady),
                o => o.IsReady,
                (o, v) => o.IsReady = v
            );

        public bool IsReady
        {
            get => _IsReady;
            private set
            {
                SetAndRaise(IsReadyProperty, ref _IsReady, value);
            }
        }
        #endregion IsReady Direct Avalonia Property


        public string CurrentExePath { get; }

        public WebView2 WebView => _webView;

        WebView2 _webView;
        public EmbeddedWebView()
        {
            _webView = new WebView2();

            CurrentExePath = Directory.GetCurrentDirectory();

            _ = Init();

            this.GetObservable(CommunicationsObjectsProperty).Subscribe(OnCommunicationsObjectsSet);
            this.GetObservable(IsReadyProperty).Subscribe(OnIsReadyChanged);

            this.GetObservable(HtmlPathProperty).Subscribe(OnRelativeHtmlPathSet);
        }

        private void OnRelativeHtmlPathSet(string? path)
        {
            if (path == null)
            {
                return;
            }

            if (path.ToLower().StartsWith("http://"))
            {
                Source = new Uri(path, UriKind.Absolute);
            }
            else
            {
                Source = 
                    new Uri
                    (
                        "file:///" + CurrentExePath + "\\" + path,
                        UriKind.RelativeOrAbsolute);
            }
        }


        #region HtmlPath Styled Avalonia Property
        public string? HtmlPath
        {
            get { return GetValue(HtmlPathProperty); }
            set { SetValue(HtmlPathProperty, value); }
        }

        public static readonly StyledProperty<string?> HtmlPathProperty =
            AvaloniaProperty.Register<EmbeddedWebView, string?>
            (
                nameof(HtmlPath)
            );
        #endregion HtmlPath Styled Avalonia Property


        public async Task Init()
        {
            await _webView.EnsureCoreWebView2Async(null);

            IsReady = true;

            _webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }


        private bool _domContentLoaded = false;
        private bool _objectsAdded = false;
        private void CoreWebView2_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            _domContentLoaded = true;
            CallJSInit();
        }

        void CallJSInit()
        {
            if ((!_domContentLoaded) || (!_objectsAdded))
            {
                return;
            }

            _webView.CoreWebView2.ExecuteScriptAsync("window.IsWebView2Ready = true;");
            _webView.CoreWebView2.ExecuteScriptAsync("console.log('Initialized IsWebView2ready');");

            string js =
                @"if (window.OnWebView2Initialized){ window.OnWebView2Initialized(); }";

            _webView.CoreWebView2.ExecuteScriptAsync(js);
        }

        private void OnCommunicationsObjectsSet(IDictionary<string, object> obj)
        {
            SetCommunicationObjects();
        }

        private void OnIsReadyChanged(bool obj)
        {
            SetCommunicationObjects();
        }

        private void SetCommunicationObjects()
        {
            if (!IsReady || CommunicationsObjects == null)
            {
                return;
            }

            foreach((string key, object value) in CommunicationsObjects)
            {
                _webView.CoreWebView2.AddHostObjectToScript(key, value);
            }
            _objectsAdded = true;

            CallJSInit();
        }


        public Uri? Source
        {
            get => _webView.Source;
            set => _webView.Source = value;
        }


        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            IPlatformHandle handle =
                new PlatformHandle(_webView.Handle, "HWND");

            return handle;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            //_webView.Dispose();
        }
    }
}
