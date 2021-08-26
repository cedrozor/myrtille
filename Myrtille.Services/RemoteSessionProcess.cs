/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2021 Cedric Coste

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
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using Myrtille.Services.ConnectionBroker;
using Myrtille.Services.Contracts;
using Cassia;

namespace Myrtille.Services
{
    public class RemoteSessionProcess : IRemoteSessionProcess, IDisposable
    {
        #region Drain

        // CAUTION! the 2 methods below require some permissions to operate; running Myrtille.Services under the (default) "LocalSystem" account may not be sufficient
        // a domain account with the proper privileges would be best:
        // to access the connection broker database: https://docs.microsoft.com/en-us/sql/relational-databases/security/permissions-database-engine?view=sql-server-2017
        // to call the RDS API: https://docs.microsoft.com/en-gb/windows/win32/termserv/terminal-services-permissions

        private static void DrainDisconnectedSessions(
            string userDomain,
            string userName,
            string initialProgram)
        {
            try
            {
                using (var db = new ConnectionBrokerDbContext())
                {
                    var sessions = db.Session.Where(session =>
                        (string.IsNullOrEmpty(userDomain) ? true : session.UserDomain == userDomain) &&
                        session.UserName == userName &&
                        session.InitialProgram == initialProgram &&
                        session.State == (byte)SessionState.Disconnected &&
                        session.ProtocolType == (byte)SessionType.RdpTcp).ToList();

                    // 0 disconnected session: a new session will be created
                    // 1 disconnected session: the disconnected session will be reconnected
                    // n disconnected sessions: logoff the oldest disconnected session(s) so that the latest one will be reconnected without prompting the user to choose
                    if (sessions.Count > 1)
                    {
                        var sessionsToLogoff = sessions.OrderBy(session => session.DisconnectTime).Take(sessions.Count - 1);
                        foreach (var session in sessionsToLogoff)
                        {
                            LogoffSession(session.Target.Netbios, session.SessionId);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to drain disconnected session(s) for user {0} and application {1} ({2})", string.IsNullOrEmpty(userDomain) ? userName : string.Format("{0}\\{1}", userDomain, userName), initialProgram, exc);
            }
        }

        private static void LogoffSession(
            string serverName,
            int sessionId)
        {
            try
            {
                var manager = new TerminalServicesManager();
                using (var server = manager.GetRemoteServer(serverName))
                {
                    server.Open();
                    var sessionToLogoff = server.GetSessions().Where(session => session.SessionId == sessionId).FirstOrDefault();
                    if (sessionToLogoff != null)
                    {
                        sessionToLogoff.Logoff(true);
                    }
                    server.Close();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to logoff session id {0} on server {1} ({2})", sessionId, serverName, exc);
            }
        }

        #endregion

        #region process

        private Guid _remoteSessionId;
        private Process _process;
        private IRemoteSessionProcessCallback _callback;

        public void StartProcess(
            Guid remoteSessionId,
            HostType hostType,
            SecurityProtocol securityProtocol,
            string serverAddress,
            string vmGuid,
            string userDomain,
            string userName,
            string startProgram,
            int clientWidth,
            int clientHeight,
            bool allowRemoteClipboard,
            bool allowPrintDownload,
            bool allowAudioPlayback,
            string loadBalanceInfo)
        {
            Trace.TraceInformation("Connecting remote session {0}, type {1}, security {2}, server (:port) {3}, vm {4}, domain {5}, user {6}, program {7}",
                remoteSessionId,
                hostType,
                hostType == HostType.RDP ? securityProtocol.ToString().ToUpper() : "N/A",
                serverAddress,
                hostType == HostType.RDP ? (string.IsNullOrEmpty(vmGuid) ? "(none)" : vmGuid) : "N/A",
                string.IsNullOrEmpty(userDomain) ? "(none)" : userDomain,
                userName,
                hostType == HostType.RDP ? (string.IsNullOrEmpty(startProgram) ? "(none)" : startProgram) : "N/A");

            try
            {
                // set the remote session id
                // the wcf service binding "wsDualHttpBinding" is "perSession" by default (maintain 1 service instance per client)
                // as there is 1 client per remote session, the remote session id is set for the current service instance
                _remoteSessionId = remoteSessionId;

                // prevent the user to be prompted for the session to reconnect in case of several disconnected sessions
                bool drainDisconnectedSessions;
                if (!bool.TryParse(ConfigurationManager.AppSettings["DrainDisconnectedSessions"], out drainDisconnectedSessions))
                {
                    drainDisconnectedSessions = false;
                }

                if (drainDisconnectedSessions)
                {
                    DrainDisconnectedSessions(userDomain, userName, startProgram);
                }

                _process = new Process();

                // select the host client executable based on the host type
                var clientFilePath = string.Empty;
                var clientFileName = string.Empty;
                switch (hostType)
                {
                    // see https://github.com/cedrozor/myrtille/blob/master/DOCUMENTATION.md#build for information and steps to build FreeRDP along with myrtille
                    case HostType.RDP:
                        clientFilePath = @"Myrtille.RDP\bin";
                        clientFileName = "Myrtille.RDP.exe";
                        break;
                    case HostType.SSH:
                        clientFilePath = @"Myrtille.SSH\bin";
                        clientFileName = "Myrtille.SSH.exe";
                        break;
                }

                if (Environment.UserInteractive)
                {
                    _process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.Replace(@"Myrtille.Services\bin", clientFilePath), clientFileName);
                }
                else
                {
                    _process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, clientFileName);
                }

                // ensure the host client executable does exists
                if (!File.Exists(_process.StartInfo.FileName))
                {
                    var msg = string.Format("The host client executable ({0}) is missing. Please read documentation for steps to build it", _process.StartInfo.FileName);
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

                #region RDP

                if (hostType == HostType.RDP)
                {
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

                    // the params below have optimal (performance oriented) values and shouldn't be changed (except in case of any issue)
                    bool compression;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPCompression"], out compression))
                    {
                        compression = true;
                    }

                    bool mouseMotion;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPMouseMotion"], out mouseMotion))
                    {
                        mouseMotion = false;
                    }

                    bool bitmapCache;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPBitmapCache"], out bitmapCache))
                    {
                        bitmapCache = true;
                    }

                    bool offscreenCache;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPOffscreenCache"], out offscreenCache))
                    {
                        offscreenCache = false;
                    }

                    bool glyphCache;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPGlyphCache"], out glyphCache))
                    {
                        glyphCache = true;
                    }

                    bool asyncInput;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPAsyncInput"], out asyncInput))
                    {
                        asyncInput = false;
                    }

                    bool asyncUpdate;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPAsyncUpdate"], out asyncUpdate))
                    {
                        asyncUpdate = false;
                    }

                    bool asyncChannels;
                    if (!bool.TryParse(ConfigurationManager.AppSettings["FreeRDPAsyncChannels"], out asyncChannels))
                    {
                        asyncChannels = false;
                    }

                    // pdf virtual printer redirection

                    // TOCHECK: for some reason, using the exact pdf virtual printer driver name ("PDF Scribe Virtual Printer") doesn't work (the printer doesn't show into the remote session) with wfreerdp, while it works with mstsc (!)
                    // it may have something to do with the driver not being installed on the remote server, but as the underlying driver is the standard "Microsoft Postscript Printer Driver (v3)" (pscript5.dll), it should have worked...
                    // as a workaround for now, the same way freerdp does in CUPS mode (printer redirection under Linux), it's possible to use the "MS Publisher Imagesetter" driver; it's also based on pscript5.dll and support basic print features (portrait/landscape orientation, custom fonts, color mode, etc.)

                    // as the rdp server uses the client numlock state, ensure it's off
                    // server side, ensure that HKEY_USERS\.DEFAULT\Control Panel\Keyboard: InitialKeyboardIndicators is set to 0 (numlock off)
                    SetNumLock(false);

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
                        (compression ? " +" : " -") + "compression" +                                                               // bulk compression (level is autodetected from the rdp version)
                        (certIgnore ? " /cert-ignore" : string.Empty) +                                                             // ignore certificate warning (when using NLA)
                        (allowPrintDownload ? " /printer:\"Myrtille PDF\",\"MS Publisher Imagesetter\"" : string.Empty) +           // pdf virtual printer
                        (mouseMotion ? " +" : " -") + "mouse-motion" +                                                              // mouse motion
                        (bitmapCache ? " +" : " -") + "bitmap-cache" +                                                              // bitmap cache
                        (offscreenCache ? " +" : " -") + "offscreen-cache" +                                                        // offscreen cache
                        (glyphCache ? " +" : " -") + "glyph-cache" +                                                                // glyph cache
                        (asyncInput ? " +" : " -") + "async-input" +                                                                // async input
                        (asyncUpdate ? " +" : " -") + "async-update" +                                                              // async update
                        (asyncChannels ? " +" : " -") + "async-channels" +                                                          // async channels
                        (allowRemoteClipboard ? " +" : " -") + "clipboard" +                                                        // clipboard support
                        (securityProtocol != SecurityProtocol.auto ? " /sec:" + securityProtocol.ToString() : string.Empty) +       // security protocol
                        (allowAudioPlayback ? " /sound" : string.Empty) +                                                           // sound support
                        " /audio-mode:" + (allowAudioPlayback ? "0" : "2") +                                                        // audio mode (0: redirect, 1: play on server, 2: do not play)
                        (!string.IsNullOrEmpty(loadBalanceInfo) ? " /load-balance-info:\"" + loadBalanceInfo +"\"" : string.Empty);
                }

                #endregion

                #region SSH

                else
                {
                    _process.StartInfo.Arguments =
                        "/myrtille-sid:" + _remoteSessionId +                                                                       // session id
                        (!Environment.UserInteractive ? string.Empty : " /myrtille-window") +                                       // session window
                        (!remoteSessionLog ? string.Empty : " /myrtille-log") +                                                     // session log
                        " /w:" + clientWidth +                                                                                      // display width
                        " /h:" + clientHeight;                                                                                      // display height
                }

                #endregion

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
                Trace.TraceError("Failed to start the host client process, remote session {0} ({1})", _remoteSessionId, exc);
            }
        }

        public void StopProcess()
        {
            // RDP: after closing the client, the remote session remains active on the server and is resumed on a subsequent connection of the same user...
            // to avoid this, set the rdp session disconnect timeout to a low value (ie: 1 sec)
            // it can be done in the registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxDisconnectionTime" (DWORD, value in msecs)
            // see http://ts.veranoest.net/ts_faq_configuration.htm

            if (_process != null && !_process.HasExited)
            {
                Trace.TraceInformation("Stopping (kill) the host client process, remote session {0}", _remoteSessionId);

                try
                {
                    _process.Kill();
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to stop (kill) the host client process, remote session {0} ({1})", _remoteSessionId, exc);
                }
            }
        }

        public string GetProcessIdentity()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// the host client process has exited
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

            // ssh session close cases:
            // > user clicks the "Disconnect" button
            // > user enters the "exit" shell command
            // > the ssh session is disconnected/closed/lost for any reason (invalid credentials, idle timeout, connection lost, etc.)

            if (_process != null && _process.HasExited)
            {
                Trace.TraceInformation("Disconnected remote session {0}, exit code {1}", _remoteSessionId, _process.ExitCode);

                try
                {
                    // invoke the callback asynchronously and possibly in a separate thread to avoid any deadlock
                    Task.Factory.StartNew(() =>
                    {
                        ProcessExitedCallback();
                    });
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to invoke the process exited callback asynchronously, trying synchronously, remote session {0} ({1})", _remoteSessionId, exc);
                    ProcessExitedCallback();
                }
            }
        }

        private void ProcessExitedCallback()
        {
            try
            {
                // notify the remote session manager of the process exit
                _callback.ProcessExited(_process.ExitCode);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to notify the host client process exit (MyrtilleAppPool down?), remote session {0} ({1})", _remoteSessionId, exc);
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