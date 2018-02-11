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
using System.IO.Pipes;
using System.Security.AccessControl;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    public class RemoteSessionPipes
    {
        public RemoteSession RemoteSession { get; private set; }

        // it's possible to have 2 ways pipes (duplex, using overlapped I/O), but it proven difficult to setup and raised concurrency access issues...
        // to keep things simple, using separate pipes...
        private NamedPipeServerStream _inputsPipe;
        public NamedPipeServerStream InputsPipe { get { return _inputsPipe; } }

        private NamedPipeServerStream _updatesPipe;
        public NamedPipeServerStream UpdatesPipe { get { return _updatesPipe; } }

        public delegate void ProcessUpdatesPipeMessageDelegate(byte[] msg);
        public ProcessUpdatesPipeMessageDelegate ProcessUpdatesPipeMessage { get; set; }

        public RemoteSessionPipes(RemoteSession remoteSession)
        {
            RemoteSession = remoteSession;
        }

        public void CreatePipes()
        {
            try
            {
                // close the pipes if already exist; they will be re-created below
                DeletePipes();

                // set the pipes access rights
                var pipeSecurity = new PipeSecurity();
                var pipeAccessRule = new PipeAccessRule(RemoteSession.Manager.Client.GetProcessIdentity(), PipeAccessRights.FullControl, AccessControlType.Allow);
                pipeSecurity.AddAccessRule(pipeAccessRule);

                // create the pipes
                _inputsPipe = new NamedPipeServerStream(
                    "remotesession_" + RemoteSession.Id + "_inputs",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    pipeSecurity);

                _updatesPipe = new NamedPipeServerStream(
                    "remotesession_" + RemoteSession.Id + "_updates",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    pipeSecurity);

                // wait for client connection
                InputsPipe.BeginWaitForConnection(InputsPipeConnected, InputsPipe);
                UpdatesPipe.BeginWaitForConnection(UpdatesPipeConnected, UpdatesPipe);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create pipes, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        public void DeletePipes()
        {
            DisposePipe("remoteSession_" + RemoteSession.Id + "_inputs", ref _inputsPipe);
            DisposePipe("remoteSession_" + RemoteSession.Id + "_updates", ref _updatesPipe);
        }

        private void InputsPipeConnected(IAsyncResult e)
        {
            try
            {
                if (InputsPipe != null)
                {
                    InputsPipe.EndWaitForConnection(e);

                    // send connection settings
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.SendServerAddress, string.IsNullOrEmpty(RemoteSession.ServerAddress) ? "localhost" : RemoteSession.ServerAddress);
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.SendUserDomain, RemoteSession.UserDomain);
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.SendUserName, RemoteSession.UserName);
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.SendUserPassword, RemoteSession.UserPassword);
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.SendStartProgram, RemoteSession.Program);

                    // send client settings, if defined (they will be otherwise send later by the client)
                    if (RemoteSession.ImageEncoding.HasValue)
                        RemoteSession.Manager.SendCommand(RemoteSessionCommand.SetImageEncoding, ((int)RemoteSession.ImageEncoding).ToString());

                    if (RemoteSession.ImageQuality.HasValue)
                        RemoteSession.Manager.SendCommand(RemoteSessionCommand.SetImageQuality, RemoteSession.ImageQuality.ToString());

                    if (RemoteSession.ImageQuantity.HasValue)
                        RemoteSession.Manager.SendCommand(RemoteSessionCommand.SetImageQuantity, RemoteSession.ImageQuantity.ToString());

                    // connect; a fullscreen update will be sent upon connection
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.ConnectRdpClient);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to wait for connection on inputs pipe, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        private void UpdatesPipeConnected(IAsyncResult e)
        {
            try
            {
                if (UpdatesPipe != null)
                {
                    UpdatesPipe.EndWaitForConnection(e);
                    ReadUpdatesPipe();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to wait for connection on updates pipe, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        private void ReadUpdatesPipe()
        {
            try
            {
                while (UpdatesPipe != null && UpdatesPipe.IsConnected)
                {
                    var msg = PipeHelper.ReadPipeMessage(UpdatesPipe, "remotesession_" + RemoteSession.Id + "_updates");
                    if (msg != null && msg.Length > 0)
                    {
                        ProcessUpdatesPipeMessage(msg);
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read updates pipe, remote session {0} ({1})", RemoteSession.Id, exc);

                // there is a problem with the updates pipe, close the remote session in order to avoid it being stuck
                RemoteSession.Manager.SendCommand(RemoteSessionCommand.CloseRdpClient);
            }
        }

        private void DisposePipe(string pipeName, ref NamedPipeServerStream pipe)
        {
            if (pipe != null)
            {
                try
                {
                    // CAUTION! closing a pipe in use can make .NET to crash! disconnect it first...
                    if (pipe.IsConnected)
                    {
                        pipe.WaitForPipeDrain();
                        pipe.Disconnect();
                    }

                    pipe.Close();
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to close pipe {0}, remote session {1} ({2})", pipeName, RemoteSession.Id, exc);
                }
                finally
                {
                    pipe.Dispose();
                    pipe = null;
                }
            }
        }
    }
}