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
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class RemoteSessionProcessClient : DuplexClientBase<IRemoteSessionProcess>, IRemoteSessionProcess
    {
        private readonly RemoteSessionManager _remoteSessionManager;

        public RemoteSessionProcessClient(RemoteSessionManager remoteSessionManager, InstanceContext callbackContext)
            : base(callbackContext)
        {
            _remoteSessionManager = remoteSessionManager;
        }

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
            Trace.TraceInformation("Calling service start process, remote session {0}, server {1}, domain {2}, user {3}, program {4}", remoteSessionId, serverAddress, string.IsNullOrEmpty(userDomain) ? "(none)" : userDomain, userName, string.IsNullOrEmpty(startProgram) ? "(none)" : startProgram);

            try
            {
                Channel.StartProcess(remoteSessionId, serverAddress, userDomain, userName, startProgram, clientWidth, clientHeight, allowRemoteClipboard, securityProtocol, hostType);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to call service start process, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }

        public void StopProcess()
        {
            Trace.TraceInformation("Calling service stop process, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                Channel.StopProcess();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to call service stop process, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }

        public string GetProcessIdentity()
        {
            Trace.TraceInformation("Retrieving service process identity, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                return Channel.GetProcessIdentity();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve service process identity, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }
    }

    [CallbackBehavior(UseSynchronizationContext = false)]
    public class RemoteSessionProcessClientCallback : IRemoteSessionProcessCallback
    {
        private readonly RemoteSessionManager _remoteSessionManager;

        public RemoteSessionProcessClientCallback(RemoteSessionManager remoteSessionManager)
        {
            _remoteSessionManager = remoteSessionManager;
        }

        public void ProcessExited(int exitCode)
        {
            Trace.TraceInformation("Received rdp client process exit notification, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                // remote session is now disconnected
                _remoteSessionManager.RemoteSession.State = RemoteSessionState.Disconnected;

                // process exit code
                _remoteSessionManager.RemoteSession.ExitCode = exitCode;

                // stop monitoring the remote session owner activity, if enabled
                if (_remoteSessionManager.ClientIdleTimeout != null)
                {
                    _remoteSessionManager.ClientIdleTimeout.Cancel();
                }

                // release the communication pipes, if any
                if (_remoteSessionManager.Pipes != null)
                {
                    _remoteSessionManager.Pipes.DeletePipes();
                }

                // if using websocket(s), send a disconnect notification
                if (_remoteSessionManager.WebSockets.Count > 0)
                {
                    foreach (var webSocket in _remoteSessionManager.WebSockets)
                    {
                        webSocket.Send("disconnected");
                    }
                }
                // no websocket at this step can mean using xhr (with long-polling or not),
                // but it can also be because the browser wasn't fast enough to open a websocket before the rdp client was closed (due to a crash, connection or authentication issue, etc.)
                // a disconnect notification will be sent to the browser by the socket handler if the remote session is disconnected in the context of a newly opened socket
                else
                {
                    // if waiting for a new image, leave
                    _remoteSessionManager.StopWaitForImageEvent();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to cleanup disconnected session, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }
    }
}