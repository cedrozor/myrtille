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

namespace Myrtille.Services.Contracts
{
    public class ConnectionInfo
    {
        public UserInfo User { get; set; }
        public HostInfo Host { get; set; }
        public VMInfo VM { get; set; }
        public bool AllowRemoteClipboard { get; set; }
        public bool AllowFileTransfer { get; set; }
        public bool AllowPrintDownload { get; set; }
        public bool AllowAudioPlayback { get; set; }
        /// <summary>
        /// 0 to disable session sharing
        /// </summary>
        public int MaxActiveGuests { get; set; }
        /// <summary>
        /// executable path, name and parameters (double quotes must be escaped) (optional)
        /// </summary>
        public string StartProgram { get; set; }
        public string GatewayUrl { get; set; }
        public string LoadBalanceInfo { get; set; }
    }
}