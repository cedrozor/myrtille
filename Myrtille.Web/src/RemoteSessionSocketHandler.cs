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
using System.Diagnostics;
using System.Text;
using System.Web.SessionState;
using Microsoft.Web.WebSockets;

namespace Myrtille.Web
{
    public class RemoteSessionSocketHandler : WebSocketHandler
    {
        public bool BinaryMode { get; private set; }
        private readonly RemoteSession _remoteSession;

        public RemoteSessionSocketHandler(HttpSessionState session, bool binaryMode)
            : base()
        {
            BinaryMode = binaryMode;

            try
            {
                if (session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the given http session
                _remoteSession = (RemoteSession)session[HttpSessionStateVariables.RemoteSession.ToString()];
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", session.SessionID, exc);
            }
        }

        public override void OnOpen()
        {
            Trace.TraceInformation("Opening websocket, remote session {0}", _remoteSession.Id);
            _remoteSession.Manager.WebSockets.Add(this);
            base.OnOpen();

            // send a disconnect notification
            if (_remoteSession.State == RemoteSessionState.Disconnected)
            {
                Trace.TraceInformation("Sending disconnected state on websocket, remote session {0}", _remoteSession.Id);
                Send("disconnected");
            }
        }

        public override void OnClose()
        {
            Trace.TraceInformation("Closing websocket, remote session {0}", _remoteSession.Id);
            _remoteSession.Manager.WebSockets.Remove(this);
            base.OnClose();
        }

        public override void OnError()
        {
            Trace.TraceError("Websocket error, remote session {0} ({1})", _remoteSession.Id, Error);
            base.OnError();
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
                var msgParams = message.Split(new[] { "|" }, StringSplitOptions.None);

                var data = msgParams[0];
                var imgIdx = int.Parse(msgParams[1]);
                var timestamp = long.Parse(msgParams[2]);

                // process input(s)
                if (!string.IsNullOrEmpty(data))
                {
                    _remoteSession.Manager.ProcessInputs(data);
                }

                // acknowledge the message processing with the given timestamp; it will be used by the client to compute the roundtrip time
                Send("ack," + timestamp);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to process websocket message, remote session {0} ({1})", _remoteSession.Id, exc);
            }
        }

        public new void Send(string message)
        {
            if (!BinaryMode)
            {
                base.Send(message);
            }
            else
            {
                Send(Encoding.UTF8.GetBytes(message));
            }
        }
    }
}