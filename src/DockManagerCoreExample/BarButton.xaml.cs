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
using System.Windows.Input;
using System.Windows.Media;

namespace DockManagerCoreExample
{
    /// <summary>
    /// Interaction logic for WindowButton.xaml
    /// </summary>
    public partial class BarButton : Window
    {
        private SelectorWindow win;

        public BarButton(Window w)
        {
            InitializeComponent();
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            win = (SelectorWindow) w;
            Top = 300;
        }


        protected override void OnMouseEnter(MouseEventArgs e)
        {
                Top = win.Height;
                win.Show();
                
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!win.IsMouseOver)
            {
                win.Hide();
                Top = 300;
                Show();
            }
        }

    }
}
