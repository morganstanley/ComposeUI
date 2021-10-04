using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DockManagerCore.Desktop
{
	internal enum FullScreenDockedMode
	{
		FullScreenFloating,
		FullScreenFloatingDisableActivate
	}

	internal class MaximizeFloatingWindowExtensionAdapter
	{
		 
        public static void FixResize(Window win_)
        {
            IntPtr handle = (new WindowInteropHelper(win_)).Handle;
            HwndSource.FromHwnd(handle).AddHook(WindowProc);
        }

		  
        internal static WindowState GetWindowState(IntPtr hwnd)
        {
            WindowState state;

            var placement = new Win32.WindowPlacement();
            placement.Length = Marshal.SizeOf(placement);
            bool retVal = Win32.GetWindowPlacement(hwnd, ref placement);

            if (placement.ShowCmd == Win32.SW_SHOWMINIMIZED)
                state = WindowState.Minimized;
            else if (placement.ShowCmd == Win32.SW_SHOWMAXIMIZED)
                state = WindowState.Maximized;
            else
            {
                state = WindowState.Normal;
            }

            return state;
        }
		 
	    private static IntPtr WindowProc(IntPtr hwnd,
												int msg,
												IntPtr wParam,
												IntPtr lParam,
												ref bool handled)
		{
			switch ((uint)msg)
			{

				case Win32.WM_WINDOWPOSCHANGING:
					var windowState2 = GetWindowState(hwnd);


			        if (windowState2 == WindowState.Maximized)
			        {

			            var val = (Win32.WINDOWPOS) Marshal.PtrToStructure(lParam, typeof (Win32.WINDOWPOS));

			            if (val.flags ==
			                (Win32.SetWindowsPosFlags.SWP_NOSIZE |
			                 Win32.SetWindowsPosFlags.SWP_NOZORDER |
			                 Win32.SetWindowsPosFlags.SWP_NOACTIVATE |
			                 Win32.SetWindowsPosFlags.SWP_NOOWNERZORDER))
			            {
			                val.flags |= Win32.SetWindowsPosFlags.SWP_NOMOVE;
			                Marshal.StructureToPtr(val, lParam, true);
			                handled = true;

			                return Win32.DefWindowProc(hwnd, msg, wParam, lParam);
			            }
			        }


			        break;

				//TODO the following block is commented because it doesn't set right minimum size of the window
				// as a result you can decrease size of a window in the way when its borders and header are partially cut

                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
			}

            return IntPtr.Zero; 
		}

		private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
		{
             

            Win32.POINT lMousePosition;
            Win32.GetCursorPos(out lMousePosition);

            IntPtr lPrimaryScreen = Win32.MonitorFromPoint(new Win32.POINT{X=0,Y = 0}, Win32.MonitorOptions.MONITOR_DEFAULTTOPRIMARY);
            Win32.MONITORINFO lPrimaryScreenInfo = new Win32.MONITORINFO();
            if (Win32.GetMonitorInfo(lPrimaryScreen, lPrimaryScreenInfo) == false)
            {
                return;
            }

            IntPtr lCurrentScreen = Win32.MonitorFromPoint(lMousePosition, Win32.MonitorOptions.MONITOR_DEFAULTTONEAREST);

            Win32.MINMAXINFO lMmi = (Win32.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Win32.MINMAXINFO));

            if (lPrimaryScreen.Equals(lCurrentScreen))
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcWork.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcWork.Right - lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcWork.Bottom - lPrimaryScreenInfo.rcWork.Top;
            }
            else
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcMonitor.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcMonitor.Right - lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcMonitor.Bottom - lPrimaryScreenInfo.rcMonitor.Top;
            }

            Marshal.StructureToPtr(lMmi, lParam, true);

		}
	}
}