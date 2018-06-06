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

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Windows.Forms;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class RemoteSessionProcess : IRemoteSessionProcess, IDisposable
    {
        #region process

        private int _remoteSessionId;
        private Process _process;
        private IRemoteSessionProcessCallback _callback;

        //FreeRDP - A Free Remote Desktop Protocol Client (https://github.com/FreeRDP/FreeRDP)
        //Usage: https://github.com/awakecoding/FreeRDP-Manuals/blob/master/User/FreeRDP-User-Manual.markdown
        public void StartProcess(
            int remoteSessionId,
            string serverAddress,
            string userDomain,
            string userName,
            string startProgram,
            int clientWidth,
            int clientHeight,
            bool allowRemoteClipboard,
            SecurityProtocolEnum securityProtocol,
            HostTypeEnum hostType)
        {
            Trace.TraceInformation("Connecting remote session {0}, server {1}, domain {2}, user {3}, program {4}", remoteSessionId, serverAddress, string.IsNullOrEmpty(userDomain) ? "(none)" : userDomain, userName, string.IsNullOrEmpty(startProgram) ? "(none)" : startProgram);

            try
            {
                // set the remote session id
                // the wcf service binding "wsDualHttpBinding" is "perSession" by default (maintain 1 service instance per client)
                // as there is 1 client per remote session, the remote session id is set for the current service instance
                _remoteSessionId = remoteSessionId;

                // as the rdp server uses the client numlock state, ensure it's off
                // server side, ensure that HKEY_USERS\.DEFAULT\Control Panel\Keyboard: InitialKeyboardIndicators is set to 0 (numlock off)
                SetNumLock(false);

                _process = new Process();

                // see https://github.com/cedrozor/myrtille/blob/master/DOCUMENTATION.md#build for information and steps to build FreeRDP along with myrtille

                // Select host client process to use based on the type of host connection
                var hostProgram = "";
                switch (hostType)
                {
                    case HostTypeEnum.RDP:
                        hostProgram = "wfreerdp.exe";
                        break;
                    case HostTypeEnum.SSH:
                        hostProgram = "Myrtille.SSH.exe";
                        break;
                }

                if (Environment.UserInteractive)
                {
                    _process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.Replace(@"Myrtille.Services\bin", @"Myrtille.RDP\FreeRDP"), hostProgram);
                }
                else
                {
                    _process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, hostProgram);
                }

                // ensure the FreeRDP executable does exists
                if (!File.Exists(_process.StartInfo.FileName))
                {
                    var msg = "The FreeRDP executable is missing. Please read documentation for steps to build it";
                    if (Environment.UserInteractive)
                    {
                        MessageBox.Show(msg);
                    }
                    Trace.TraceError(msg);
                    return;
                }

                // log remote session events into a file (located into <Myrtille folder>\log)
                bool remoteSessionLog;
                if (!bool.TryParse(ConfigurationManager.AppSettings["RemoteSessionLog"], out remoteSessionLog))
                {
                    remoteSessionLog = false;
                }

                #region FreeRDP params

                // color depth
                int bpp;
                if (!int.TryParse(ConfigurationManager.AppSettings["FreeRDPBpp"], out bpp))
                {
                    bpp = 16;
                }

                // gdi mode (sw: software, hw: hardware). default software because there is a palette issue with windows server 2008; also, the performance gain is small and even null on most virtual machines, when hardware isn't available
                var gdi = "sw";
                if (ConfigurationManager.AppSettings["FreeRDPGdi"] != null)
                {
                    gdi = ConfigurationManager.AppSettings["FreeRDPGdi"];
                }

                // wallpaper
                bool wallpaper;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPWallpaper"], out wallpaper))
                {
                    wallpaper = false;
                }

                // desktop composition
                bool aero;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPAero"], out aero))
                {
                    aero = false;
                }

                // window drag
                bool windowDrag;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPWindowDrag"], out windowDrag))
                {
                    windowDrag = false;
                }

                // menu animations
                bool menuAnims;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPMenuAnims"], out menuAnims))
                {
                    menuAnims = false;
                }

                // themes
                bool themes;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPThemes"], out themes))
                {
                    themes = false;
                }

                // smooth fonts (requires ClearType enabled on the remote server)
                bool smoothFonts;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPSmoothFonts"], out smoothFonts))
                {
                    smoothFonts = true;
                }

                // ignore certificate warning (when using NLA); may happen, for example, with a self-signed certificate (not trusted) or if the server joined a domain after the certificate was issued (name mismatch). more details here: http://www.vkernel.ro/blog/configuring-certificates-in-2012r2-remote-desktop-services-rds
                bool certIgnore;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPCertIgnore"], out certIgnore))
                {
                    certIgnore = true;
                }

                // pdf virtual printer redirection

                // TOCHECK: for some reason, using the exact pdf virtual printer driver name ("PDF Scribe Virtual Printer") doesn't work (the printer doesn't show into the remote session) with wfreerdp, while it works with mstsc (!)
                // it may have something to do with the driver not being installed on the remote server, but as the underlying driver is the standard "Microsoft Postscript Printer Driver (v3)" (pscript5.dll), it should have worked...
                // as a workaround for now, the same way freerdp does in CUPS mode (printer redirection under Linux), it's possible to use the "MS Publisher Imagesetter" driver; it's also based on pscript5.dll and support basic print features (portrait/landscape orientation, custom fonts, color mode, etc.)

                bool pdfPrinter;
                if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPPdfPrinter"], out pdfPrinter))
                {
                    pdfPrinter = true;
                }

                #endregion

                // https://github.com/FreeRDP/FreeRDP/wiki/CommandLineInterface
                // Syntax: /flag enables flag, +toggle or -toggle enables or disables toggle. /toggle and +toggle are the same. Options with values work like this: /option:<value>
                // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the rdp client through the inputs pipe
                _process.StartInfo.Arguments =
                    "/myrtille-sid:" + _remoteSessionId +                                                                       // session id
                    (!Environment.UserInteractive ? string.Empty : " /myrtille-window") +                                       // session window
                    (!remoteSessionLog ? string.Empty : " /myrtille-log") +                                                     // session log
                    " /w:" + clientWidth +                                                                                      // display width
                    " /h:" + clientHeight +                                                                                     // display height
                    " /bpp:" + bpp +                                                                                            // color depth
                    " /gdi:" + gdi +                                                                                            // gdi mode (sw: software, hw: hardware)
                    (wallpaper ? " +" : " -") + "wallpaper" +                                                                   // wallpaper
                    (aero ? " +" : " -") + "aero" +                                                                             // desktop composition
                    (windowDrag ? " +" : " -") + "window-drag" +                                                                // window drag
                    (menuAnims ? " +" : " -") + "menu-anims" +                                                                  // menu animations
                    (themes ? " +" : " -") + "themes" +                                                                         // themes
                    (smoothFonts ? " +" : " -") + "fonts" +                                                                     // smooth fonts (requires ClearType enabled on the remote server)
                    " +compression" +                                                                                           // bulk compression (level is autodetected from the rdp version)
                    (certIgnore ? " /cert-ignore" : string.Empty) +                                                             // ignore certificate warning (when using NLA)
                    (pdfPrinter ? " /printer:\"Myrtille PDF\",\"MS Publisher Imagesetter\"" : string.Empty) +                   // pdf virtual printer
                    " -mouse-motion" +                                                                                          // mouse motion
                    " +bitmap-cache" +                                                                                          // bitmap cache
                    " -offscreen-cache" +                                                                                       // offscreen cache
                    " +glyph-cache" +                                                                                           // glyph cache
                    " -async-input" +                                                                                           // async input
                    " -async-update" +                                                                                          // async update
                    " -async-channels" +                                                                                        // async channels
                    " -async-transport" +                                                                                       // async transport
                    (allowRemoteClipboard ? " +" : " -") + "clipboard" +                                                        // clipboard support
                    (securityProtocol != SecurityProtocolEnum.auto ? " /sec:" + securityProtocol.ToString() : string.Empty) +   // security protocol
                    " /audio-mode:2";                                                                                           // audio mode (not supported for now, 2: do not play)

                if (!Environment.UserInteractive)
                {
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.RedirectStandardError = true;
                    _process.StartInfo.RedirectStandardInput = true;
                    _process.StartInfo.RedirectStandardOutput = true;
                    _process.StartInfo.CreateNoWindow = true;
                    _process.StartInfo.ErrorDialog = false;
                    _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                _process.EnableRaisingEvents = true;
                _process.Exited += ProcessExited;

                // set the callback instance
                // the wcf service binding "wsDualHttpBinding" allows for duplex communication
                _callback = OperationContext.Current.GetCallbackChannel<IRemoteSessionProcessCallback>();

                _process.Start();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to start rdp client process, remote session {0} ({1})", _remoteSessionId, exc);
            }
        }

        public void StopProcess()
        {
            // after closing the client, the rdp session does remains active on the server and is resumed on a subsequent connection of the same user...
            // to avoid this, we set the rdp session disconnect timeout to a low value (ie: 1 sec)
            // it can be done in the registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxDisconnectionTime" (DWORD, value in msecs)
            // see http://ts.veranoest.net/ts_faq_configuration.htm

            if (_process != null && !_process.HasExited)
            {
                Trace.TraceInformation("Stopping (kill) rdp client process, remote session {0}", _remoteSessionId);

                try
                {
                    _process.Kill();
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to stop (kill) rdp client process, remote session {0} ({1})", _remoteSessionId, exc);
                }
            }
        }

        public string GetProcessIdentity()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// the rdp client process has exited
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessExited(
            object sender,
            EventArgs e)
        {
            // rdp session close cases:
            // > user standard logout (using the windows "start" menu) or rdp session is killed server side: the rdp session is closed immediately, the rdp client process exits automatically
            // > user click the "Disconnect" button or rdp session is disconnected for any reason (i.e.: rdp server down, rdp connection lost, rdp client closed, etc.): rdp client process is closed, rdp session is closed 1 sec after (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxDisconnectionTime=1000" (DWORD, value in msecs))
            // > user is inactive: after a given idle timeout (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxIdleTime" (DWORD, value in msecs)), the user gets a 2mn warning before disconnection; if it aknowledges it, the session is resumed... otherwise it's closed 2mn after; the rdp client process exits automatically
            // > user close its browser/tab, goes offline or switch off its computer: the rdp session is closed after a given idle timeout (plus 2mn, because the user won't be able to acknowledge the warning message as mentioned above); the rdp client process exits automatically

            // also interesting to note, it's possible to set a MaxConnectionTime for the rdp session (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxConnectionTime" (DWORD, value in msecs))
            // an alternative to alter the registry directly (which impact the whole server) is to define group policies strategies (GPOs) into the Active Directory; it's a bit more complicated to handle, but proper...

            if (_process != null && _process.HasExited)
            {
                Trace.TraceInformation("Disconnected remote session {0}, exit code {1}", _remoteSessionId, _process.ExitCode);

                try
                {
                    // notify the remote session manager of the process exit
                    _callback.ProcessExited(_process.ExitCode);
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to notify rdp client process exit (MyrtilleAppPool down?), remote session {0} ({1})", _remoteSessionId, exc);
                }
                finally
                {
                    if (_process != null)
                    {
                        _process.Dispose();
                        _process = null;
                    }
                }
            }
        }

        #endregion

        #region NumLock

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_NUMLOCK = 0x90;
        private const uint KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYDOWN = 0x0;
        private const int KEYEVENTF_KEYUP = 0x2;

        private static bool GetNumLock()
        {
            return (((ushort)GetKeyState(0x90)) & 0xffff) != 0;
        }

        private static void SetNumLock(bool state)
        {
            if (GetNumLock() != state)
            {
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
        }

        #endregion

        #region IDisposable

        ~RemoteSessionProcess()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            StopProcess();
        }

        #endregion
    }
}