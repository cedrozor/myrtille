/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2016 Cedric Coste

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
using System.Diagnostics;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Caching;
using Myrtille.Fleck;
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
                WebSocket = null;

                // events
                ImageEventLock = new object();

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
                var message = Encoding.UTF8.GetString(msg);

                if (RemoteSession.State != RemoteSessionState.Connected)
                {
                    // simple handshaking
                    if (message.Equals("Hello server"))
                    {
                        PipeHelper.WritePipeMessage(
                            Pipes.InputsPipe,
                            "remotesession_" + RemoteSession.Id + "_inputs",
                            "Hello client");

                        // remote session is now connected
                        RemoteSession.State = RemoteSessionState.Connected;

                        SendCommand(RemoteSessionCommand.SetImageEncoding, ((int)_imageEncoding).ToString());
                        SendCommand(RemoteSessionCommand.SetImageQuality, _imageQuality.ToString());
                    }
                }
                // remote clipboard
                else if (message.StartsWith("clipboard|"))
                {
                    // if using a websocket, send the clipboard directly
                    if (WebSocket != null)
                    {
                        if (WebSocket.IsAvailable)
                        {
                            Trace.TraceInformation("Sending clipboard content {0} on websocket, remote session {1}", message, RemoteSession.Id);
                            WebSocket.Send(message);
                        }
                        else
                        {
                            Trace.TraceInformation("Websocket is unavailable (connection closed by client?), remote session {0}, status: {1}", RemoteSession.Id, RemoteSession.State);
                        }
                    }
                    // otherwise store it (will be retrieved later)
                    else
                    {
                        ClipboardText = message.Remove(0, 10);
                        ClipboardRequested = true;
                    }
                }
                // new image
                else
                {
                    ProcessUpdate(message);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process updates pipe message, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        #endregion

        #region Sockets

        public IWebSocketConnection WebSocket { get; set; }

        #endregion

        #region Commands

        public void SendCommand(RemoteSessionCommand command, string args = "")
        {
            if (RemoteSession.State != RemoteSessionState.Connected && RemoteSession.State != RemoteSessionState.Disconnecting)
                return;

            try
            {
                Trace.TraceInformation("Sending rdp command {0}, remote session {1}", "C" + (int)command + "-" + args, RemoteSession.Id);
                
                if (command == RemoteSessionCommand.SendFullscreenUpdate)
                {
                    Trace.TraceInformation("Fullscreen update requested, all image(s) will now be discarded while waiting for it, remote session {0}", RemoteSession.Id);
                    _fullscreenPending = true;
                }

                PipeHelper.WritePipeMessage(
                    Pipes.InputsPipe,
                    "remotesession_" + RemoteSession.Id + "_inputs",
                    "C" + (int)command + "-" + args + ",");
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send command {0}, remote session {1} ({2})", command, RemoteSession.Id, exc);
            }
        }

        #endregion

        #region Inputs

        public void SendUserEvent(string data)
        {
            if (RemoteSession.State != RemoteSessionState.Connected)
                return;

            try
            {
                var rdpData = string.Empty;

                var entries = data.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry))
                    {
                        // keyboard scancode (non character key)
                        if (entry.Substring(0, 1).Equals("K"))
                        {
                            var keyEntry = entry.Remove(0, 1);
                            var keyCodeAndState = keyEntry.Split(new[] { "-" }, StringSplitOptions.None);
                            
                            var jsKeyCode = int.Parse(keyCodeAndState[0]);
                            var keyState = keyCodeAndState[1];

                            var rdpScanCode = JsKeyCodeToRdpScanCodeMapping.MapTable[jsKeyCode];
                            if (rdpScanCode != null && (int)rdpScanCode != 0)
                            {
                                rdpData += (string.IsNullOrEmpty(rdpData) ? "K" : ",K") + (int)rdpScanCode + "-" + keyState;
                            }
                        }

                        // keyboard unicode (character key)
                        else if (entry.Substring(0, 1).Equals("U"))
                        {
                            // same format as scancode (key code and state), if parsing is needed
                            rdpData += (string.IsNullOrEmpty(rdpData) ? entry : "," + entry);
                        }

                        // mouse
                        else if (entry.Substring(0, 1).Equals("M"))
                        {
                            rdpData += (string.IsNullOrEmpty(rdpData) ? entry : "," + entry);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(rdpData) && rdpData.Length <= Pipes.InputsPipeBufferSize)
                {
                    Trace.TraceInformation("Forwarding user input(s) {0}, remote session {1}", rdpData, RemoteSession.Id);

                    PipeHelper.WritePipeMessage(
                        Pipes.InputsPipe,
                        "remotesession_" + RemoteSession.Id + "_inputs",
                        rdpData + ",");
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send user input {0}, remote session {1} ({2})", data, RemoteSession.Id, exc);
            }
        }

        #endregion

        #region Updates

        // new image
        public void ProcessUpdate(string data)
        {
            try
            {
                var imgParts = data.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (imgParts.Length != 9)
                    throw new Exception("image can't be deserialized");

                var image = new RemoteSessionImage
                {
                    Idx = int.Parse(imgParts[0]),
                    PosX = int.Parse(imgParts[1]),
                    PosY = int.Parse(imgParts[2]),
                    Width = int.Parse(imgParts[3]),
                    Height = int.Parse(imgParts[4]),
                    Format = (ImageFormat)Enum.Parse(typeof(ImageFormat), imgParts[5]),
                    Quality = int.Parse(imgParts[6]),
                    Base64Data = imgParts[7].Replace("\n", ""),
                    Fullscreen = imgParts[8] == "1"
                };

                Trace.TraceInformation("Received image {0} ({1}), remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);

                // while a fullscreen update is pending, discard all updates; the fullscreen will replace all of them
                if (_fullscreenPending)
                {
                    if (!image.Fullscreen)
                    {
                        Trace.TraceInformation("Discarding image {0} (region) as a fullscreen update is pending, remote session {1}", image.Idx, RemoteSession.Id);
                        return;
                    }
                    else
                    {
                        Trace.TraceInformation("Fullscreen update received, resuming image(s) processing, remote session {0}", RemoteSession.Id);
                        _fullscreenPending = false;
                    }
                }

                // if using a websocket, send the image
                if (WebSocket != null)
                {
                    if (WebSocket.IsAvailable)
                    {
                        Trace.TraceInformation("Sending image {0} ({1}) on websocket, remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), RemoteSession.Id);

                        WebSocket.Send(
                            image.Idx + "," +
                            image.PosX + "," +
                            image.PosY + "," +
                            image.Width + "," +
                            image.Height + "," +
                            image.Format.ToString().ToLower() + "," +
                            image.Quality + "," +
                            image.Base64Data + "," +
                            image.Fullscreen.ToString().ToLower());
                    }
                    else
                    {
                        Trace.TraceInformation("Websocket is unavailable (connection closed by client?), remote session {0}, status: {1}", RemoteSession.Id, RemoteSession.State);
                    }
                }
                // otherwise cache it (will be retrieved later)
                else
                {
                    lock (ImageEventLock)
                    {
                        _imageCache.Insert(
                            "remoteSessionImage_" + RemoteSession.Id + "_" + image.Idx,
                            image,
                            null,
                            DateTime.Now.AddMilliseconds(_imageCacheDuration),
                            Cache.NoSlidingExpiration);

                        // last received image index
                        _lastReceivedImageIdx = image.Idx;

                        // if waiting for a new image, signal the reception
                        if (ImageEventPending)
                        {
                            ImageEventPending = false;
                            Monitor.Pulse(ImageEventLock);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process update {0}, remote session {1} ({2})", data, RemoteSession.Id, exc);
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

            lock (ImageEventLock)
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
                        ImageEventPending = true;
                        if (Monitor.Wait(ImageEventLock, waitDuration))
                        {
                            image = GetCachedUpdate(_lastReceivedImageIdx);
                        }
                        ImageEventPending = false;
                        Monitor.Pulse(ImageEventLock);
                    }
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to retrieve next update from index {0}, remote session {1} ({2})", lastReceivedImageIdx, RemoteSession.Id, exc);
                }
            }

            return image;
        }

        private ImageEncoding _imageEncoding = ImageEncoding.JPEG;
        public ImageEncoding ImageEncoding
        {
            get { return _imageEncoding; }
            set
            {
                _imageEncoding = value;
            }
        }

        private int _imageQuality = (int)Web.ImageQuality.High;
        public int ImageQuality
        {
            get { return _imageQuality; }
            set
            {
                _imageQuality = value;
            }
        }

        #endregion

        #region Clipboard

        public string ClipboardText { get; private set; }
        public bool ClipboardRequested { get; set; }

        #endregion

        #region Events

        // pending fullscreen update
        private bool _fullscreenPending = false;

        // image reception (fullscreen and region)
        public object ImageEventLock { get; private set; }
        public bool ImageEventPending { get; set; }

        // last received image
        private int _lastReceivedImageIdx = 0;

        #endregion

        #region Cache

        // when using polling (long-polling or xhr only), images must be cached for a delayed retrieval; not applicable for websocket (push)
        private Cache _imageCache;

        // cache lifetime; that is, represents the maximal lag possible for a client, before having to drop some images in order to catch up with the remote session display (proceed with caution with this value!)
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
            if (WebSocket != null)
            {
                WebSocket.Close();
                WebSocket = null;
            }
        }

        #endregion
    }
}