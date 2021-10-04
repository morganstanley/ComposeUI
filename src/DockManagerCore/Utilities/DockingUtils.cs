using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using DockManagerCore.Services;
using Application = System.Windows.Application;

namespace DockManagerCore.Utilities
{
    static class DockingUtils
    {
        private static readonly DockingPlaceholder VerticalDock = new DockingPlaceholder { IsGlobalDocker = true };
        private static readonly DockingPlaceholder HorizontalDock = new DockingPlaceholder { IsGlobalDocker = true };

        public static FloatingWindow FindWindow(string wid)
        {
            WindowCollection windows = Application.Current.Windows;
            foreach (Window w in windows)
            {
                if (w is FloatingWindow && w.Uid.Equals(wid))
                {
                    return w as FloatingWindow;
                }
            }
            return null;
        }

        public static void DockUp(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left;
            w.Top = rect.Top;
            w.Width = rect.Width;
            w.Height = rect.Height / 2.0;
        }

        public static void DockLeft(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            Console.WriteLine(rect.Left);
            w.Left = rect.Left;
            w.Top = rect.Top;
            w.Width = rect.Width / 2.0;
            w.Height = rect.Height;
        }

        public static void DockRight(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left + rect.Width / 2.0;
            w.Top = rect.Top;
            w.Width = rect.Width /2.0;
            w.Height = rect.Height;
        }

        public static void DockDown(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left;
            w.Top = rect.Top + rect.Height / 2.0;
            w.Width = rect.Width;
            w.Height = rect.Height / 2.0;
        }

        public static void DockBottomLeft(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left;
            w.Top = rect.Top + rect.Height / 2;
            w.Width = rect.Width / 2.0;
            w.Height = rect.Height / 2.0;
        }

        public static void DockBottomRight(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left + rect.Width / 2.0;
            w.Top = rect.Top + rect.Height / 2.0;
            w.Width = rect.Width / 2.0;
            w.Height = rect.Height / 2.0;
        }

        public static void DockTopLeft(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left;
            w.Top = rect.Top;
            w.Width = rect.Width / 2.0;
            w.Height = rect.Height / 2.0;
             
        }

        public static void DockTopRight(Window w, Screen s)
        {
            Rectangle rect = s.WorkingArea;
            w.Left = rect.Left + rect.Width / 2.0;
            w.Top = rect.Top;
            w.Width = rect.Width / 2.0;
            w.Height = rect.Height / 2.0;
        }

        public static void BorderDocking(double left, double top, Window win, bool dock)
        {
            bool couldDockHor = false;
            bool couldDockVer = false;
            Screen s = FindScreenFromWindow(win);
            Rectangle rect = s.WorkingArea;
            left -= rect.Left;
            if (left < DockService.DockingThreshold)
            {
                couldDockVer = true;
                VerticalDock.Width = 20;
                VerticalDock.Height = rect.Height;  
                VerticalDock.Left = rect.Left;
                VerticalDock.Top = rect.Top; 
                if (dock)
                {
                    win.Left = rect.Left;
                }
                
            }
            else if (left + win.Width > (rect.Width - DockService.DockingThreshold))
            {
                couldDockVer = true;
                VerticalDock.Width = 20;
                VerticalDock.Height = rect.Height;  
                VerticalDock.Left = rect.Left + rect.Width - 20;
                VerticalDock.Top = rect.Top; 
                if (dock)
                {
                    win.Left = rect.Left + rect.Width - win.Width;
                }
            }
            if (top < DockService.DockingThreshold)
            {
                couldDockHor = true;
                HorizontalDock.Width = rect.Width;
                HorizontalDock.Height = 20;  
                HorizontalDock.Left = rect.Left;
                HorizontalDock.Top = rect.Top; 
                if (dock)
                {
                    win.Top = rect.Top;
                }
            }
            else if (top + win.Height > (rect.Height - DockService.DockingThreshold))
            {
                couldDockHor = true;
                HorizontalDock.Width = rect.Width;
                HorizontalDock.Height = 20;  
                HorizontalDock.Left = rect.Left;
                HorizontalDock.Top = rect.Top + rect.Height - 20; 
                if (dock)
                {
                    win.Top = rect.Top + rect.Height - win.Height;
                }
            }
            
            if (!couldDockHor)
            {
                HorizontalDock.Hide(); 
            }
            else
            {
                HorizontalDock.Show();
            }
            if (!couldDockVer)
            {
                VerticalDock.Hide();
            }
            else
            {
                VerticalDock.Show();
            }
        }

        public static void SearchToDock(double left, double top, Window win, bool dock)
        {
            WindowCollection windows = Application.Current.Windows;
            foreach (Window w in windows)
            {
                if (w is FloatingWindow && w != win) continue;
                double distR = Math.Abs(left - (w.Left + w.Width));
                double distL = Math.Abs((left + win.Width) - w.Left);
                double distB = Math.Abs(top - (w.Top + w.Height));
                double distT = Math.Abs((top + win.Height) - w.Top);

                bool inBoundsVertical = (top > (w.Top - w.Height) && top < (w.Top + w.Height));
                if (distL < DockService.DockingThreshold && inBoundsVertical)
                {
                    VerticalDock.Width = 20; 
                    VerticalDock.Height = w.Height;
                    VerticalDock.Left = w.Left - 10;
                    VerticalDock.Top = w.Top;
                    VerticalDock.Show();
                    if (dock)
                    {
                        win.Left = w.Left - win.Width;
                    }
                }
                if (distR < DockService.DockingThreshold && inBoundsVertical)
                {
                    VerticalDock.Width = 20; 
                    VerticalDock.Height = w.Height;
                    VerticalDock.Left = w.Left + w.Width - 10;
                    VerticalDock.Top = w.Top;
                    VerticalDock.Show();
                    if (dock)
                    {
                        win.Left = w.Left + w.Width;
                    }
                }

                bool inBoundsHorizontal = (left > (w.Left - w.Width) && left < (w.Left + w.Width));
                if (distB < DockService.DockingThreshold && inBoundsHorizontal)
                {
                    HorizontalDock.Width = w.Width;
                    HorizontalDock.Height = 20; 
                    HorizontalDock.Left = w.Left;
                    HorizontalDock.Top = w.Top + w.Height - 10;
                    HorizontalDock.Show();
                    if (dock)
                    {
                        win.Top = w.Top + w.Height;
                    }
                }
                if (distT < DockService.DockingThreshold && inBoundsHorizontal)
                {
                    HorizontalDock.Width = w.Width;
                    HorizontalDock.Height = 20; 
                    HorizontalDock.Left = w.Left;
                    HorizontalDock.Top = w.Top - 10;
                    HorizontalDock.Show();
                    if (dock)
                    {
                        win.Top = w.Top - win.Height;
                    }
                }
            }
        }

        public static void HideDockBorders()
        {
            HorizontalDock.Hide();
            VerticalDock.Hide();
        }

        public static Screen FindScreenFromWindow(Window w)
        {
            return Screen.FromHandle(new WindowInteropHelper(w).Handle);
        }
    }
}
