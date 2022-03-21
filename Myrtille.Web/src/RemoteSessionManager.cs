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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class RemoteSessionManager : IDisposable
    {
        #region Init

        public RemoteSession RemoteSession { get; private set; }

        public RemoteSessionManager(RemoteSession remoteSession)
        {
            try
            {
                RemoteSession = remoteSession;

                // remote session process client and callback
                var callback = new RemoteSessionProcessClientCallback(this, HttpContext.Current.Application);
                var callbackContext = new InstanceContext(callback);
                HostClient = new RemoteSessionProcessClient(this, callbackContext);

                // clients
                Clients = new Dictionary<string, RemoteSessionClient>();
                ClientsLock = new object();

                // pipes
                Pipes = new RemoteSessionPipes(RemoteSession);

                // images event
                _imageEventLock = new object();

                // cache
                _cache = (Cache)HttpContext.Current.Application[HttpApplicationStateVariables.Cache.ToString()];

                // owner idle timeout
                if (!int.TryParse(ConfigurationManager.AppSettings["OwnerIdleTimeout"], out _ownerIdleTimeoutDelay))
                {
                    _ownerIdleTimeoutDelay = 0;
                }

                if (_ownerIdleTimeoutDelay > 0)
                {
                    OwnerIdleTimeout = new CancellationTokenSource();
                }

                // guests idle timeout
                _guestsIdleTimeout = new Dictionary<string, CancellationTokenSource>();
                _guestsIdleTimeoutLock = new object();

                // clients idle timeout
                _clientsIdleTimeout = new Dictionary<string, CancellationTokenSource>();
                _clientsIdleTimeoutLock = new object();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to initialize remote session manager, remote session {0} ({1})", RemoteSession?.Id, exc);
            }
        }

        #endregion

        #region Process

        public RemoteSessionProcessClient HostClient { get; private set; }

        #endregion

        #region Pipes

        public RemoteSessionPipes Pipes { get; private set; }

        public void ProcessUpdatesPipeData(byte[] data)
        {
            try
            {
                string message = null;

                // image structure: tag (4 bytes) + info (32 bytes) + data
                // > tag is used to identify an image (0: image; other: message)
                // > info contains the image metadata (idx, posX, posY, etc.)
                // > data is the image raw data

                var imgTag = BitConverter.ToInt32(data, 0);

                if (imgTag != 0)
                {
                    // RDP: UTF16-LE, 2 bytes (16 bits) per character
                    // SSH: UTF-8, 1 byte (8 bits) per character
                    message = RemoteSession.HostType == HostType.RDP ? Encoding.Unicode.GetString(data) : Encoding.UTF8.GetString(data);
                }

                // message
                if (!string.IsNullOrEmpty(message))
                {
                    // request page reload
                    if (message.Equals("reload"))
                    {
                        SendMessage(new RemoteSessionMessage { Type = MessageType.PageReload });
                    }
                    // remote clipboard
                    // truncated above max length
                    else if (message.StartsWith("clipboard|"))
                    {
                        RemoteSession.ClipboardText = message.Remove(0, 10);
                        SendMessage(new RemoteSessionMessage { Type = MessageType.RemoteClipboard, Text = message.Remove(0, 10) });
                    }
                    // SSH Terminal data
                    else if (message.StartsWith("term|"))
                    {
                        if (RemoteSession.State == RemoteSessionState.Connecting)
                        {
                            RemoteSession.State = RemoteSessionState.Connected;
                            SendMessage(new RemoteSessionMessage { Type = MessageType.Connected });
                        }

                        SendMessage(new RemoteSessionMessage { Type = MessageType.TerminalOutput, Text = message.Remove(0, 5) });
                    }
                    // print job
                    else if (message.StartsWith("printjob|"))
                    {
                        SendMessage(new RemoteSessionMessage { Type = MessageType.PrintJob, Text = message.Remove(0, 9) });
                    }
                }
                // image
                else
                {
                    if (RemoteSession.State == RemoteSessionState.Connecting)
                    {
                        RemoteSession.State = RemoteSessionState.Connected;
                        SendMessage(new RemoteSessionMessage { Type = MessageType.Connected });

                        // in case the remote session was reconnected, send the capture API config
                        SendCommand(RemoteSessionCommand.SetScreenshotConfig, string.Format("{0}|{1}|{2}", RemoteSession.ScreenshotIntervalSecs, (int)RemoteSession.ScreenshotFormat, RemoteSession.ScreenshotPath));
                    }

                    ProcessUpdate(data);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process updates pipe message, remote session {0} ({1})", RemoteSession?.Id, exc);
            }
        }

        public void ProcessAudioPipeData(byte[] data)
        {
            try
            {
                if (RemoteSession.State == RemoteSessionState.Connected)
                {
                    ProcessAudio(data);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process audio pipe message, remote session {0} ({1})", RemoteSession?.Id, exc);
            }
        }

        #endregion

        #region Commands

        public void SendCommand(RemoteSessionCommand command, string args = "")
        {
            try
            {
                if (RemoteSession.State == RemoteSessionState.NotConnected ||
                    RemoteSession.State == RemoteSessionState.Disconnected)
                    return;

                var commandWithArgs = string.Concat((string)RemoteSessionCommandMapping.ToPrefix[command], args);

                switch (command)
                {
                    // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the host client through the inputs pipe
                    // their values are set from the login page (using http(s) get or post) and shouldn't be modified at this step
                    case RemoteSessionCommand.SendServerAddress:
                    case RemoteSessionCommand.SendVMGuid:
                    case RemoteSessionCommand.SendUserDomain:
                    case RemoteSessionCommand.SendUserName:
                    case RemoteSessionCommand.SendUserPassword:
                    case RemoteSessionCommand.SendStartProgram:

                        if (RemoteSession.State != RemoteSessionState.Connecting)
                            return;

                        break;

                    // browser resize
                    case RemoteSessionCommand.SendBrowserResize:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        if (RemoteSession.BrowserResize == BrowserResize.None)
                            return;

                        if (_resizeDelayed)
                        {
                            if (_resizeTimeout != null)
                            {
                                _resizeTimeout.Cancel();
                                _resizeTimeout.Dispose();
                            }
                            _resizeTimeout = new CancellationTokenSource();
                            Task.Delay(500, _resizeTimeout.Token).ContinueWith(task =>
                            {
                                var parts = args.Split(new[] { "|" }, StringSplitOptions.None);
                                var resolution = parts[1].Split(new[] { "x" }, StringSplitOptions.None);
                                var width = int.Parse(resolution[0]);
                                var height = int.Parse(resolution[1]);

                                RemoteSession.ClientWidth = width < 100 ? 100 : width;
                                RemoteSession.ClientHeight = height < 100 ? 100 : height;

                                if (RemoteSession.BrowserResize == BrowserResize.Reconnect)
                                {
                                    RemoteSession.Reconnect = true;
                                    SendCommand(RemoteSessionCommand.CloseClient);
                                }
                                else if (RemoteSession.BrowserResize == BrowserResize.Scale)
                                {
                                    _resizeDelayed = false;
                                    SendCommand(RemoteSessionCommand.SendBrowserResize, args);
                                }
                            }, TaskContinuationOptions.NotOnCanceled);
                            return;
                        }
                        _resizeDelayed = true;
                        break;

                    // browser pulse
                    case RemoteSessionCommand.SendBrowserPulse:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        break;

                    // keyboard, mouse
                    case RemoteSessionCommand.SendKeyUnicode:
                    case RemoteSessionCommand.SendMouseMove:
                    case RemoteSessionCommand.SendMouseLeftButton:
                    case RemoteSessionCommand.SendMouseMiddleButton:
                    case RemoteSessionCommand.SendMouseRightButton:
                    case RemoteSessionCommand.SendMouseWheelUp:
                    case RemoteSessionCommand.SendMouseWheelDown:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        break;

                    case RemoteSessionCommand.SendKeyScancode:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        var keyCodeAndState = args.Split(new[] { "_" }, StringSplitOptions.None);

                        var jsKeyCode = int.Parse(keyCodeAndState[0]);
                        var keyState = keyCodeAndState[1];
                        var keyboardLayout = "en-us";
                        if (keyCodeAndState.Length > 2)
                        {
                            keyboardLayout = keyCodeAndState[2];
                            // if layout is not found, default to en-us
                            if (!JsKeyCodeToRdpScanCodeMapping.MapTable.ContainsKey(keyboardLayout))
                                keyboardLayout = "en-us";
                        }

                        var rdpScanCode = JsKeyCodeToRdpScanCodeMapping.MapTable[keyboardLayout][jsKeyCode] as RdpScanCode;
                        if (rdpScanCode != null && rdpScanCode.Value != 0)
                        {
                            commandWithArgs = string.Concat((string)RemoteSessionCommandMapping.ToPrefix[command], rdpScanCode.Value + "-" + keyState + "-" + (rdpScanCode.Extend ? "1" : "0"));
                        }
                        else
                        {
                            return;
                        }
                        break;

                    // control
                    case RemoteSessionCommand.SetScaleDisplay:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Display scaling {0}, remote session {1}", args != "0" ? args : "OFF", RemoteSession.Id);
                        RemoteSession.BrowserResize = args != "0" ? BrowserResize.Scale : BrowserResize.None;
                        break;

                    case RemoteSessionCommand.SetReconnectSession:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Session reconnect {0}, remote session {1}", args.StartsWith("1") ? "ON" : "OFF", RemoteSession.Id);
                        RemoteSession.BrowserResize = args.StartsWith("1") ? BrowserResize.Reconnect : BrowserResize.None;
                        break;

                    case RemoteSessionCommand.SetImageEncoding:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Image encoding {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                        RemoteSession.ImageEncoding = (ImageEncoding)int.Parse(args);
                        break;

                    case RemoteSessionCommand.SetImageQuality:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Image quality {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                        RemoteSession.ImageQuality = int.Parse(args);
                        break;

                    case RemoteSessionCommand.SetImageQuantity:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Image quantity {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                        RemoteSession.ImageQuantity = int.Parse(args);
                        break;

                    case RemoteSessionCommand.SetAudioFormat:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Audio format {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                        RemoteSession.AudioFormat = (AudioFormat)int.Parse(args);
                        break;

                    case RemoteSessionCommand.SetAudioBitrate:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        Trace.TraceInformation("Audio bitrate {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                        RemoteSession.AudioBitrate = int.Parse(args);
                        break;

                    case RemoteSessionCommand.SetScreenshotConfig:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        var config = args.Split(new[] { "|" }, StringSplitOptions.None);
                        RemoteSession.ScreenshotIntervalSecs = int.Parse(config[0]);
                        RemoteSession.ScreenshotFormat = (CaptureFormat)int.Parse(config[1]);
                        RemoteSession.ScreenshotPath = config[2];

                        Trace.TraceInformation("Screenshot config {0}, remote session {1}", args, RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.StartTakingScreenshots:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        if (_screenshotTimeout != null)
                        {
                            _screenshotTimeout.Cancel();
                            _screenshotTimeout.Dispose();
                        }
                        _screenshotTimeout = new CancellationTokenSource();
                        SendCommand(RemoteSessionCommand.TakeScreenshot);

                        Trace.TraceInformation("Starting taking screenshots {0}, remote session {1}", args, RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.StopTakingScreenshots:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        if (_screenshotTimeout != null)
                        {
                            _screenshotTimeout.Cancel();
                            _screenshotTimeout.Dispose();
                        }
                        _screenshotTimeout = null;

                        Trace.TraceInformation("Stopping taking screenshots {0}, remote session {1}", args, RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.TakeScreenshot:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        if (_screenshotTimeout != null)
                        {
                            _screenshotTimeout.Cancel();
                            _screenshotTimeout.Dispose();
                            _screenshotTimeout = new CancellationTokenSource();
                            Task.Delay(RemoteSession.ScreenshotIntervalSecs * 1000, _screenshotTimeout.Token).ContinueWith(task =>
                            {
                                SendCommand(RemoteSessionCommand.TakeScreenshot);
                            }, TaskContinuationOptions.NotOnCanceled);
                        }

                        FullscreenEventPending = true;

                        Trace.TraceInformation("Taking screenshot {0}, remote session {1}", args, RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.RequestFullscreenUpdate:

                        if (RemoteSession.State != RemoteSessionState.Connected)
                            return;

                        FullscreenEventPending = true;

                        Trace.TraceInformation("Requesting fullscreen update, all image(s) will now be discarded while waiting for it, remote session {0}", RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.SendLocalClipboard:

                        if ((RemoteSession.State != RemoteSessionState.Connecting) &&
                            (RemoteSession.State != RemoteSessionState.Connected))
                            return;

                        var clipboardText = string.Empty;

                        // read the clipboard text from unicode code points
                        var charsCodes = args.Split(new[] { "-" }, StringSplitOptions.None);
                        foreach (var charCode in charsCodes)
                        {
                            clipboardText += char.ConvertFromUtf32(int.Parse(charCode));
                        }

                        // truncated above max length, which was normally already enforced client side; re-checking
                        if (clipboardText.Length > _clipboardMaxLength)
                        {
                            clipboardText = clipboardText.Substring(0, _clipboardMaxLength) + "--- TRUNCATED ---";
                        }

                        // set the clipboard text on the gateway; if the remote session is reconnected, it will be sent to the new instance of wfreerdp
                        RemoteSession.ClipboardText = clipboardText;

                        commandWithArgs = string.Concat((string)RemoteSessionCommandMapping.ToPrefix[command], clipboardText);

                        Trace.TraceInformation("Sending local clipboard, remote session {0}", RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.ConnectClient:

                        if (RemoteSession.State != RemoteSessionState.Connecting)
                            return;

                        Trace.TraceInformation("Connecting remote session, remote session {0}", RemoteSession.Id);
                        break;

                    case RemoteSessionCommand.CloseClient:

                        if ((RemoteSession.State != RemoteSessionState.Connecting) &&
                            (RemoteSession.State != RemoteSessionState.Connected))
                            return;

                        RemoteSession.State = RemoteSessionState.Disconnecting;

                        Trace.TraceInformation("disconnecting remote session, remote session {0}", RemoteSession.Id);
                        break;
                }

                Trace.TraceInformation("Sending command with args {0}, remote session {1}", commandWithArgs, RemoteSession.Id);

                PipeHelper.WritePipeData(
                    Pipes.InputsPipe,
                    "remotesession_" + RemoteSession.Id + "_inputs",
                    commandWithArgs);
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to send command {0}, args {1}, remote session {2} ({3})", command, args, RemoteSession?.Id, exc);

                // there is a problem with the inputs pipe, force close the remote session in order to avoid it being stuck
                // it's usually not a big deal, some inputs being sent while the pipes are being disconnected when the host client is closed (and thus before the remote session state is set to disconnected), but better take no risk...
                HostClient.StopProcess();
            }
        }

        #endregion

        #region Inputs

        public void ProcessInputs(HttpSessionState session, string clientId, string data)
        {
            if (RemoteSession.State == RemoteSessionState.NotConnected ||
                RemoteSession.State == RemoteSessionState.Disconnected)
                return;

            try
            {
                // monitor the activity of the remote session owner; if its browser window/tab is closed without disconnecting first, or if the connection is lost, there won't be anymore input
                // in that case, disconnect the remote session after some time (set in web.config); if the session is shared, guests will be disconnected too
                // this comes in addition (but not replace) the session idle timeout which may defined (or not) for the remote server
                if (session.SessionID.Equals(RemoteSession.OwnerSessionID))
                {
                    if (OwnerIdleTimeout != null)
                    {
                        OwnerIdleTimeout.Cancel();
                        OwnerIdleTimeout.Dispose();
                        OwnerIdleTimeout = new CancellationTokenSource();
                        Task.Delay(_ownerIdleTimeoutDelay, OwnerIdleTimeout.Token).ContinueWith(task =>
                        {
                            if (RemoteSession.State == RemoteSessionState.Connecting ||
                                RemoteSession.State == RemoteSessionState.Connected)
                            {
                                SendCommand(RemoteSessionCommand.CloseClient);
                            }
                        }, TaskContinuationOptions.NotOnCanceled);
                    }
                }
                // same thing for guests, except that it doesn't disconnect the remote session
                // instead, it just frees a slot so that another invited guest can connect it
                else
                {
                    lock (_guestsIdleTimeoutLock)
                    {
                        if (!_guestsIdleTimeout.ContainsKey(session.SessionID))
                        {
                            _guestsIdleTimeout.Add(session.SessionID, new CancellationTokenSource());
                        }

                        var guestIdleTimeout = _guestsIdleTimeout[session.SessionID];
                        if (guestIdleTimeout != null)
                        {
                            guestIdleTimeout.Cancel();
                            guestIdleTimeout.Dispose();
                        }
                        guestIdleTimeout = new CancellationTokenSource();

                        // guest idle time is fixed at 1mn; because of the periodical FSUs (even for guests with no control) this can only happen if the guest closes its browser window/tab or if the connection is lost
                        Task.Delay(60000, guestIdleTimeout.Token).ContinueWith(task =>
                        {
                            if (RemoteSession.State != RemoteSessionState.Connected)
                                return;

                            session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                            if (session[HttpSessionStateVariables.GuestInfo.ToString()] != null)
                            {
                                // an inactive guest isn't removed, it just looses its slot; if there is an available slot afterward, the guest can reclaim it
                                ((GuestInfo)session[HttpSessionStateVariables.GuestInfo.ToString()]).Active = false;

                                if (RemoteSession.ActiveGuests > 0)
                                {
                                    RemoteSession.ActiveGuests--;
                                }
                            }

                            if (_guestsIdleTimeout.ContainsKey(session.SessionID))
                            {
                                _guestsIdleTimeout.Remove(session.SessionID);
                            }
                        }, TaskContinuationOptions.NotOnCanceled);

                        _guestsIdleTimeout[session.SessionID] = guestIdleTimeout;
                    }
                }

                // monitor the activity of the remote session client (browser window/tab),
                // regardless of whether it's the remote session owner or a guest, whose URL is shared or not
                // idle time is fixed at 1mn
                if (Clients.ContainsKey(clientId))
                {
                    lock (_clientsIdleTimeoutLock)
                    {
                        if (!_clientsIdleTimeout.ContainsKey(clientId))
                        {
                            _clientsIdleTimeout.Add(clientId, new CancellationTokenSource());
                        }

                        var clientIdleTimeout = _clientsIdleTimeout[clientId];
                        if (clientIdleTimeout != null)
                        {
                            clientIdleTimeout.Cancel();
                            clientIdleTimeout.Dispose();
                        }
                        clientIdleTimeout = new CancellationTokenSource();

                        Task.Delay(60000, clientIdleTimeout.Token).ContinueWith(task =>
                        {
                            if (Clients.ContainsKey(clientId))
                            {
                                lock (ClientsLock)
                                {
                                    Clients.Remove(clientId);
                                }
                            }
                        }, TaskContinuationOptions.NotOnCanceled);

                        _clientsIdleTimeout[clientId] = clientIdleTimeout;
                    }
                }

                var inputs = data.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var input in inputs)
                {
                    if (!string.IsNullOrEmpty(input))
                    {
                        var command = (RemoteSessionCommand)RemoteSessionCommandMapping.FromPrefix[input.Substring(0, 3)];

                        // if the remote session is shared, only the remote session owner and guests with control access can interact with it
                        // for the latter, such a control is limited to some actions (not sharing it with others persons or change the remote session configuration)
                        // only FSUs is allowed for anyone to update their display

                        // TODO: maintain a list of guests for the remote session, with different settings for each guest, then have different processings accordingly

                        if (session.SessionID.Equals(RemoteSession.OwnerSessionID) ||
                            (session[HttpSessionStateVariables.GuestInfo.ToString()] != null &&
                             ((GuestInfo)session[HttpSessionStateVariables.GuestInfo.ToString()]).Control &&
                              (command == RemoteSessionCommand.SendKeyUnicode ||
                               command == RemoteSessionCommand.SendKeyScancode ||
                               command == RemoteSessionCommand.SendMouseLeftButton ||
                               command == RemoteSessionCommand.SendMouseMiddleButton ||
                               command == RemoteSessionCommand.SendMouseRightButton ||
                               command == RemoteSessionCommand.SendMouseWheelUp ||
                               command == RemoteSessionCommand.SendMouseWheelDown ||
                               command == RemoteSessionCommand.SendMouseMove ||
                               command == RemoteSessionCommand.SendLocalClipboard)) ||
                            command == RemoteSessionCommand.RequestFullscreenUpdate)
                        {
                            SendCommand(command, input.Remove(0, 3));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process input(s) {0}, remote session {1} ({2})", data, RemoteSession?.Id, exc);
            }
        }

        #endregion

        #region Owner

        private bool _resizeDelayed = true;
        private CancellationTokenSource _resizeTimeout;

        private int _ownerIdleTimeoutDelay = 0;
        public CancellationTokenSource OwnerIdleTimeout { get; set; }

        #endregion

        #region Guests

        // TODO: better guests management from here

        private IDictionary<string, CancellationTokenSource> _guestsIdleTimeout;
        private object _guestsIdleTimeoutLock;

        #endregion

        #region Clients

        // a remote session client is a browser window/tab,
        // regardless of whether it's the remote session owner or a guest, whose URL is shared or not
        // it's given an unique id on each page load

        public IDictionary<string, RemoteSessionClient> Clients { get; private set; }
        public object ClientsLock { get; private set; }

        private IDictionary<string, CancellationTokenSource> _clientsIdleTimeout;
        private object _clientsIdleTimeoutLock;

        #endregion

        #region Updates

        // new image
        private void ProcessUpdate(byte[] data)
        {
            try
            {
                if (data.Length <= 36)
                    throw new Exception("invalid image data");

                // image info (8 items, 32 bits each => 8 * 4 = 32 bytes)
                var imgInfo = new byte[32];
                Array.Copy(data, 4, imgInfo, 0, 32);

                // CAUTION! if the remote session is reconnected (i.e.: browser resize), a new instance of wfreerdp is spawned
                // the image index can no longer be handled by wfreerdp, this must be done by the remote session manager
                // the image index provided by wfreerdp is relative to its own instance, if needed

                var image = new RemoteSessionImage
                {
                    //Idx = BitConverter.ToInt32(imgInfo, 0),
                    Idx = _imageIdx == int.MaxValue ? 1 : ++_imageIdx,
                    PosX = BitConverter.ToInt32(imgInfo, 4),
                    PosY = BitConverter.ToInt32(imgInfo, 8),
                    Width = BitConverter.ToInt32(imgInfo, 12),
                    Height = BitConverter.ToInt32(imgInfo, 16),
                    Format = (ImageFormat)BitConverter.ToInt32(imgInfo, 20),
                    Quality = BitConverter.ToInt32(imgInfo, 24),
                    Fullscreen = BitConverter.ToInt32(imgInfo, 28) == 1,
                    Data = new byte[data.Length - 36]
                };

                Array.Copy(data, 36, image.Data, 0, data.Length - 36);

                // cache the image, even if using websocket (used to retrieve the mouse cursor on IE)
                _cache.Insert(
                    "remoteSessionImage_" + RemoteSession.Id + "_" + image.Idx,
                    image,
                    null,
                    DateTime.Now.AddMilliseconds(_imageCacheDuration),
                    Cache.NoSlidingExpiration);

                Trace.TraceInformation("Received image {0} ({1}), remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);

                // a fullscreen update was requested
                if (FullscreenEventPending && image.Fullscreen)
                {
                    Trace.TraceInformation("Fullscreen update received, remote session {0}", RemoteSession.Id);
                    FullscreenEventPending = false;

                    // screenshot request
                    if (ScreenshotEventLock != null)
                    {
                        lock (ScreenshotEventLock)
                        {
                            // screenshot image index
                            ScreenshotImageIdx = image.Idx;

                            // if waiting for a screenshot, signal the reception
                            if (ScreenshotEventPending)
                            {
                                ScreenshotEventPending = false;
                                Monitor.Pulse(ScreenshotEventLock);
                            }
                        }
                    }
                }

                // send update to client(s)
                foreach (var client in Clients.Values)
                {
                    // websocket(s)
                    if (client.WebSockets != null && client.WebSockets.Count > 0)
                    {
                        // round robin on the client websockets down pool
                        if (client.WebSocketsRoundRobinIdx >= client.WebSockets.Count)
                        {
                            client.WebSocketsRoundRobinIdx = 0;
                        }

                        //Trace.TraceInformation("Sending image {0} ({1}) on websocket, client {2}, remote session {3}", image.Idx, (image.Fullscreen ? "screen" : "region"), client.Id, RemoteSession.Id);
                        client.WebSockets[client.WebSocketsRoundRobinIdx++].SendImage(image);
                    }
                    // event source
                    else if (client.EventSource != null)
                    {
                        //Trace.TraceInformation("Sending image {0} ({1}) on event source, client {2}, remote session {3}", image.Idx, (image.Fullscreen ? "screen" : "region"), client.Id, RemoteSession.Id);
                        client.EventSource.SendImage(image);
                    }
                    // long polling
                    else if (client.LongPolling != null)
                    {
                        //Trace.TraceInformation("Sending image {0} ({1}) on long polling, client {2}, remote session {3}", image.Idx, (image.Fullscreen ? "screen" : "region"), client.Id, RemoteSession.Id);
                        client.LongPolling.SendImage(image);
                    }
                    // xhr: updates are polled against the cache by the client
                }

                // image event
                lock (_imageEventLock)
                {
                    // last received image index
                    _lastReceivedImageIdx = image.Idx;

                    // if waiting for a new image, signal the reception
                    if (_imageEventPending)
                    {
                        _imageEventPending = false;
                        Monitor.Pulse(_imageEventLock);
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process update, remote session {0} ({1})", RemoteSession?.Id, exc);
            }
        }

        // retrieve a cached image
        public RemoteSessionImage GetCachedUpdate(int imageIdx)
        {
            RemoteSessionImage image = null;

            try
            {
                var imageObj = _cache["remoteSessionImage_" + RemoteSession.Id + "_" + imageIdx];
                if (imageObj != null)
                {
                    image = (RemoteSessionImage)imageObj;
                    Trace.TraceInformation("Retrieved image {0} ({1}) from cache, remote session {2}", imageIdx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve image {0} from cache, remote session {1} ({2})", imageIdx, RemoteSession?.Id, exc);
            }

            return image;
        }

        // retrieve the next image
        public RemoteSessionImage GetNextUpdate(int imageIdx, int? waitDuration = null)
        {
            RemoteSessionImage image = null;

            lock (_imageEventLock)
            {
                try
                {
                    // retrieve the next available image from cache, up to the latest received
                    if (imageIdx < _lastReceivedImageIdx)
                    {
                        for (var idx = imageIdx + 1; idx <= _lastReceivedImageIdx; idx++)
                        {
                            image = GetCachedUpdate(idx);
                            if (image != null)
                            {
                                break;
                            }
                        }
                    }

                    // if no image is available and a wait duration is specified, wait for a new image
                    if (image == null && waitDuration.HasValue)
                    {
                        Trace.TraceInformation("Waiting for new image, remote session {0}", RemoteSession.Id);
                        _imageEventPending = true;
                        if (waitDuration.Value > 0)
                        {
                            // wait for the specified time
                            if (Monitor.Wait(_imageEventLock, waitDuration.Value))
                            {
                                image = GetCachedUpdate(_lastReceivedImageIdx);
                            }
                        }
                        else
                        {
                            // wait indefinitely
                            if (Monitor.Wait(_imageEventLock))
                            {
                                image = GetCachedUpdate(_lastReceivedImageIdx);
                            }
                        }
                        _imageEventPending = false;
                        Monitor.Pulse(_imageEventLock);
                    }
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to retrieve next update from index {0}, remote session {1} ({2})", imageIdx, RemoteSession?.Id, exc);
                }
            }

            return image;
        }

        // retrieve the last image
        public RemoteSessionImage GetLastUpdate()
        {
            return GetCachedUpdate(_lastReceivedImageIdx);
        }

        #endregion

        #region Messages

        public void SendMessage(RemoteSessionMessage message)
        {
            // send message to client(s)
            foreach (var client in Clients.Values)
            {
                // websocket(s)
                if (client.WebSockets != null && client.WebSockets.Count > 0)
                {
                    // no round robin there; send message on the first websocket down
                    Trace.TraceInformation("Sending {0} notification on websocket, client {1}, remote session {2}", message.Type, client.Id, RemoteSession.Id);
                    client.WebSockets[0].SendMessage(message);
                }
                // event source
                else if (client.EventSource != null)
                {
                    Trace.TraceInformation("Sending {0} notification on event source, client {1}, remote session {2}", message.Type, client.Id, RemoteSession.Id);
                    client.EventSource.SendMessage(message);
                }
                // long polling
                else if (client.LongPolling != null)
                {
                    Trace.TraceInformation("Sending {0} notification on long polling, client {1}, remote session {2}", message.Type, client.Id, RemoteSession.Id);
                    client.LongPolling.SendMessage(message);
                }
                // xhr
                else if (client.MessageQueue != null)
                {
                    Trace.TraceInformation("Sending {0} notification on xhr, client {1}, remote session {2}", message.Type, client.Id, RemoteSession.Id);
                    lock (((ICollection)client.MessageQueue).SyncRoot)
                    {
                        client.MessageQueue.Add(message);
                    }
                }
            }

            StopWaitForImageEvent();
        }

        #endregion

        #region Images

        // image index
        private int _imageIdx = 0;

        // pending fullscreen update
        public bool FullscreenEventPending { get; private set; }

        // image reception (fullscreen and region)
        private object _imageEventLock;
        private bool _imageEventPending;

        // screenshot
        public object ScreenshotEventLock { get; set; }
        public bool ScreenshotEventPending { get; set; }
        public int ScreenshotImageIdx { get; private set; }

        // stop waiting for an image reception (a page reload is requested, the remote session is disconnected, etc.)
        public void StopWaitForImageEvent()
        {
            lock (_imageEventLock)
            {
                if (_imageEventPending)
                {
                    _imageEventPending = false;
                    Monitor.Pulse(_imageEventLock);
                }
            }
        }

        // last received image
        private int _lastReceivedImageIdx = 0;

        #endregion

        #region Audio

        // audio index
        private int _audioIdx = 0;

        // new audio
        private void ProcessAudio(byte[] data)
        {
            try
            {
                var audio = new RemoteSessionAudio
                {
                    Idx = _audioIdx == int.MaxValue ? 1 : ++_audioIdx,
                    Format = RemoteSession.AudioFormat.HasValue ? RemoteSession.AudioFormat.Value : AudioFormat.MP3,
                    Bitrate = RemoteSession.AudioBitrate.HasValue ? RemoteSession.AudioBitrate.Value : 128,
                    Data = data
                };

                _cache.Insert(
                    "remoteSessionAudio_" + RemoteSession.Id + "_" + audio.Idx,
                    audio,
                    null,
                    DateTime.Now.AddMilliseconds(_audioCacheDuration),
                    Cache.NoSlidingExpiration);

                Trace.TraceInformation("Received audio {0}, remote session {1}", audio.Idx, RemoteSession.Id);

                // send audio to client(s)
                foreach (var client in Clients.Values)
                {
                    // audio websocket
                    if (client.AudioWebSocket != null)
                    {
                        Trace.TraceInformation("Sending audio {0} on audio websocket, client {1}, remote session {2}", audio.Idx, client.Id, RemoteSession.Id);
                        client.AudioWebSocket.ProcessAudio(audio);
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process audio, remote session {0} ({1})", RemoteSession?.Id, exc);
            }
        }

        // retrieve a cached audio
        public RemoteSessionAudio GetCachedAudio(int audioIdx)
        {
            RemoteSessionAudio audio = null;

            try
            {
                var audioObj = _cache["remoteSessionAudio_" + RemoteSession.Id + "_" + audioIdx];
                if (audioObj != null)
                {
                    audio = (RemoteSessionAudio)audioObj;
                    Trace.TraceInformation("Retrieved audio {0} from cache, remote session {1}", audioIdx, RemoteSession.Id);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve audio {0} from cache, remote session {1} ({2})", audioIdx, RemoteSession?.Id, exc);
            }

            return audio;
        }

        #endregion

        #region Capture

        private CancellationTokenSource _screenshotTimeout;

        #endregion

        #region Clipboard

        // the clipboard must be limited in size or otherwise create too much network traffic and slowness; 1MB is usually enough for most copy/paste actions
        private const int _clipboardMaxLength = 1048576;

        #endregion

        #region Cache

        // when using polling (long-polling or xhr only), images or audio must be cached for a delayed retrieval; not applicable for websocket
        private Cache _cache;

        // cache lifetime (ms); that is, represents the maximal lag possible for a client, before having to drop some images or audio in order to catch up with the remote session (proceed with caution with these values!)
        private const int _imageCacheDuration = 1000;
        private const int _audioCacheDuration = 2000;

        #endregion

        #region IDisposable

        ~RemoteSessionManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (Pipes != null)
            {
                Pipes.DeletePipes();
            }

            foreach (var client in Clients.Values)
            {
                foreach (var webSocket in client.WebSockets)
                {
                    webSocket.Close();
                }

                if (client.AudioWebSocket != null)
                {
                    client.AudioWebSocket.Close();
                }
            }
        }

        #endregion
    }
}