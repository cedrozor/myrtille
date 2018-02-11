/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

// Implements IWin32Window where Handle returns the handle of the system's foreground window.
// Can be used with MessageBox to display the box in front of the active window, such as:
//    MessageBox.Show(ActiveWindow.Active, "Hello, World!");

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Myrtille.Helpers
{
    /// <summary>Used to get an IWin32Window for the active window.</summary>
    public class ActiveWindow : IWin32Window
    {
        #region Private Members
        /// <summary>Static instance to use for factory pattern.</summary>
        private static ActiveWindow _window = new ActiveWindow();
        #endregion

        #region Construction
        /// <summary>Prevent external instantiation.</summary>
        private ActiveWindow() { }
        #endregion

        #region Public Properties
        /// <summary>Gets an IWin32Window for the active window.</summary>
        public static IWin32Window Active { get { return _window; } }
        #endregion

        #region Private Functions
        /// <summary>Finds the frontmost window using the Win32 API.</summary>
        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        /// <summary>Gets a handle to the active window.</summary>
        IntPtr IWin32Window.Handle
        {
            get { return new IntPtr(GetForegroundWindow()); }
        }
        #endregion
    }
}