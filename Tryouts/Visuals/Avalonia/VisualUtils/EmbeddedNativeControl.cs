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
using System.Threading.Tasks;

namespace MorganStanley.ComposeUI.Tryouts.Visuals.Avalonia.VisualUtils
{
    using static WindowStyles;
    using static WindowLongFlags;
    using static Win32Exports;

    public class EmbeddedNativeControl : ContentPresenter
    {
        #region ProcessExePath Styled Avalonia Property
        public string ProcessExePath
        {
            get { return GetValue(ProcessExePathProperty); }
            set { SetValue(ProcessExePathProperty, value); }
        }

        public static readonly StyledProperty<string> ProcessExePathProperty =
            AvaloniaProperty.Register<EmbeddedNativeControl, string>
            (
                nameof(ProcessExePath)
            );
        #endregion ProcessExePath Styled Avalonia Property

        IDisposable _subscription;
        public EmbeddedNativeControl()
        {
            _subscription =
                this.GetObservable(ProcessExePathProperty).Subscribe(OnProcessExePathChanged);
        }

        private async void OnProcessExePathChanged(string newPath)
        {
            await LoadChildWindow();
        }

        public async Task LoadChildWindow()
        {
            NativeHost oldHost = this.Content as NativeHost;
            if (oldHost != null)
            {
                oldHost.DestroyProcess();
            }

            if (ProcessExePath != null)
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = ProcessExePath
                };

                Process p = Process.Start(processStartInfo);

                await Task.Delay(2000);

                this.Content = new NativeHost(p);
            }
        }

        private class NativeHost : NativeControlHost
        {
            private Process _process;

            public IntPtr WindowHandle => _process.MainWindowHandle;

            public NativeHost(Process process)
            {
                _process = process;
                _process.EnableRaisingEvents = true;

                _process.Exited += OnProcessExited;
            }

            private void OnProcessExited(object sender, EventArgs e)
            {
                SetParent(WindowHandle, IntPtr.Zero);

            }

            private Window _rootWindow;

            protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
            {
                if (_rootWindow != null)
                {
                    _rootWindow.Closed -= _rootWindow_Closed;
                }

                _rootWindow = e.Root as Window;

                if (_rootWindow != null)
                {
                    _rootWindow.Closed += _rootWindow_Closed;

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

            private void _rootWindow_Closed(object sender, EventArgs e)
            {
                this.DestroyProcess();
            }

            protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
            {
                if (_rootWindow != null)
                {
                    _rootWindow.Closed -= _rootWindow_Closed;
                }

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

            public void DestroyProcess()
            {
                _process?.Kill(true);

                _process?.WaitForExit();

                _process?.Dispose();

                _process = null;
            }
        }
    }
}
