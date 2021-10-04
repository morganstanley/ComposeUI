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
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DockManagerCore.Desktop;

namespace DockManagerCore.Utilities
{
    public static class WPFHelper
    {
        internal static bool Contains(this FloatingWindow window_, PaneContainer container_)
        {
            if (container_ != null)
            {
                var currentContainer = container_;
                while (currentContainer != null)
                {
                    if (currentContainer == window_.PaneContainer)
                    {
                        return true;
                    }
                    currentContainer = currentContainer.FindVisualParent<PaneContainer>();
                }
            }
            return false;
        }
         

        public static int GetZIndex(Window win)
        {
            var source = PresentationSource.FromVisual(win);
            if (source == null) return -1;
            var byHandle = ((HwndSource)source).Handle;
            int zindex = 0;
            for (IntPtr hWnd = GetTopWindow(IntPtr.Zero); hWnd != IntPtr.Zero; hWnd = GetWindow(hWnd, GW_HWNDNEXT), zindex++)
                if (hWnd == byHandle)
                    return zindex;

            return -1;

        }

        public static Point GetMousePosition()
        { 
            Win32.POINT p;
            Win32.GetCursorPos(out p); 

            return new Point(p.X, p.Y);
        }


        public static Point GetCurrentPosition(FrameworkElement relativeTo_)
        {
            Win32.POINT cursor;
            Win32.GetCursorPos(out cursor);
            return relativeTo_.PointFromScreen(new Point(cursor.X, cursor.Y));
        }

        public static Point GetPositionWithOffset(FrameworkElement relativeTo_)
        {
            var screenPosition = GetMousePosition();
            var relativePosition = relativeTo_.PointFromScreen(screenPosition);
            return new Point(screenPosition.X - relativePosition.X, screenPosition.Y - relativePosition.Y);

        }
        const uint GW_HWNDNEXT = 2;
         
        [DllImport("user32.dll")]
        extern static IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        extern static IntPtr GetWindow(IntPtr hWnd, uint wCmd);


        internal static bool Activate(Window window)
        {
            if (((window == null) || !window.IsVisible) || !window.IsLoaded)
            {
                return false;
            }
            if (window.IsActive)
            {
                return true;
            }
            WindowInteropHelper helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
            {
                return false;
            }
            if (!Win32.IsWindowEnabled(helper.Handle))
            {
                return false;
            }
            return window.Activate();
        }

 


    }
}
