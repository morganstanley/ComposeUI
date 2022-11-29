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
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using System;

namespace MorganStanley.ComposeUI.Tryouts.Visuals.Avalonia.VisualUtils
{
    public class EmbeddedWindowBasedNativeControl : ContentPresenter
    {

        #region WindowHandle Styled Avalonia Property
        public long WindowHandle
        {
            get { return GetValue(WindowHandleProperty); }
            set { SetValue(WindowHandleProperty, value); }
        }

        public static readonly StyledProperty<long> WindowHandleProperty =
            AvaloniaProperty.Register<EmbeddedWindowBasedNativeControl, long>
            (
                nameof(WindowHandle)
            );
        #endregion WindowHandle Styled Avalonia Property


        public EmbeddedWindowBasedNativeControl()
        {
            this.GetObservable(WindowHandleProperty)
                .Subscribe(OnWindowHandleChanged);

            this.GetObservable(PreloadingContentProperty)
                .Subscribe(OnPreloadingContentChanged);
        }

        private void OnPreloadingContentChanged(object preloadingContent)
        {
            if (WindowHandle == 0)
            {
                Content = PreloadingContent;
            }
        }

        private void OnWindowHandleChanged(long obj)
        {
            if (WindowHandle != 0)
            {
                this.Content = new WindowBasedNativeHost(WindowHandle);
            }
        }

        #region PreloadingContent Styled Avalonia Property
        public object PreloadingContent
        {
            get { return GetValue(PreloadingContentProperty); }
            set { SetValue(PreloadingContentProperty, value); }
        }

        public static readonly StyledProperty<object> PreloadingContentProperty =
            AvaloniaProperty.Register<EmbeddedWindowBasedNativeControl, object>
            (
                nameof(PreloadingContent)
            );
        #endregion PreloadingContent Styled Avalonia Property
    }
}
