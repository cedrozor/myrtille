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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public enum WebSocketDirection
    {
        Duplex = 0,
        Up = 1,
        UpWithAck = 2,
        Down = 3
    }

    public class RemoteSessionSocketHandler : WebSocketHandler
    {
        private HttpSessionState _session;
        private RemoteSession _remoteSession;
        private RemoteSessionClient _client;

        public bool Binary { get; private set; }
        public WebSocketDirection Direction { get; private set; }

        public RemoteSessionSocketHandler(HttpContext context, bool binary, string clientId, WebSocketDirection direction)
            : base()
        {
            _session = context.Session;
            Binary = binary;
            Direction = direction;

            try
            {
                if (context.Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the given http session
                _remoteSession = (RemoteSession)context.Session[HttpSessionStateVariables.RemoteSession.ToString()];

                if (!_remoteSession.Manager.Clients.ContainsKey(clientId))
                {
                    lock (_remoteSession.Manager.ClientsLock)
                    {
                        _remoteSession.Manager.Clients.Add(clientId, new RemoteSessionClient(clientId));
                    }
                }

                _client = _remoteSession.Manager.Clients[clientId];
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to initialize socket handler ({0})", exc);
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Direction == WebSocketDirection.Duplex || Direction == WebSocketDirection.Down)
            {
                try
                {
                    lock (_client.Lock)
                    {
                        if (_client.WebSockets == null)
                        {
                            _client.WebSockets = new List<RemoteSessionSocketHandler>();
                        }

                        _client.WebSockets.Add(this);

                        // unregister the message queue for the client (now using HTML5)
                        if (_client.MessageQueue != null)
                        {
                            _client.MessageQueue = null;
                        }
                    }

                    Trace.TraceInformation("Registered websocket handler for client {0}, remote session {1}", _client.Id, _remoteSession.Id);
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to register websocket handler for client {0}, remote session {1} ({2})", _client?.Id, _remoteSession?.Id, exc);
                }
            }

            if (Direction == WebSocketDirection.Down)
            {
                return;
            }

            // update guest information
            if (!_session.SessionID.Equals(_remoteSession.OwnerSessionID))
            {
                if (_session[HttpSessionStateVariables.GuestInfo.ToString()] != null)
                {
                    ((GuestInfo)_session[HttpSessionStateVariables.GuestInfo.ToString()]).Websocket = true;
                }
            }
            // connect the remote server
            else if (_remoteSession.State == RemoteSessionState.Connecting && !_remoteSession.Manager.HostClient.ProcessStarted)
            {
                try
                {
                    // create pipes for the web gateway and the host client to talk
                    _remoteSession.Manager.Pipes.CreatePipes();

                    // the host client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the remote session had ended), pipes are released
                    // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the host client through the inputs pipe
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    _remoteSession.Manager.HostClient.StartProcess(
                        _remoteSession.Id,
                        _remoteSession.HostType,
                        _remoteSession.SecurityProtocol,
                        _remoteSession.ServerAddress,
                        _remoteSession.VMGuid,
                        _remoteSession.UserDomain,
                        _remoteSession.UserName,
                        _remoteSession.StartProgram,
                        _remoteSession.ClientWidth,
                        _remoteSession.ClientHeight,
                        _remoteSession.AllowRemoteClipboard,
                        _remoteSession.AllowPrintDownload,
                        _remoteSession.AllowAudioPlayback,
                        _remoteSession.LoadBalanceInfo);
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to connect the remote session {0} ({1})", _remoteSession?.Id, exc);
                }
            }
            
            // send a disconnect notification
            if (_remoteSession.State == RemoteSessionState.Disconnected)
            {
                Trace.TraceInformation("Sending disconnected state on websocket, remote session {0}", _remoteSession?.Id);
                Send("disconnected");
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            if (Direction == WebSocketDirection.Duplex || Direction == WebSocketDirection.Down)
            {
                try
                {
                    lock (_client.Lock)
                    {
                        _client.WebSockets.Remove(this);
                    }

                    Trace.TraceInformation("Unregistered websocket handler for client {0}, remote session {1}", _client.Id, _remoteSession.Id);
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to unregister websocket handler for client {0}, remote session {1} ({2})", _client?.Id, _remoteSession?.Id, exc);
                }
            }
        }

        public override void OnError()
        {
            base.OnError();
            Trace.TraceError("Websocket error, client {0}, remote session {1} ({2})", _client?.Id, _remoteSession?.Id, Error);
        }

        public override void OnMessage(string message)
        {
            ProcessMessage(message);
        }

        public override void OnMessage(byte[] message)
        {
            ProcessMessage(Encoding.UTF8.GetString(message));
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var msgParams = message.Split(new[] { "&" }, StringSplitOptions.None);

                var clientId = msgParams[0];
                var data = Uri.UnescapeDataString(msgParams[1]);
                var imgIdx = int.Parse(msgParams[2]);
                var latency = int.Parse(msgParams[3]);
                var timestamp = long.Parse(msgParams[4]);

                // process input(s)
                if (!string.IsNullOrEmpty(data))
                {
                    _remoteSession.Manager.ProcessInputs(_session, clientId, data);
                }

                _client.ImgIdx = imgIdx;
                _client.Latency = latency;

                // acknowledge the message processing with the given timestamp; it will be used by the client to compute the roundtrip time
                var websocket = Direction != WebSocketDirection.Up ? this : _client.WebSockets[0];
                websocket.SendMessage(new RemoteSessionMessage { Type = MessageType.Ack, Text = timestamp.ToString() });
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process websocket message, remote session {0} ({1})", _remoteSession?.Id, exc);
            }
        }

        private string GetImageText(RemoteSessionImage image)
        {
            return
                image.Idx + "," +
                image.PosX + "," +
                image.PosY + "," +
                image.Width + "," +
                image.Height + "," +
                image.Format.ToString().ToLower() + "," +
                image.Quality + "," +
                image.Fullscreen.ToString().ToLower() + "," +
                Convert.ToBase64String(image.Data);
        }

        private byte[] GetImageBytes(RemoteSessionImage image)
        {
            using (var memoryStream = new MemoryStream())
            {
                // tag (4 bytes)
                memoryStream.Write(BitConverter.GetBytes(0), 0, 4);

                // info (32 bytes)
                memoryStream.Write(BitConverter.GetBytes(image.Idx), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.PosX), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.PosY), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.Width), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.Height), 0, 4);
                memoryStream.Write(BitConverter.GetBytes((int)image.Format), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.Quality), 0, 4);
                memoryStream.Write(BitConverter.GetBytes(image.Fullscreen ? 1 : 0), 0, 4);

                // data
                memoryStream.Write(image.Data, 0, image.Data.Length);

                return memoryStream.ToArray();
            }
        }

        public void SendImage(RemoteSessionImage image)
        {
            if (!Binary)
            {
                Send(GetImageText(image));
            }
            else
            {
                Send(GetImageBytes(image));
            }
        }

        public void SendMessage(RemoteSessionMessage message)
        {
            if (!Binary)
            {
                Send(JsonConvert.SerializeObject(message));
            }
            else
            {
                Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            }
        }
    }
}