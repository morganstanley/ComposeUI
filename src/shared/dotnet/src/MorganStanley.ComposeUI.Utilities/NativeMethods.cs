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

using System.Runtime.InteropServices;
using System;

namespace MorganStanley.ComposeUI.Utilities;

public static class NativeMethods
{
    /// <summary>
    /// Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object.
    /// </summary>
    /// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
    /// <returns>
    /// true if the function succeeds; otherwise, false. If the function fails, call <see cref="Marshal.GetLastWin32Error"/> for extended error information.
    /// </returns>
    /// <remarks>
    /// See: https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-deleteobject
    /// </remarks>
    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteObject(in IntPtr hObject);
}