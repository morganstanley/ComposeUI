using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace DockManagerCore.Desktop
{
    /// <summary>
    /// Used to tell the ViewManager where and how to place the new view.
    /// </summary>
    [Flags]
    public enum InitialLocation
    {
        /// <summary>
        /// The new view will be created floating, and can be docked by the user. 
        /// </summary>
        Floating = 1,

        /// <summary>
        /// The new view will be created floating, and can NOT be docked by the user. 
        /// </summary>
        FloatingOnly = 2,

        /// <summary>
        /// The new view will be created docked to a new empty tab in the main docking area.
        /// </summary>
        DockInNewTab = 4,

        /// <summary>
        /// The new view will be created floating centered under the mouse pointer. Can only be 
        /// used in conjunction with InitialLocation.Floating or InitialLocation.FloatingOnly.
        /// </summary>
        PlaceAtCursor = 8,

        /// <summary>
        /// Custom placement logic via the InitialLocationCallback, look at <see cref="InitialLocationHelper"/> for 
        /// common scenarios. Can only be used in conjunction with InitialLocation.Floating or 
        /// InitialLocation.FloatingOnly.
        /// </summary>
        Custom = 16,

        /// <summary>
        /// The new view will be created docked in a tab group with DockTarget in <see cref="InitialWindowParameters"/>.
        /// 
        /// If InitialLocationTarget is null and DocumentContentHost mode is enabled, pane will be docked
        /// inside the DocumentContentHost.
        /// </summary>
        DockTabbed = 32,

        /// <summary>
        /// The new view will be created docked to the left of DockTarget in <see cref="InitialWindowParameters"/>.
        /// </summary>
        DockLeft = 64,

        /// <summary>
        /// The new view will be created docked to the top of DockTarget in <see cref="InitialWindowParameters"/>.
        /// </summary>
        DockTop = 128,

        /// <summary>
        /// The new view will be created docked to the right of DockTarget in <see cref="InitialWindowParameters"/>.
        /// </summary>
        DockRight = 256,

        /// <summary>
        /// The new view will be created docked to the bottom of DockTarget in <see cref="InitialWindowParameters"/>.
        /// </summary>
        DockBottom = 512,

        /// <summary>
        /// Thew new view will be docked in the currently active tab. Has to be used in conjuction with
        /// DockTabbed, DockLeft, DockTop, DockRight or DockBottom.
        /// 
        /// The option should be used with InitialLocationTarget == null.
        /// </summary>
        DockInActiveTab = 1024
    }

	public delegate Rect CustomInitialLocationDelegate(double width, double height);

    public delegate PointF CustomInitialLocationDelegate2(double parentContainerWidth_, double parentContainerHeight_, double viewWidth_, double viewHeight_);

	public static class InitialLocationHelper
	{
		public static Rect PlaceAtCursor(double width, double height)
		{
			Point p = Cursor.Position;
			var proposedLeft = p.X - width / 2;
			var proposedTop = p.Y - height / 2;
			var screen = Screen.FromPoint(new Point(p.X, p.Y));

			if (proposedLeft < screen.WorkingArea.Left) proposedLeft = screen.WorkingArea.Left;
			else if (proposedLeft + width > screen.WorkingArea.Right) proposedLeft = screen.WorkingArea.Right - width;

			if (proposedTop < screen.WorkingArea.Top) proposedTop = screen.WorkingArea.Top;
			else if (proposedTop + height > screen.WorkingArea.Bottom) proposedTop = screen.WorkingArea.Bottom - height;

			return new Rect(proposedLeft, proposedTop, width, height);
		}

		public static Rect MainDisplayBottomRight(double width, double height)
		{
			var wa = Screen.PrimaryScreen.WorkingArea;

			return new Rect(wa.Right - width - 10, wa.Bottom - height - 10, width, height);
		}

		public static Rect MainDisplayCenter(double width, double height)
		{
			var wa = Screen.PrimaryScreen.WorkingArea;

			return new Rect(wa.Left + (wa.Width - width)/2, wa.Top + (wa.Height - height)/2, width, height);
		}
	}
     
}
