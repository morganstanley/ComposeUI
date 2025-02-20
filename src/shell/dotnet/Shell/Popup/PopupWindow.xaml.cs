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

namespace MorganStanley.ComposeUI.Shell.Popup;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();

        SizeChanged += PopupWindow_SizeChanged;
        Closed += PopupWindow_Closed;
    }

    private void PopupWindow_Closed(object? sender, System.EventArgs e)
    {
        if (MainContentPresenter.Content is WebContent webContent)
        {
            webContent.Dispose();
        }
    }

    private void PopupWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainContentPresenter.Content is FrameworkElement element)
        {
            element.Width = ActualWidth;   // Set content width to window width
            element.Height = ActualHeight; // Set content height to window height

            element.HorizontalAlignment = HorizontalAlignment.Stretch;
            element.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    public void SetContent(UIElement content)
    {
        if (content is FrameworkElement element)
        {
            element.HorizontalAlignment = HorizontalAlignment.Stretch;
            element.VerticalAlignment = VerticalAlignment.Stretch;
            Height = element.Height;
            Width = element.Width;
        }

        MainContentPresenter.Content = content;
    }
}
