using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using DockManagerCore.Utilities;

namespace DockManagerCore
{
    /*
     * Adorner - each UI element in WPF has a top layer where can be added adorner controls
     * Thumb - used to drag elements, returns drag delta
     */

    // Implements custom window resizing when default resizing is disabled (Aero disabled)
    class WindowResizingAdorner : Adorner
    {
        [Flags]
        enum Position
        {
            Top = 0x1,
            Bottom = 0x2,
            Left = 0x8,
            Right = 0x10
        }

        // Width of thumb resizer
        const int ThumbThickness = 2;

        // Stores Thumbs
        VisualCollection visualChildren;
        WindowThumb[] thumbs;

        Window _window;
        Point _mouseStartPosition;
        Point _windowStartPosition;
        Size _windowStartSize;

        /// <summary>
        /// Instantiates WindowResizingAdorner class
        /// </summary>
        /// <param name="element">Control into which's adorer layer will be this adorner added</param>
        /// <param name="window"></param>
        public WindowResizingAdorner(UIElement element, Window window)
            : base(element)
        {
            _window = window;

            // must be instantiated first before WindowThumbs are created and added
            visualChildren = new VisualCollection(element);
            thumbs = new WindowThumb[8];

            // * if you change the order, you have to change indexing in ArrangeOverride() method
            thumbs[0] = CreateThumb(Position.Left | Position.Top, Cursors.SizeNWSE);
            thumbs[1] = CreateThumb(Position.Right | Position.Top, Cursors.SizeNESW);
            thumbs[2] = CreateThumb(Position.Left | Position.Bottom, Cursors.SizeNESW);
            thumbs[3] = CreateThumb(Position.Right | Position.Bottom, Cursors.SizeNWSE);
            thumbs[4] = CreateThumb(Position.Left, Cursors.SizeWE);
            thumbs[5] = CreateThumb(Position.Top, Cursors.SizeNS);
            thumbs[6] = CreateThumb(Position.Right, Cursors.SizeWE);
            thumbs[7] = CreateThumb(Position.Bottom, Cursors.SizeNS);
        }

        /// <summary>
        /// Auxilliary method for creating thumbs
        /// </summary>
        /// <param name="position">Thumb position in the window</param>
        /// <returns>Returns created WindowThumb</returns>
        WindowThumb CreateThumb(Position position, Cursor cursor)
        {
            WindowThumb thumb = new WindowThumb();
            thumb.Position = position;
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragDelta += Thumb_DragDelta;
            thumb.Cursor = cursor;

            visualChildren.Add(thumb);

            return thumb;
        }

        // called when thumb drag started (window resize started)
        void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            WindowThumb thumb = (WindowThumb)sender;

            // store settings of the window, will be used to resize and move the window
            _mouseStartPosition = WPFHelper.GetMousePosition();
            //_mouseStartPosition = PointToScreen(Mouse.GetPosition(_window));
            _windowStartPosition = new Point(_window.Left, _window.Top);
            _windowStartSize = new Size(_window.Width, _window.Height);
        }

        // Called whenever thumb dragged (window resizing)
        void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            WindowThumb thumb = (WindowThumb)sender;

            // calculate mouse delta
            var position = WPFHelper.GetMousePosition();
            //Point position = PointToScreen(Mouse.GetPosition(_window));
            double deltaX = position.X - _mouseStartPosition.X;
            double deltaY = position.Y - _mouseStartPosition.Y;

            // horizontal resize
            if ((thumb.Position & Position.Left) == Position.Left)
            {
                double leftToMove = -deltaX;
                SetWindowWidth(_windowStartSize.Width, ref leftToMove);
                _window.Left = _windowStartPosition.X - leftToMove;
            }
            else if ((thumb.Position & Position.Right) == Position.Right)
                SetWindowWidth(_windowStartSize.Width, ref deltaX);

            // vertical resize
            if ((thumb.Position & Position.Top) == Position.Top)
            {
                double upToMove = -deltaY;
                SetWindowHeight(_windowStartSize.Height, ref upToMove);
                _window.Top = _windowStartPosition.Y - upToMove;
            }
            else if ((thumb.Position & Position.Bottom) == Position.Bottom)
                SetWindowHeight(_windowStartSize.Height, ref deltaY);
        }

        /// <summary>
        /// Auxiliary method for setting Window width
        /// </summary>
        /// <param name="width">New window width</param>
        void SetWindowWidth(double oldWidth_, ref double deltaWidth_)
        {
            var newWidth = oldWidth_ + deltaWidth_;
            var newWidthCalculated = newWidth;
            if (newWidthCalculated < 2 * ThumbThickness)
                newWidthCalculated = 2 * ThumbThickness;
            if (newWidthCalculated < _window.MinWidth)
            {
                newWidthCalculated = _window.MinWidth;
            }
            if (newWidthCalculated > _window.MaxWidth)
        {
                newWidthCalculated = _window.MaxWidth;
            }
            deltaWidth_ += newWidthCalculated - newWidth;
            _window.Width = newWidthCalculated; 
        }

        /// <summary>
        /// Auxiliary method for setting Window height
        /// </summary>
        /// <param name="height">New window hright</param>
        void SetWindowHeight(double oldHeight_, ref double deltaHeight_)
        {
            var newHeight = oldHeight_ + deltaHeight_;
            var newHeightCalculated = newHeight;
            if (newHeightCalculated < 2 * ThumbThickness)
                newHeightCalculated = 2 * ThumbThickness;
            if (newHeightCalculated < _window.MinHeight)
            {
                newHeightCalculated = _window.MinHeight;
            }
            if (newHeightCalculated > _window.MaxHeight)
        {
                newHeightCalculated = _window.MaxHeight;
            }
            deltaHeight_ += newHeightCalculated - newHeight;
            _window.Height = newHeightCalculated;
        }

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // DesiredWidth and desiredHeight are the width and height of the element that's being adorned.  
            // These will be used to place the ResizingAdorner at the corners of the adorned element.  
            double desiredWidth = AdornedElement.DesiredSize.Width;
            double desiredHeight = AdornedElement.DesiredSize.Height;

            var left = DesiredSize.Width - ThumbThickness;
            if (left < 0) left = 0;
            var width = DesiredSize.Width - (2 * ThumbThickness);
            if (width < 0) width = 0;
            var top = DesiredSize.Height - ThumbThickness;
            if (top < 0) top = 0;
            var height = DesiredSize.Height - (2 * ThumbThickness);
            if (height < 0) height = 0;
            thumbs[0].Arrange(new Rect(0, 0, ThumbThickness, ThumbThickness));
            thumbs[1].Arrange(new Rect(left, 0, ThumbThickness, ThumbThickness));
            thumbs[2].Arrange(new Rect(0, top, ThumbThickness, ThumbThickness));
            thumbs[3].Arrange(new Rect(left, top, ThumbThickness, ThumbThickness));
            thumbs[4].Arrange(new Rect(0, ThumbThickness, ThumbThickness, height));
            thumbs[5].Arrange(new Rect(ThumbThickness, 0, width, ThumbThickness));
            thumbs[6].Arrange(new Rect(left, ThumbThickness, ThumbThickness, height));
            thumbs[7].Arrange(new Rect(ThumbThickness, top, width, ThumbThickness));

            // Return the final size.
            return finalSize;
        }

 
        // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index) { return visualChildren[index]; }


        // Used for resizing a window as a dragging point
        class WindowThumb : Thumb
        {
            public Position Position { get; set; }

            public WindowThumb()
            {
                FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);

                ControlTemplate template = new ControlTemplate(typeof(WindowThumb));
                template.VisualTree = borderFactory;

                Template = template;
            }
        }
    }
}
