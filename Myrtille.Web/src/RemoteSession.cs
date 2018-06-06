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

using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class RemoteSession
    {
        public RemoteSessionManager Manager { get; private set; }

        public int Id;
        public RemoteSessionState State;
        public string ServerName;
        public string ServerAddress;
        public string UserDomain;
        public string UserName;
        public string UserPassword;
        public int ClientWidth;
        public int ClientHeight;
        public bool ScaleDisplay;
        public ImageEncoding? ImageEncoding;            // provided by the client
        public int? ImageQuality;                       // provided by the client
        public int? ImageQuantity;                      // provided by the client
        public bool StatMode;
        public bool DebugMode;
        public bool CompatibilityMode;
        public string StartProgram;
        public bool AllowRemoteClipboard;               // set in myrtille web config
        public SecurityProtocolEnum SecurityProtocol;
        public int ExitCode;
        public bool AllowSessionSharing;                // set in myrtille web config
        public string OwnerSessionID;                   // the http session on which the remote session is bound to
        public HostTypeEnum HostType;                   // set remote host type, RDP or SSH

        public RemoteSession()
        {
            Manager = new RemoteSessionManager(this);
        }
    }
}