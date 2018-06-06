/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2018 Olive Innovations

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.IO.Pipes;
using Myrtille.Helpers;

namespace Myrtille.SSH
{
    internal class PipeMessaging
    {
        private string _sessionID;
        private NamedPipeClientStream _inputsPipe;
        private NamedPipeClientStream _updatesPipe;
        private StringBuilder commandData = new StringBuilder();

        public delegate void MessageReceivedEvent(string command, string data);
        public event MessageReceivedEvent OnMessageReceivedEvent;
        public DateTime LastFullUpdate { get; set; }

        public PipeMessaging(string sessionID)
        {
            _sessionID = sessionID;
        }


        public bool CreateCommunicationPipes()
        {
            try
            {
                _inputsPipe = new NamedPipeClientStream(".", "remotesession_" + _sessionID + "_inputs"
                        , PipeDirection.InOut
                        , PipeOptions.Asynchronous
                        , System.Security.Principal.TokenImpersonationLevel.Impersonation);


                _updatesPipe = new NamedPipeClientStream(".","remotesession_" + _sessionID + "_updates"
                        , PipeDirection.InOut
                        , PipeOptions.Asynchronous
                        , System.Security.Principal.TokenImpersonationLevel.Impersonation);
                
                _inputsPipe.Connect(5000);

                _updatesPipe.Connect(5000);
                LastFullUpdate = DateTime.Now;

                return true;

            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create pipes, remote session {0} ({1})", _sessionID, exc);
                return false;
            }
            
        }

        public void ClosePipes()
        {
            ClosePipe("remoteSession_" + _sessionID + "_inputs", ref _inputsPipe);
            ClosePipe("remoteSession_" + _sessionID + "_updates", ref _updatesPipe);
        }

        public void ReadInputPipes()
        {
            try
            {
                while (_inputsPipe != null && _inputsPipe.IsConnected)
                {
                    var hasData = ReadPipeMessage(_inputsPipe, "remotesession_" + _sessionID + "_input");

                    if(hasData)
                    {
                        ProcessPipeMessages();
                    }

                    if(LastFullUpdate < DateTime.Now.AddMinutes(-1))
                    {
                        throw new Exception("No full update request received in timeout period");
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read input pipe, remote session {0} ({1})", _sessionID, exc);

                // there is a problem with the updates pipe, close the remote session in order to avoid it being stuck
                //RemoteSession.Manager.SendCommand(RemoteSessionCommand.CloseRdpClient);
                ClosePipes();
            }
        }

        private bool ReadPipeMessage(PipeStream pipe, string pipeName)
        {
            if (pipe == null)
            {
                Trace.TraceError("Failed to read message from pipe {0} (not set)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return false;
            }

            if (!pipe.IsConnected)
            {
                Trace.TraceError("Failed to read message from pipe {0} (not connected)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return false;
            }

            try
            {
                if (pipe.CanRead)
                {
                    var buffer = new byte[1024]; 

                    var bytesRead = 0;
                    if ((bytesRead = pipe.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        commandData.Append(Encoding.UTF8.GetString(buffer,0,bytesRead));
                        return true;
                    }
                    return false;
                }
                else
                {
                    throw new Exception("not readable");
                }
            }
            catch (IOException)
            {
                Trace.TraceError("Failed to read message from pipe {0} (I/O error)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                throw;
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read message from pipe {0} ({1})", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName), exc);
                throw;
            }
        }

        private void ProcessPipeMessages()
        {
            var commandList = commandData.ToString();
            var endWithTerminator = commandList.EndsWith("\t");
            var commands = commandList.Split("\t".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < commands.Length; i++)
            {
                if (i == (commands.Length - 1) && !endWithTerminator)
                {
                    commandData.Clear();
                    commandData.Append(commands[i]);
                    break;
                }

                var commandType = commands[i].Substring(0, 3);
                var commandValue = commands[i].Substring(3);

                OnMessageReceivedEvent?.Invoke(commandType, commandValue);

                if (i == (commands.Length - 1))
                    commandData.Clear();
            }
        }

        public void SendUpdatePipeMessage(byte[] data)
        {
            try
            {
                
                using (var ms = new MemoryStream())
                {
                    int msgLength = (5 + data.Length);
                    ms.Write(BitConverter.GetBytes(msgLength), 0, 4);
                    ms.Write(Encoding.UTF8.GetBytes("term|"), 0, 5);
                    ms.Write(data, 0, data.Length);
                    _updatesPipe.Write(ms.ToArray(), 0, (int)ms.Length);
                    _updatesPipe.Flush();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to write updates pipe, remote session {0} ({1})", _sessionID, exc);

                // there is a problem with the updates pipe, close the remote session in order to avoid it being stuck
                ClosePipes();
            }
        }

        private void ClosePipe(string pipeName, ref NamedPipeClientStream pipe)
        {
            if (pipe != null)
            {
                try
                {
                    pipe.Close();
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to close pipe {0}, remote session {1} ({2})", pipeName, _sessionID, exc);
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
