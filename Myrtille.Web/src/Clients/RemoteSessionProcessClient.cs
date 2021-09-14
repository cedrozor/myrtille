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
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Web;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;
using Myrtille.Web.Properties;

namespace Myrtille.Web
{
    public class RemoteSessionProcessClient : DuplexClientBase<IRemoteSessionProcess>, IRemoteSessionProcess
    {
        private RemoteSessionManager _remoteSessionManager;
        private object _processStartLock;
        private bool _processStarted = false;

        public RemoteSessionProcessClient(RemoteSessionManager remoteSessionManager, InstanceContext callbackContext)
            : base(callbackContext)
        {
            _remoteSessionManager = remoteSessionManager;
            _processStartLock = new object();
        }

        public bool ProcessStarted
        {
            get
            {
                lock (_processStartLock)
                {
                    return _processStarted;
                }
            }
            set
            {
                _processStarted = value;
            }
        }

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
            string loadBalancerInfo)
        {
            Trace.TraceInformation("Calling service start process, remote session {0}, server {1}, domain {2}, user {3}, program {4}", remoteSessionId, serverAddress, string.IsNullOrEmpty(userDomain) ? "(none)" : userDomain, userName, string.IsNullOrEmpty(startProgram) ? "(none)" : startProgram);

            lock (_processStartLock)
            {
                try
                {
                    Channel.StartProcess(
                        remoteSessionId,
                        hostType,
                        securityProtocol,
                        serverAddress,
                        vmGuid,
                        userDomain,
                        userName,
                        startProgram,
                        clientWidth,
                        clientHeight,
                        allowRemoteClipboard,
                        allowPrintDownload,
                        allowAudioPlayback,
                        loadBalancerInfo);

                    _processStarted = true;
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to call service start process, remote session {0} ({1})", _remoteSessionManager.RemoteSession?.Id, exc);
                    throw;
                }
            }
        }

        public void StopProcess()
        {
            try
            {
                Trace.TraceInformation("Calling service stop process, remote session {0}", _remoteSessionManager.RemoteSession.Id);
                Channel.StopProcess();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to call service stop process, remote session {0} ({1})", _remoteSessionManager.RemoteSession?.Id, exc);
                throw;
            }
        }

        public string GetProcessIdentity()
        {
            try
            {
                Trace.TraceInformation("Retrieving service process identity, remote session {0}", _remoteSessionManager.RemoteSession.Id);
                return Channel.GetProcessIdentity();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve service process identity, remote session {0} ({1})", _remoteSessionManager.RemoteSession?.Id, exc);
                throw;
            }
        }
    }

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class RemoteSessionProcessClientCallback : IRemoteSessionProcessCallback
    {
        private ConnectionClient _connectionClient = new ConnectionClient(Settings.Default.ConnectionServiceUrl);
        private EnterpriseClient _enterpriseClient = new EnterpriseClient();
        private ApplicationPoolClient _applicationPoolClient = new ApplicationPoolClient();

        private RemoteSessionManager _remoteSessionManager;
        private HttpApplicationState _application;

        public RemoteSessionProcessClientCallback(RemoteSessionManager remoteSessionManager, HttpApplicationState application)
        {
            _remoteSessionManager = remoteSessionManager;
            _application = application;
        }

        public void ProcessExited(int exitCode)
        {
            try
            {
                Trace.TraceInformation("Received host client process exit notification, remote session {0}", _remoteSessionManager.RemoteSession.Id);

                // remote session is now disconnected
                _remoteSessionManager.RemoteSession.State = RemoteSessionState.Disconnected;

                // process exit code
                _remoteSessionManager.RemoteSession.ExitCode = exitCode;

                // stop monitoring the remote session owner activity, if enabled
                if (_remoteSessionManager.OwnerIdleTimeout != null)
                {
                    _remoteSessionManager.OwnerIdleTimeout.Cancel();
                    _remoteSessionManager.OwnerIdleTimeout.Dispose();
                }

                // release the communication pipes, if any
                if (_remoteSessionManager.Pipes != null)
                {
                    _remoteSessionManager.Pipes.DeletePipes();
                }

                // if the remote session is marked for reconnection, reconnect it
                if (_remoteSessionManager.RemoteSession.Reconnect)
                {
                    _remoteSessionManager.RemoteSession.Reconnect = false;
                    _remoteSessionManager.RemoteSession.BrowserResize = null;
                    _remoteSessionManager.HostClient.ProcessStarted = false;
                    if (_remoteSessionManager.OwnerIdleTimeout != null)
                    {
                        _remoteSessionManager.OwnerIdleTimeout = new CancellationTokenSource();
                    }
                    _remoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;
                    _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected });
                }
                // otherwise, redirect to the login page (or the hosts dashboard in enterprise mode)
                else
                {
                    // CAUTION! exit code list is not exhaustive (that's why RemoteSession.ExitCode is an int)
                    RemoteSessionExitCode _exitCode;
                    if (!Enum.TryParse(_remoteSessionManager.RemoteSession.ExitCode.ToString(), out _exitCode))
                    {
                        _exitCode = RemoteSessionExitCode.Unknown;
                    }

                    // if using a connection service, send the connection state and exit code
                    if (_remoteSessionManager.RemoteSession.ConnectionService)
                    {
                        _connectionClient.SetConnectionState(
                            _remoteSessionManager.RemoteSession.Id,
                            string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.VMAddress) ? _remoteSessionManager.RemoteSession.ServerAddress : _remoteSessionManager.RemoteSession.VMAddress,
                            GuidHelper.ConvertFromString(_remoteSessionManager.RemoteSession.VMGuid),
                            _remoteSessionManager.RemoteSession.State);

                        _connectionClient.SetConnectionExitCode(
                            _remoteSessionManager.RemoteSession.Id,
                            string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.VMAddress) ? _remoteSessionManager.RemoteSession.ServerAddress : _remoteSessionManager.RemoteSession.VMAddress,
                            GuidHelper.ConvertFromString(_remoteSessionManager.RemoteSession.VMGuid),
                            _exitCode);
                    }

                    CleanupDisconnectedSession(_exitCode);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to handle session disconnect, remote session {0} ({1})", _remoteSessionManager.RemoteSession?.Id, exc);
                throw;
            }
        }

        private void CleanupDisconnectedSession(RemoteSessionExitCode exitCode)
        {
            try
            {
                _application.Lock();

                #region session

                // unregister the remote session at the application level
                var remoteSessions = (IDictionary<Guid, RemoteSession>)_application[HttpApplicationStateVariables.RemoteSessions.ToString()];
                if (remoteSessions.ContainsKey(_remoteSessionManager.RemoteSession.Id))
                {
                    remoteSessions.Remove(_remoteSessionManager.RemoteSession.Id);
                }

                #endregion

                #region session sharing

                // remove the remote session guest(s)
                var guests = new List<SharingInfo>();
                var sharedSessions = (IDictionary<Guid, SharingInfo>)_application[HttpApplicationStateVariables.SharedRemoteSessions.ToString()];
                foreach (var sharingInfo in sharedSessions.Values)
                {
                    if (sharingInfo.RemoteSession.Id.Equals(_remoteSessionManager.RemoteSession.Id))
                    {
                        guests.Add(sharingInfo);
                    }
                }

                foreach (var guest in guests)
                {
                    sharedSessions.Remove(guest.GuestInfo.Id);
                }

                #endregion

                #region application pool recycling

                /*
                application pool recycling may seem a bit extreme, but the garbage collector doesn't return the freed memory to the operating system
                instead, it makes it available to the memory workspace of the application pool process, which in turn uses it for faster memory allocation later
                while this is fine for most usage, this becomes critical when the OS is under memory pressure
                if that occurs, the process is meant to return its unused memory to the OS
                in reality, this is not always true; so the system becomes slow (hdd swap) and unstable

                memory usage of a process under Windows: https://dzone.com/articles/windows-process-memory-usage-demystified
                tool: https://technet.microsoft.com/en-us/sysinternals/vmmap.aspx
                garbage collector: https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
                reallocation of freed memory: https://stackoverflow.com/questions/28614210/why-doesnt-net-release-unused-memory-back-to-os-when-physical-95
                */
                bool idleAppPoolRecycling;
                if (!bool.TryParse(ConfigurationManager.AppSettings["IdleAppPoolRecycling"], out idleAppPoolRecycling))
                {
                    idleAppPoolRecycling = false;
                }

                // connect from a login page or url
                bool loginEnabled;
                if (!bool.TryParse(ConfigurationManager.AppSettings["LoginEnabled"], out loginEnabled))
                {
                    loginEnabled = true;
                }

                // if enabled, url of the login page
                var loginUrl = string.Empty;
                if (loginEnabled)
                {
                    loginUrl = ConfigurationManager.AppSettings["LoginUrl"];
                }

                // recycle only if enabled and when there is no active remote session
                // don't recycle if using the enterprise mode (if there are enterprise sessions, they musn't be dropped!)
                // don't recycle in case of connection failure, so that the page can handle it (show the related error dialog)
                if (idleAppPoolRecycling &&
                    remoteSessions.Count == 0 &&
                    (_enterpriseClient.GetMode() == EnterpriseMode.None)  &&
                    (exitCode == RemoteSessionExitCode.Success || exitCode == RemoteSessionExitCode.SessionDisconnectFromMenu || exitCode == RemoteSessionExitCode.SessionLogoutFromMenu))
                {
                    // if using a custom login page, the application pool must be recycled after the redirect
                    if (!string.IsNullOrEmpty(loginUrl))
                    {
                        // redirect to the custom login page
                        _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected });

                        // give some time for the redirection
                        Thread.Sleep(2000);

                        // the gateway doesn't have enough rights to recycle the application pool, this is delegated to the myrtille services
                        _applicationPoolClient.RecycleApplicationPool(Environment.UserName);
                    }
                    // otherwise, the application pool must be recycled before the redirect
                    // the browser will acquire a new http session
                    else
                    {
                        // the gateway doesn't have enough rights to recycle the application pool, this is delegated to the myrtille services
                        _applicationPoolClient.RecycleApplicationPool(Environment.UserName);

                        // give some time for the recycling
                        Thread.Sleep(2000);

                        // redirect to the default login page (empty if login is not enabled)
                        _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected });
                    }
                }
                else
                {
                    // redirect to the login page (or the hosts dashboard in enterprise mode)
                    _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected });
                }

                #endregion
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to cleanup disconnected session, remote session {0} ({1})", _remoteSessionManager.RemoteSession?.Id, exc);
                throw;
            }
            finally
            {
                _application.UnLock();
            }
        }
    }
}