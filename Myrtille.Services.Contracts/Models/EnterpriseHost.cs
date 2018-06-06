/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste
    Copyright(c) 2014-2018 Paul Oliver

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
    public class EnterpriseHost
    {
        public long HostID { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public SecurityProtocolEnum Protocol { get; set; }
        public string HostType { get; set; }
        public string StartRemoteProgram { get; set; }
        public string HostImage
        {
            get
            {
                if(HostType == "SSH")
                {
                    return "./img/RemoteSSH.png";
                }

                return "./img/RemoteDesktop.png";
            }
        }
        public bool PromptForCredentials { get; set; }
    }
}