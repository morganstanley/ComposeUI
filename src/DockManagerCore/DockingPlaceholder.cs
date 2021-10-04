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
using System.Windows.Media;
using DockManagerCore.Desktop;
using DockManagerCore.Utilities;

namespace DockManagerCore
{

    [TemplatePart(Name = "PART_HintBlock", Type=typeof(DockingHintBlock))]
    public class DockingPlaceholder : Window
    {
        static DockingPlaceholder()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPlaceholder),
                new FrameworkPropertyMetadata(typeof(DockingPlaceholder)));
        }

        public DockingPlaceholder()
        {
            AllowsTransparency = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
        }
        public bool IsGlobalDocker
        {
            get => (bool)GetValue(IsGlobalDockerProperty);
            set => SetValue(IsGlobalDockerProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsGlobalDocker.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsGlobalDockerProperty =
            DependencyProperty.Register("IsGlobalDocker", typeof(bool), typeof(DockingPlaceholder), new PropertyMetadata(false));

        private DockingHintBlock hintBlock;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            hintBlock = GetTemplateChild("PART_HintBlock") as DockingHintBlock;
        }

        internal void SetPosition(DockingGrid grid_)
        {
            Window parentWindow = grid_.FindVisualParent<Window>();
            if (parentWindow == null) return;
            GeneralTransform transform = grid_.TransformToAncestor(parentWindow);
            Point transformedPoint = transform.Transform(new Point(0, 0));
            Point topLeft = grid_.PointToScreen(transformedPoint); 
            topLeft.X -= transformedPoint.X;
            topLeft.Y -= transformedPoint.Y; 
            Point bottomRight = new Point(topLeft.X + grid_.ActualWidth, topLeft.Y + grid_.ActualHeight);
            Rect rect = new Rect(topLeft, bottomRight); 
            Position(rect); 
            Show();

            Point relativePos = WPFHelper.GetCurrentPosition(grid_);

            if (relativePos.X < grid_.ActualWidth / 3.0)
            {
                if (relativePos.Y < grid_.ActualHeight / 3.0)
                    DockTopLeft(rect);
                else if (relativePos.Y < grid_.ActualHeight * 2.0 / 3.0)
                    DockLeft(rect);
                else
                    DockBottomLeft(rect);
            }
            else if (relativePos.X < grid_.ActualWidth * 2.0 / 3.0)
            {
                if (relativePos.Y < grid_.ActualHeight / 3.0)
                    DockTop(rect);
                else if (relativePos.Y < grid_.ActualHeight * 2.0 / 3.0)
                    DockCenter(rect);
                else
                    DockBottom(rect);
            }
            else
            {
                if (relativePos.Y < grid_.ActualWidth / 3.0)
                    DockTopRight(rect);
                else if (relativePos.Y < grid_.ActualHeight * 2.0 / 3.0)
                    DockRight(rect);
                else
                    DockBottomRight(rect);
            }
        }

        private void Position(Rect area_)
        {
            Left = area_.X;
            Top = area_.Y;
            Width = area_.Width;
            Height = area_.Height;
        }

        private void DockTop(Rect area_)
        {
            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, 0.0);
            hintBlock.SetValue(Canvas.TopProperty, 0.0);

            hintBlock.Width = width;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.Top;

        }

        private void DockLeft(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, 0.0);
            hintBlock.SetValue(Canvas.TopProperty, 0.0);

            hintBlock.Width = width / 2;
            hintBlock.Height = height;
            hintBlock.Dock = DockLocation.Left;
        }

        private void DockRight(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, width * 0.5);
            hintBlock.SetValue(Canvas.TopProperty, 0.0);

            hintBlock.Width = width / 2;
            hintBlock.Height = height;
            hintBlock.Dock = DockLocation.Right;
        }

        private void DockBottom(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, 0.0);
            hintBlock.SetValue(Canvas.TopProperty, height * 0.5);

            hintBlock.Width = width;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.Bottom;
        }

        private void DockBottomLeft(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, 0.0);
            hintBlock.SetValue(Canvas.TopProperty, height * 0.5);

            hintBlock.Width = width / 2;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.BottomLeft;
        }

        private void DockBottomRight(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, width * 0.5);
            hintBlock.SetValue(Canvas.TopProperty, height * 0.5);

            hintBlock.Width = width / 2;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.BottomRight;
        }

        private void DockTopLeft(Rect area_)
        {

            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, 0.0);
            hintBlock.SetValue(Canvas.TopProperty, 0.0);

            hintBlock.Width = width / 2;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.TopLeft;
        }

        private void DockTopRight(Rect area_)
        {
            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, width * 0.5);
            hintBlock.SetValue(Canvas.TopProperty, 0.0);

            hintBlock.Width = width / 2;
            hintBlock.Height = height / 2;
            hintBlock.Dock = DockLocation.TopRight;
        }

        private void DockCenter(Rect area_)
        {
            double width = area_.Width - BorderThickness.Left - BorderThickness.Right;
            double height = area_.Height - BorderThickness.Top - BorderThickness.Bottom;

            hintBlock.SetValue(Canvas.LeftProperty, width * 0.3);
            hintBlock.SetValue(Canvas.TopProperty, height * 0.3);

            hintBlock.Width = width / 3;
            hintBlock.Height = height / 3;
            hintBlock.Dock = DockLocation.Center;
        }

    }
}
