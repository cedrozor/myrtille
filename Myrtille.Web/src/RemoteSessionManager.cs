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
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Caching;
using Myrtille.Helpers;

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
                var callback = new RemoteSessionProcessClientCallback(this);
                var callbackContext = new InstanceContext(callback);
                Client = new RemoteSessionProcessClient(this, callbackContext);

                // pipes
                Pipes = new RemoteSessionPipes(RemoteSession);
                Pipes.ProcessUpdatesPipeMessage = ProcessUpdatesPipeMessage;

                // sockets
                WebSockets = new List<RemoteSessionSocketHandler>();

                // messages
                _messageEventLock = new object();

                // images
                _imageEventLock = new object();

                // cache
                _imageCache = (Cache)HttpContext.Current.Application[HttpApplicationStateVariables.Cache.ToString()];
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to initialize remote session manager, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        #endregion

        #region Client

        public RemoteSessionProcessClient Client { get; private set; }

        #endregion

        #region Pipes

        public RemoteSessionPipes Pipes { get; private set; }

        private void ProcessUpdatesPipeMessage(byte[] msg)
        {
            try
            {
                string message = null;

                // image structure: tag (4 bytes) + info (32 bytes) + data
                // > tag is used to identify an image (0: image; other: message)
                // > info contains the image metadata (idx, posX, posY, etc.)
                // > data is the image raw data
                if (BitConverter.ToInt32(msg, 0) != 0)
                {
                    message = Encoding.UTF8.GetString(msg);
                }

                // message
                if (!string.IsNullOrEmpty(message))
                {
                    // request page reload
                    if (message.Equals("reload"))
                    {
                        if (WebSockets.Count > 0)
                        {
                            Trace.TraceInformation("Sending reload request on websocket(s), remote session {0}", RemoteSession.Id);
                            foreach (var webSocket in WebSockets)
                            {
                                webSocket.Send(message);
                            }
                        }
                        else
                        {
                            ReloadPage = true;
                            StopWaitForImageEvent();
                        }
                    }
                    // remote clipboard is available
                    else if (message.StartsWith("clipboard|"))
                    {
                        if (WebSockets.Count > 0)
                        {
                            Trace.TraceInformation("Sending clipboard content {0} on websocket(s), remote session {1}", message, RemoteSession.Id);
                            foreach (var webSocket in WebSockets)
                            {
                                webSocket.Send(message);
                            }
                        }
                        else
                        {
                            ClipboardText = message.Remove(0, 10);
                            ClipboardAvailable = true;
                            StopWaitForImageEvent();
                        }
                    }
                }
                // image
                else
                {
                    if (RemoteSession.State == RemoteSessionState.Connecting)
                    {
                        // remote session is now connected
                        RemoteSession.State = RemoteSessionState.Connected;
                    }

                    ProcessUpdate(msg);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process updates pipe message, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        #endregion

        #region Sockets

        public List<RemoteSessionSocketHandler> WebSockets { get; set; }

        #endregion

        #region Commands

        public void SendCommand(RemoteSessionCommand command, string args = "")
        {
            if (RemoteSession.State == RemoteSessionState.NotConnected || RemoteSession.State == RemoteSessionState.Disconnected)
                return;

            var commandWithArgs = string.Concat((string)RemoteSessionCommandMapping.ToPrefix[command], args);

            switch (command)
            {
                // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the rdp client through the inputs pipe
                // their values are set from the login page (using http(s) post), they shouldn't be modified at this step
                case RemoteSessionCommand.SendServerAddress:
                case RemoteSessionCommand.SendUserDomain:
                case RemoteSessionCommand.SendUserName:
                case RemoteSessionCommand.SendUserPassword:
                case RemoteSessionCommand.SendStartProgram:
                    // if needed
                    break;

                // browser, keyboard, mouse, etc.
                case RemoteSessionCommand.SendBrowserResize:
                case RemoteSessionCommand.SendKeyUnicode:
                case RemoteSessionCommand.SendMouseMove:
                case RemoteSessionCommand.SendMouseLeftButton:
                case RemoteSessionCommand.SendMouseMiddleButton:
                case RemoteSessionCommand.SendMouseRightButton:
                case RemoteSessionCommand.SendMouseWheelUp:
                case RemoteSessionCommand.SendMouseWheelDown:
                    // if needed
                    break;

                case RemoteSessionCommand.SendKeyScancode:
                    var keyCodeAndState = args.Split(new[] { "-" }, StringSplitOptions.None);

                    var jsKeyCode = int.Parse(keyCodeAndState[0]);
                    var keyState = keyCodeAndState[1];

                    var rdpScanCode = JsKeyCodeToRdpScanCodeMapping.MapTable[jsKeyCode];
                    if (rdpScanCode != null && (int)rdpScanCode != 0)
                    {
                        commandWithArgs = string.Concat((string)RemoteSessionCommandMapping.ToPrefix[command], (int)rdpScanCode + "-" + keyState);
                    }
                    break;

                // control
                case RemoteSessionCommand.SetStatMode:
                    Trace.TraceInformation("Stat mode {0}, remote session {1}", args == "1" ? "ON" : "OFF", RemoteSession.Id);
                    RemoteSession.StatMode = args == "1";
                    break;

                case RemoteSessionCommand.SetDebugMode:
                    Trace.TraceInformation("Debug mode {0}, remote session {1}", args == "1" ? "ON" : "OFF", RemoteSession.Id);
                    RemoteSession.DebugMode = args == "1";
                    break;

                case RemoteSessionCommand.SetCompatibilityMode:
                    Trace.TraceInformation("Compatibility mode {0}, remote session {1}", args == "1" ? "ON" : "OFF", RemoteSession.Id);
                    RemoteSession.CompatibilityMode = args == "1";
                    break;

                case RemoteSessionCommand.SetScaleDisplay:
                    Trace.TraceInformation("Display scaling {0}, remote session {1}", args != "0" ? args : "OFF", RemoteSession.Id);
                    RemoteSession.ScaleDisplay = args != "0";
                    break;

                case RemoteSessionCommand.SetImageEncoding:
                    Trace.TraceInformation("Image encoding {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                    RemoteSession.ImageEncoding = (ImageEncoding)int.Parse(args);
                    break;

                case RemoteSessionCommand.SetImageQuality:
                    Trace.TraceInformation("Image quality {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                    RemoteSession.ImageQuality = int.Parse(args);
                    break;

                case RemoteSessionCommand.SetImageQuantity:
                    Trace.TraceInformation("Image quantity {0}, remote session {1}", int.Parse(args), RemoteSession.Id);
                    RemoteSession.ImageQuantity = int.Parse(args);
                    break;

                case RemoteSessionCommand.RequestFullscreenUpdate:
                    Trace.TraceInformation("Requesting fullscreen update, all image(s) will now be discarded while waiting for it, remote session {0}", RemoteSession.Id);
                    FullscreenEventPending = true;
                    break;

                case RemoteSessionCommand.RequestRemoteClipboard:
                    Trace.TraceInformation("Requesting remote clipboard, remote session {0}", RemoteSession.Id);
                    break;

                case RemoteSessionCommand.ConnectRdpClient:
                    Trace.TraceInformation("Connecting remote session, remote session {0}", RemoteSession.Id);
                    break;

                case RemoteSessionCommand.CloseRdpClient:
                    Trace.TraceInformation("Closing remote session, remote session {0}", RemoteSession.Id);
                    break;
            }

            Trace.TraceInformation("Sending command with args {0}, remote session {1}", commandWithArgs, RemoteSession.Id);

            try
            {
                PipeHelper.WritePipeMessage(
                    Pipes.InputsPipe,
                    "remotesession_" + RemoteSession.Id + "_inputs",
                    commandWithArgs + "\t");
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send command {0}, args {1}, remote session {2} ({3})", command, args, RemoteSession.Id, exc);

                // there is a problem with the inputs pipe, force close the remote session in order to avoid it being stuck
                Client.StopProcess();
            }
        }

        #endregion

        #region Inputs

        public void ProcessInputs(string data)
        {
            try
            {
                var inputs = data.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var input in inputs)
                {
                    if (!string.IsNullOrEmpty(input))
                    {
                        SendCommand((RemoteSessionCommand)RemoteSessionCommandMapping.FromPrefix[input.Substring(0, 3)], input.Remove(0, 3));
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process input(s) {0}, remote session {1} ({2})", data, RemoteSession.Id, exc);
            }
        }

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

                var image = new RemoteSessionImage
                {
                    Idx = BitConverter.ToInt32(imgInfo, 0),
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
                _imageCache.Insert(
                    "remoteSessionImage_" + RemoteSession.Id + "_" + image.Idx,
                    image,
                    null,
                    DateTime.Now.AddMilliseconds(_imageCacheDuration),
                    Cache.NoSlidingExpiration);

                Trace.TraceInformation("Received image {0} ({1}), remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);

                // while a fullscreen update is pending, discard all updates; the fullscreen will replace all of them
                if (FullscreenEventPending)
                {
                    if (!image.Fullscreen)
                    {
                        Trace.TraceInformation("Discarding image {0} (region) as a fullscreen update is pending, remote session {1}", image.Idx, RemoteSession.Id);
                        return;
                    }
                    else
                    {
                        Trace.TraceInformation("Fullscreen update received, resuming image(s) processing, remote session {0}", RemoteSession.Id);
                        FullscreenEventPending = false;
                    }
                }

                // if using websocket(s), send the image
                if (WebSockets.Count > 0)
                {
                    Trace.TraceInformation("Sending image {0} ({1}) on websocket(s), remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);

                    foreach (var webSocket in WebSockets)
                    {
                        if (!webSocket.BinaryMode)
                        {
                            webSocket.Send(
                                image.Idx + "," +
                                image.PosX + "," +
                                image.PosY + "," +
                                image.Width + "," +
                                image.Height + "," +
                                image.Format.ToString().ToLower() + "," +
                                image.Quality + "," +
                                image.Fullscreen.ToString().ToLower() + "," +
                                Convert.ToBase64String(image.Data));
                        }
                        else
                        {
                            webSocket.Send(data);
                        }
                    }
                }
                // otherwise, it will be retrieved later
                else
                {
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
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process update, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        // retrieve a cached image
        public RemoteSessionImage GetCachedUpdate(int imageIdx)
        {
            RemoteSessionImage image = null;

            try
            {
                var imageObj = _imageCache["remoteSessionImage_" + RemoteSession.Id + "_" + imageIdx];
                if (imageObj != null)
                {
                    image = (RemoteSessionImage)imageObj;
                    Trace.TraceInformation("Retrieved image {0} ({1}) from cache, remote session {2}", imageIdx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve image {0} from cache, remote session {1} ({2})", imageIdx, RemoteSession.Id, exc);
            }

            return image;
        }

        // retrieve the next image
        public RemoteSessionImage GetNextUpdate(int lastReceivedImageIdx, int waitDuration = 0)
        {
            RemoteSessionImage image = null;

            lock (_imageEventLock)
            {
                try
                {
                    // retrieve the next available image from cache, up to the latest received
                    if (lastReceivedImageIdx < _lastReceivedImageIdx)
                    {
                        for (var idx = lastReceivedImageIdx + 1; idx <= _lastReceivedImageIdx; idx++)
                        {
                            image = GetCachedUpdate(idx);
                            if (image != null)
                            {
                                break;
                            }
                        }
                    }

                    // if no image is available and a wait duration is specified, wait for a new image
                    if (image == null && waitDuration > 0)
                    {
                        Trace.TraceInformation("Waiting for new image, remote session {0}", RemoteSession.Id);
                        _imageEventPending = true;
                        if (Monitor.Wait(_imageEventLock, waitDuration))
                        {
                            image = GetCachedUpdate(_lastReceivedImageIdx);
                        }
                        _imageEventPending = false;
                        Monitor.Pulse(_imageEventLock);
                    }
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to retrieve next update from index {0}, remote session {1} ({2})", lastReceivedImageIdx, RemoteSession.Id, exc);
                }
            }

            return image;
        }

        #endregion

        #region Messages

        private object _messageEventLock;

        private bool _reloadPage;
        public bool ReloadPage
        {
            get
            {
                return _reloadPage;
            }
            set
            {
                lock (_messageEventLock)
                {
                    _reloadPage = value;
                }
            }
        }

        public string ClipboardText { get; private set; }
        private bool _clipboardAvailable;
        public bool ClipboardAvailable
        {
            get
            {
                return _clipboardAvailable;
            }
            set
            {
                lock (_messageEventLock)
                {
                    _clipboardAvailable = value;
                }
            }
        }

        #endregion

        #region Images

        // pending fullscreen update
        public bool FullscreenEventPending { get; private set; }

        // image reception (fullscreen and region)
        private object _imageEventLock;
        private bool _imageEventPending;

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

        #region Cache

        // when using polling (long-polling or xhr only), images must be cached for a delayed retrieval; not applicable for websocket (push)
        private Cache _imageCache;

        // cache lifetime (ms); that is, represents the maximal lag possible for a client, before having to drop some images in order to catch up with the remote session display (proceed with caution with this value!)
        private const int _imageCacheDuration = 1000;

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
            if (WebSockets.Count > 0)
            {
                foreach (var webSocket in WebSockets)
                {
                    webSocket.Close();
                }
            }
        }

        #endregion
    }
}