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
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MorganStanley.ComposeUI.Playground.VisualUtils;

using static WindowStyles;
using static WindowLongFlags;
using static Win32Exports;

public class EmbeddedNativeControl : ContentPresenter
{
    #region ProcessId Styled Avalonia Property
    public int? ProcessId
    {
        get { return GetValue(ProcessIdProperty); }
        set { SetValue(ProcessIdProperty, value); }
    }

    public static readonly StyledProperty<int?> ProcessIdProperty =
        AvaloniaProperty.Register<EmbeddedNativeControl, int?>
        (
            nameof(ProcessId)
        );
    #endregion ProcessId Styled Avalonia Property

    IDisposable _subscription;
    public EmbeddedNativeControl()
    {
        _subscription =
            this.GetObservable(ProcessIdProperty).Subscribe(OnProcessIdChanged);
    }

    private void OnProcessIdChanged(int? newPath)
    {
        
        NativeHost oldHost = this.Content as NativeHost;
        if (oldHost != null)
        {
            oldHost.DetachWindow();
        }

        if (ProcessIdProperty != null && ProcessId.HasValue)
        {
            Process p = Process.GetProcessById(ProcessId.Value);
            this.Content = new NativeHost(p);
        }
        else
        {
            this.Content = null;
        }
    }      

    private class NativeHost : NativeControlHost
    {
        private Process _process;
        public IntPtr WindowHandle => _process.MainWindowHandle;

        public NativeHost(Process process)
        {
            _process = process;                
        }

        private Window _rootWindow;

        public void DetachWindow()
        {
            SetParent(WindowHandle, IntPtr.Zero);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _rootWindow = e.Root as Window;

            if (_rootWindow != null)
            {                    
                SetParent(WindowHandle, _rootWindow.PlatformImpl.Handle.Handle);

                long style = (long)GetWindowLongPtr(WindowHandle, (int)GWL_STYLE);

                style = (style & ~((uint)WS_POPUP | (uint)WS_CAPTION | (uint)WS_THICKFRAME | (uint)WS_MINIMIZEBOX | (uint)WS_MAXIMIZEBOX | (uint)WS_SYSMENU)) | (uint)WS_CHILD;

                SetWindowLongPtr(new HandleRef(null, WindowHandle), (int)GWL_STYLE, (IntPtr)style);
            }

            // force refreshing the handle
            MethodInfo methodInfo = 
                typeof(NativeControlHost)
                    .GetMethod("DestroyNativeControl", BindingFlags.Instance | BindingFlags.NonPublic);

            methodInfo.Invoke(this, null);
            //(this as NativeControlHost).CallMethodExtras("DestroyNativeControl", true, false);
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            SetParent(WindowHandle, IntPtr.Zero);

            base.OnDetachedFromVisualTree(e);
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new PlatformHandle(WindowHandle, "CTRL");
            }
            else
            {
                return base.CreateNativeControlCore(parent);
            }

        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //DestroyProcess();
            }
            else
            {
                base.DestroyNativeControlCore(control);
            }
        }

        //public void DestroyProcess()
        //{
        //    _process?.Kill(true);

        //    _process?.WaitForExit();

        //    _process?.Dispose();

        //    _process = null;
        //}
    }
}
