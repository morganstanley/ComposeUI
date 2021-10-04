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
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace DockManagerCore.Desktop
{
    [SuppressUnmanagedCodeSecurity]
    [ComVisible(false)]
    public static class Win32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32")]
        internal static extern bool SetCursorPos(int x, int y);

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32")]
        internal static extern bool IsWindowEnabled(IntPtr hwnd);
        public static void ConvertToChildWindow(IntPtr hwnd)
        {
            SetWindowLong(hwnd, GWL_STYLE, WS_CHILD | WS_CLIPCHILDREN);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
        }

        public const int
            GWL_STYLE = -16,
            GWL_EX_STYLE = -20,

            VK_SHIFT = VK.VK_SHIFT,
            VK_CONTROL = VK.VK_CONTROL,
            VK_MENU = VK.VK_MENU;

        public const uint
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CAPTION = 0x00C00000,
            WS_DISABLED = 0x08000000,
            WS_SYSMENU = 0x00080000,
            WS_BORDER = 0x00800000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_THICKFRAME = 0x00040000,
            WS_VISIBLE = 0x10000000,
            WS_POPUP = 0x80000000,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_TOOLWINDOW = 0x00000080,

            SWP_FRAMECHANGED = 0x0020,
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOACTIVATE = 0x10,
            SWP_ASYNCWINDOWPOS = 16384,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400;

        public const int
            SW_SHOWNORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_SHOWNA = 0x0008,
            SW_SHOWNOACTIVATE = 0x0004,
            SW_PARENTCLOSING = 0x0001,
            SW_PARENTOPENING = 0x0003;


        public static readonly IntPtr TRUE = new IntPtr(1);
        public static readonly IntPtr FALSE = IntPtr.Zero;

        #region Constants

        public const uint
            WM_KEYFIRST = 0x0100,
            WM_KEYUP = 0x0101,
            WM_CHAR = 0x0102,
            WM_DEADCHAR = 0x0103,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_SYSCHAR = 0x0106,
            WM_SYSDEADCHAR = 0x0107,
            WM_KEYLAST = 0x0108,
            WM_IME_STARTCOMPOSITION = 0x010D,
            WM_IME_ENDCOMPOSITION = 0x010E,
            WM_IME_COMPOSITION = 0x010F,
            WM_IME_KEYLAST = 0x010F,
            WM_SYSCOMMAND = 0x00000112,
            WM_SETTEXT = 0x0000000C,
            WM_PAINT = 0x0000000F,
            WM_SETFOCUS = 0x00000007,
            WM_MOUSEACTIVATE = 0x00000021,
            WM_MDIACTIVATE = 0x00000222,
            WM_CHILDACTIVATE = 0x00000022,
            WM_ACTIVATE = 0x00000006,
            WM_ACTIVATEAPP = 0x0000001C,
            WM_LBUTTONDOWN = 0x00000201,
            WM_RBUTTONDOWN = 0x00000204,
            WM_MOUSEMOVE = 0x00000200,
            WM_MOUSELEAVE = 0x000002A3,
            WM_SIZE = 0x00000005,
            WM_SIZING = 0x00000214,
            WM_WINDOWPOSCHANGING = 0x000046,
            WM_WINDOWPOSCHANGED = 0x0000047,
            WM_MOVING = 0x00000216,
            WM_MOVE = 0x00000003,
            WM_LBUTTONUP = 0x00000202,
            WM_KEYDOWN = 0x00000100,
            WM_SHOWWINDOW = 0x0018,

            // non client area
            WM_NCCREATE = 0x00000081,
            WM_NCDESTROY = 0x00000082,
            WM_NCCALCSIZE = 0x00000083,
            WM_NCHITTEST = 0x00000084,
            WM_NCPAINT = 0x00000085,
            WM_NCACTIVATE = 0x00000086,

            // non client mouse
            WM_NCMOUSEMOVE = 0x000000A0,
            WM_NCLBUTTONDOWN = 0x000000A1,
            WM_NCLBUTTONUP = 0x000000A2,
            WM_NCLBUTTONDBLCLK = 0x000000A3,
            WM_NCRBUTTONDOWN = 0x000000A4,
            WM_NCRBUTTONUP = 0x000000A5,
            WM_NCRBUTTONDBLCLK = 0x000000A6,
            WM_NCMBUTTONDOWN = 0x000000A7,
            WM_NCMBUTTONUP = 0x000000A8,
            WM_NCMBUTTONDBLCLK = 0x000000A9,
            WM_NCMOUSELEAVE = 0x000002A2,
            WM_NCMOUSEHOVER = 0x000002A0,

            WM_USER = 0x0400;


        public const int MENU_IDENTIFIER = 14;

        public const int MIIM_STATE = 0x00000001;
        public const int MIIM_ID = 0x00000002;
        public const int MIIM_SUBMENU = 0x00000004;
        public const int MIIM_CHECKMARKS = 0x00000008;
        public const int MIIM_TYPE = 0x00000010;
        public const int MIIM_DATA = 0x00000020;
        public const int MIIM_STRING = 0x00000040;
        public const int MIIM_BITMAP = 0x00000080;
        public const int MIIM_FTYPE = 0x00000100;

        public const int MF_DEFAULT = 0x00001000;
        public const int MF_BYCOMMAND = 0x00000000;
        public const int MF_BYPOSITION = 0x00000400;
        public const int MF_SEPARATOR = 0x00000800;
        public const int MF_STRING = 0x00000000;
        public const int MF_CHECKED = 0x00000008;
        public const int MF_UNCHECKED = 0x00000000;

        public const int SC_MAXIMIZE = 0x0000F030;
        public const int SC_MINIMIZE = 0x0000F020;
        public const int SC_RESTORE = 0x0000F120;
        public const int SC_CLOSE = 0xF060;

        public const int MA_ACTIVATE = 1;
        public const int MA_ACTIVATEANDEAT = 2;
        public const int MA_NOACTIVATE = 3;
        public const int MA_NOACTIVATEANDEAT = 4;

        public const int GW_HWNDFIRST = 0;
        public const int GW_HWNDLAST = 1;
        public const int GW_HWNDNEXT = 2;
        public const int GW_HWNDPREV = 3;

        public const int HWND_BOTTOM = 1;
        public const int HWND_NOTOPMOST = -2;
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        public const int CWP_ALL = 0x0000;
        public const int CWP_SKIPINVISIBLE = 0x0001;
        public const int CWP_SKIPDISABLED = 0x0002;
        public const int CWP_SKIPTRANSPARENT = 0x0004;

        // tooltips
        public const string TOOLTIPS_CLASS = "tooltips_class32";

        public const int TTS_ALWAYSTIP = 0x01;
        public const int TTS_NOPREFIX = 0x02;
        public const int TTS_BALLOON = 0x40;

        public const int TTF_IDISHWND = 0x0001;
        public const int TTF_CENTERTIP = 0x0002;
        public const int TTF_SUBCLASS = 0x0010;
        public const int TTF_TRACK = 0x0020;
        public const int TTF_ABSOLUTE = 0x0080;
        public const int TTF_TRANSPARENT = 0x0100;

        public const uint TTM_ADDTOOL = WM_USER + 50;
        public const uint TTM_DELTOOL = WM_USER + 51;
        public const uint TTM_UPDATETIPTEXT = WM_USER + 57;
        public const uint TTM_TRACKACTIVATE = WM_USER + 17; // wParam = TRUE/FALSE start end  lparam = LPTOOLINFO
        public const uint TTM_TRACKPOSITION = WM_USER + 18; // lParam = dwPos

        public static class WA
        {
            public const int WA_INACTIVE = 0;
            public const int WA_ACTIVE = 1;
            public const int WA_CLICKACTIVATE = 2;
        }

        /// <summary>
        /// VK is just a placeholder for VK (VirtualKey) general definitions
        /// </summary>
        public static class VK
        {
            public const int VK_SHIFT = 0x10;
            public const int VK_CONTROL = 0x11;
            public const int VK_MENU = 0x12;
            public const int VK_ESCAPE = 0x1B;

            public static bool IsKeyPressed(int KeyCode)
            {
                return (GetAsyncKeyState(KeyCode) & 0x0800) == 0;
            }
        }

        public static class WMSZ
        {
            public const int WMSZ_BOTTOM = 6;
            public const int WMSZ_BOTTOMLEFT = 7;
            public const int WMSZ_BOTTOMRIGHT = 8;
            public const int WMSZ_LEFT = 1;
            public const int WMSZ_RIGHT = 2;
            public const int WMSZ_TOP = 3;
            public const int WMSZ_TOPLEFT = 4;
            public const int WMSZ_TOPRIGHT = 5;
        }

        /// <summary>
        /// HT is just a placeholder for HT (HitTest) definitions
        /// </summary>
        public static class HT
        {
            public const int HTERROR = (-2);
            public const int HTTRANSPARENT = (-1);
            public const int HTNOWHERE = 0;
            public const int HTCLIENT = 1;
            public const int HTCAPTION = 2;
            public const int HTSYSMENU = 3;
            public const int HTGROWBOX = 4;
            public const int HTSIZE = HTGROWBOX;
            public const int HTMENU = 5;
            public const int HTHSCROLL = 6;
            public const int HTVSCROLL = 7;
            public const int HTMINBUTTON = 8;
            public const int HTMAXBUTTON = 9;
            public const int HTLEFT = 10;
            public const int HTRIGHT = 11;
            public const int HTTOP = 12;
            public const int HTTOPLEFT = 13;
            public const int HTTOPRIGHT = 14;
            public const int HTBOTTOM = 15;
            public const int HTBOTTOMLEFT = 16;
            public const int HTBOTTOMRIGHT = 17;
            public const int HTBORDER = 18;
            public const int HTREDUCE = HTMINBUTTON;
            public const int HTZOOM = HTMAXBUTTON;
            public const int HTSIZEFIRST = HTLEFT;
            public const int HTSIZELAST = HTBOTTOMRIGHT;

            public const int HTOBJECT = 19;
            public const int HTCLOSE = 20;
            public const int HTHELP = 21;
        }

        #endregion Constants

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowPlacement
        {
            public int Length;
            public int Flags;
            public int ShowCmd;
            public POINT MinPosition;
            public POINT MaxPosition;
            public RECT NormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowInfo
        {
            public int cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;
        }

        #region SetWindowsPosFlags enum

        [Flags]
        public enum SetWindowsPosFlags : uint
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOREDRAW = 0x0008,
            SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020, /* The frame changed: send WM_NCCALCSIZE */
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200, /* Don't do owner Z ordering */
            SWP_NOSENDCHANGING = 0x0400, /* Don't send WM_WINDOWPOSCHANGING */
            SWP_DEFERERASE = 0x2000,
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DRAWFRAME = SWP_FRAMECHANGED,
            SWP_NOREPOSITION = SWP_NOOWNERZORDER,
        }

        #endregion SetWindowsPosFlags enum

        #region RedrawWindowOptions enum

        [Flags]
        public enum RedrawWindowOptions
        {
            RDW_INVALIDATE = 0x0001,
            RDW_INTERNALPAINT = 0x0002,
            RDW_ERASE = 0x0004,
            RDW_VALIDATE = 0x0008,
            RDW_NOINTERNALPAINT = 0x0010,
            RDW_NOERASE = 0x0020,
            RDW_NOCHILDREN = 0x0040,
            RDW_ALLCHILDREN = 0x0080,
            RDW_UPDATENOW = 0x0100,
            RDW_ERASENOW = 0x0200,
            RDW_FRAME = 0x0400,
            RDW_NOFRAME = 0x0800
        }

        #endregion RedrawWindowOptions enum

        public sealed class SetWindowPosFlags
        {
            private SetWindowPosFlags()
            {
            }

            public const int SWP_NOSIZE = 0x0001,
                             SWP_NOMOVE = 0x0002,
                             SWP_NOZORDER = 0x0004,
                             SWP_NOREDRAW = 0x0008,
                             SWP_NOACTIVATE = 0x0010,
                             SWP_FRAMECHANGED = 0x0020,
                /* The frame changed: send WM_NCCALCSIZE */
                             SWP_SHOWWINDOW = 0x0040,
                             SWP_HIDEWINDOW = 0x0080,
                             SWP_NOCOPYBITS = 0x0100,
                             SWP_NOOWNERZORDER = 0x0200,
                /* Don't do owner Z ordering */
                             SWP_NOSENDCHANGING = 0x0400,
                /* Don't send WM_WINDOWPOSCHANGING */
                             SWP_DEFERERASE = 0x2000,
                             SWP_ASYNCWINDOWPOS = 0x4000,
                             SWP_DRAWFRAME = SWP_FRAMECHANGED,
                             SWP_NOREPOSITION = SWP_NOOWNERZORDER;
        }

        public sealed class ShowWindowCommands
        {
            private ShowWindowCommands()
            {
            }

            public const int SW_HIDE = 0,
                             SW_SHOWNORMAL = 1,
                             SW_NORMAL = 1,
                             SW_SHOWMINIMIZED = 2,
                             SW_SHOWMAXIMIZED = 3,
                             SW_MAXIMIZE = 3,
                             SW_SHOWNOACTIVATE = 4,
                             SW_SHOW = 5,
                             SW_MINIMIZE = 6,
                             SW_SHOWMINNOACTIVE = 7,
                             SW_SHOWNA = 8,
                             SW_RESTORE = 9,
                             SW_SHOWDEFAULT = 10,
                             SW_FORCEMINIMIZE = 11,
                             SW_MAX = 11;
        }

        public sealed class FormatMessageFlags
        {
            private FormatMessageFlags()
            {
            }

            public const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
            public const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000;
            public const int FORMAT_MESSAGE_FROM_HMODULE = 0x800;
            public const int FORMAT_MESSAGE_FROM_STRING = 0x400;
            public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
            public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
            public const int FORMAT_MESSAGE_MAX_WIDTH_MASK = 0xFF;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public SetWindowsPosFlags flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            internal Point Point => new Point(X, Y);

            internal static POINT FromPoint(Point point)
            {
                return new POINT
                {
                    X = point.X,
                    Y = point.Y
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENUITEMINFOW
        {
            public int cbSize;
            public int fMask;
            public int fType;
            public int fState;
            public int wid;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public int dwItemData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string dwTypeData;
            public int cch;
            public IntPtr hbmpItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public Rectangle Rect => new Rectangle(Left, Top, Right - Left, Bottom - Top);

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public static RECT FromRectangle(Rectangle rect)
            {
                return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            /// <summary>
            /// Computes the length of this rectange.
            /// </summary>
            /// <returns>Length of this rectangle</returns>
            public int GetLength()
            {
                return Right - Left;
            }

            /// <summary>
            /// Computes teh height of this rectangle.
            /// </summary>
            /// <returns>Height of this rectangle.</returns>
            public int GetHeight()
            {
                return Bottom - Top;
            }

            public override string ToString()
            {
                return string.Format("RECT: ({0},{1})-({2},{3})", Left, Top, Right, Bottom);
            }

            /// <summary>
            /// Determines if 2 passed RECT instances are equal.
            /// </summary>
            /// <param name="r1">First comparable instance.</param>
            /// <param name="r2">Second comparable instance.</param>
            /// <returns>True if the passed instances are equal, false otherwise.</returns>
            public static bool Equals(RECT r1, RECT r2)
            {
                return
                    r1.Left == r2.Left
                    && r1.Top == r2.Top
                    && r1.Right == r2.Right
                    && r1.Bottom == r2.Bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WPFRECT
        {
            public double Left;
            public double Top;
            public double Right;
            public double Bottom;

            public WPFRECT(double left, double top, double right, double bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public Rect Rect => new Rect(Left, Top, Right - Left, Bottom - Top);

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public static RECT FromRectangle(Rectangle rect)
            {
                return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            public override string ToString()
            {
                return string.Format("RECT: ({0},{1})-({2},{3})", Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTS
        {
            public byte x;
            public byte y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOOLINFO
        {
            public int cbSize;
            public int uFlags;
            public IntPtr hwnd;
            public IntPtr uId;
            public RECT rect;
            public IntPtr hinst;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszText;
            public uint lParam;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITIORINFOEX
        {
            internal int _size;
            internal RECT _rcMonitor;
            internal RECT _rcWork;
            internal int _flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            internal char[] _device;

            internal MONITIORINFOEX()
            {
                _size = Marshal.SizeOf(typeof(MONITIORINFOEX));
                _rcMonitor = new RECT();
                _rcWork = new RECT();
                _device = new char[0x20];
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            /// <summary>
            /// Contains the new coordinates of a window that has been moved or resized, that is, it is the proposed new window coordinates.
            /// </summary>
            public RECT rectProposed;

            /// <summary>
            /// Contains the coordinates of the window before it was moved or resized.
            /// </summary>
            public RECT rectBeforeMove;

            /// <summary>
            /// Contains the coordinates of the window's client area before the window was moved or resized.
            /// </summary>
            public RECT rectClientBeforeMove;

            /// <summary>
            /// Pointer to a WINDOWPOS structure that contains the size and position values specified in the operation that moved or resized the window.
            /// </summary>
            public WINDOWPOS lpPos;
        }

        public enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002
        }

        #endregion Structs

        #region Declarations

        [DllImport("user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr window, ref WindowPlacement position);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr window, ref WindowPlacement position);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32")]
        public static extern int MapWindowPoints(IntPtr hwndSrc, IntPtr hwndDest, [In, Out] ref POINT pt, int ptCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int SetCapture(int hwnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr handle_);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetDCEx(IntPtr hwnd, IntPtr hrgnclip, uint fdwOptions);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr handle_, IntPtr dcHandle_);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowRect(IntPtr handle_, ref RECT rect_);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32")]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo pwi);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool InsertMenuItemW(
            IntPtr hMenu, // handle to menu
            int uItem, // identifier or position
            bool fByPosition, // meaning of uItem
            ref MENUITEMINFOW lpmii // menu item information
            );

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool InsertMenuW(
            IntPtr hMenu, // handle to menu
            int uPosition, // item that new item precedes
            int uFlags, // options
            int uIDNewItem, // identifier, menu, or submenu
            string lpNewItem // menu item content
            );

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool SetMenuItemInfoW(
            IntPtr hMenu, // handle to menu
            uint uItem, // identifier or position
            bool fByPosition, // meaning of uItem
            MENUITEMINFOW lpmii // menu item information
            );

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu(IntPtr hMenu, int position, int uFlags);

        [DllImport("user32.dll")]
        public static extern bool CheckMenuItem(IntPtr hmenu, int uIDCheckItem, int uCheck);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr window, int index, int value);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                               int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginDeferWindowPos(int nNumWindows);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DeferWindowPos(IntPtr hWinPosInfo, IntPtr hWnd,
                                                   IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool EndDeferWindowPos(IntPtr hWinPosInfo);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindow", SetLastError = true)]
        public static extern IntPtr GetNextWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.U4)] int wFlag);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
        public static extern bool IsAppThemed();

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
        public static extern bool IsThemeActive();

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCurrentThemeName(char[] themeName, int nameSize, char[] colorName, int colorSize,
                                                      char[] sizeName, int sizeSize);

        [DllImport("uxtheme.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenThemeData(IntPtr hwnd, String pszClassList);

        [DllImport("uxtheme.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetThemePartSize(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId,
                                                  ref Rectangle prc, THEMESIZE eSize, ref Size psz);

        [DllImport("uxtheme.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DrawThemeBackground(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId,
                                                     ref Rectangle pRect, IntPtr pClipRect);

        [DllImport("uxtheme.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseThemeData(IntPtr htheme);

        [DllImport("kernel32")]
        public static extern int GetLastError();

        [DllImport("kernel32")]
        public static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId,
                                               StringBuilder lpBuffer, int nSize, int Arguments);

        public static string GetAPIErrorMessageDescription(int apiErrNumber_)
        {
            StringBuilder sError = new StringBuilder(512);
            int lErrorMessageLength;
            lErrorMessageLength = FormatMessage(FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM, (IntPtr)0, apiErrNumber_,
                                                0, sError, sError.Capacity, 0);

            if (lErrorMessageLength > 0)
            {
                string strgError = sError.ToString();
                strgError = strgError.Substring(0, strgError.Length - 2);
                return strgError + " (" + apiErrNumber_ + ")";
            }
            return "none";
        }

        public static Bitmap PrintWindow(IntPtr hwnd)
        {
            var rc = new RECT();
            GetWindowRect(hwnd, ref rc);

            var bmp = new Bitmap(rc.GetLength(), rc.GetHeight(), PixelFormat.Format32bppArgb);
            using (var gfxBmp = Graphics.FromImage(bmp))
            {
                var hdcBitmap = gfxBmp.GetHdc();
                PrintWindow(hwnd, hdcBitmap, 0);
                gfxBmp.ReleaseHdc(hdcBitmap);
            }

            return bmp;
        }

        [DllImport("comctl32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool _TrackMouseEvent(TRACKMOUSEEVENT tme);

        public static bool TrackMouseEvent(TRACKMOUSEEVENT tme)
        {
            return _TrackMouseEvent(tme);
        }

        #region Macros

        public static int HIWORD(int n)
        {
            return ((n >> 16) & 0xffff /*=~0x0000*/);
        }

        public static int LOWORD(int n)
        {
            return (n & 0xffff /*=~0x0000*/);
        }

        public static int LOWORD(IntPtr n)
        {
            return LOWORD((int)n);
        }

        public static int HIWORD(IntPtr n)
        {
            return HIWORD((int)n);
        }

        public static int MAKELONG(int low, int high)
        {
            return ((high << 16) | (low & 0xffff));
        }

        public static int MAKELPARAM(int low, int high)
        {
            return ((high << 16) | (low & 0xffff));
        }

        #endregion Macros

        #endregion Declarations

        #region Enums

        #region DCX enum

        [Flags]
        internal enum DCX
        {
            DCX_CACHE = 0x2,
            DCX_CLIPCHILDREN = 0x8,
            DCX_CLIPSIBLINGS = 0x10,
            DCX_EXCLUDERGN = 0x40,
            DCX_EXCLUDEUPDATE = 0x100,
            DCX_INTERSECTRGN = 0x80,
            DCX_INTERSECTUPDATE = 0x200,
            DCX_LOCKWINDOWUPDATE = 0x400,
            DCX_NORECOMPUTE = 0x100000,
            DCX_NORESETATTRS = 0x4,
            DCX_PARENTCLIP = 0x20,
            DCX_VALIDATE = 0x200000,
            DCX_WINDOW = 0x1,
        }

        #endregion //DCX

        public enum THEMESIZE
        {
            TS_MIN,
            TS_TRUE,
            TS_DRAW
        }

        public enum WINDOW_CLASSPARTS
        {
            WP_CAPTION = 1,
            WP_SMALLCAPTION = 2,
            WP_MINCAPTION = 3,
            WP_SMALLMINCAPTION = 4,
            WP_MAXCAPTION = 5,
            WP_SMALLMAXCAPTION = 6,
            WP_FRAMELEFT = 7,
            WP_FRAMERIGHT = 8,
            WP_FRAMEBOTTOM = 9,
            WP_SMALLFRAMELEFT = 10,
            WP_SMALLFRAMERIGHT = 11,
            WP_SMALLFRAMEBOTTOM = 12,
            WP_SYSBUTTON = 13,
            WP_MDISYSBUTTON = 14,
            WP_MINBUTTON = 15,
            WP_MDIMINBUTTON = 16,
            WP_MAXBUTTON = 17,
            WP_CLOSEBUTTON = 18,
            WP_SMALLCLOSEBUTTON = 19,
            WP_MDICLOSEBUTTON = 20,
            WP_RESTOREBUTTON = 21,
            WP_MDIRESTOREBUTTON = 22,
            WP_HELPBUTTON = 23,
            WP_MDIHELPBUTTON = 24,
            WP_HORZSCROLL = 25,
            WP_HORZTHUMB = 26,
            WP_VERTSCROLL = 27,
            WP_VERTTHUMB = 28,
            WP_DIALOG = 29,
            WP_CAPTIONSIZINGTEMPLATE = 30,
            WP_SMALLCAPTIONSIZINGTEMPLATE = 31,
            WP_FRAMELEFTSIZINGTEMPLATE = 32,
            WP_SMALLFRAMELEFTSIZINGTEMPLATE = 33,
            WP_FRAMERIGHTSIZINGTEMPLATE = 34,
            WP_SMALLFRAMERIGHTSIZINGTEMPLATE = 35,
            WP_FRAMEBOTTOMSIZINGTEMPLATE = 36,
            WP_SMALLFRAMEBOTTOMSIZINGTEMPLATE = 37
        }

        public enum CAPTIONBUTTON_STATEPARTS
        {
            CBS_NORMAL = 1,
            CBS_HOT = 2,
            CBS_PUSHED = 3,
            CBS_DISABLED = 4
        }

        #endregion Enums

        #region Public Methods

        public static void SetWindowPlacement(IntPtr handle_, Rectangle bounds_)
        {
            RECT rect = new RECT();
            rect.Left = bounds_.Left;
            rect.Top = bounds_.Top;
            rect.Right = bounds_.Left + bounds_.Width;
            rect.Bottom = bounds_.Top + bounds_.Height;

            WindowPlacement placement = new WindowPlacement();
            GetWindowPlacement(handle_, ref placement);
            placement.NormalPosition = rect;
            placement.Length = Marshal.SizeOf(placement);
            SetWindowPlacement(handle_, ref placement);
        }

        #endregion Public Methods

        #region TRACKMOUSEEVENT structure

        [StructLayout(LayoutKind.Sequential)]
        public sealed class TRACKMOUSEEVENT
        {
            public TRACKMOUSEEVENT()
            {
                cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
                dwHoverTime = 100;
            }

            public int cbSize;
            public int dwFlags;
            public IntPtr hwndTrack;
            public int dwHoverTime;
        }

        #endregion

        #region TrackMouseEventFalgs enum

        [Flags]
        public enum TrackMouseEventFalgs
        {
            TME_HOVER = 1,
            TME_LEAVE = 2,
            TME_NONCLIENT = 0x00000010,
            TME_QUERY = 0x40000000,
        }

        #endregion
    }
}
