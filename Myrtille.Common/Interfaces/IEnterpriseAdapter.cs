/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste/Paul Oliver

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
using Myrtille.Common.Models;

namespace Myrtille.Common.Interfaces
{
    public interface IEnterpriseAdapter
    {

        void Initialise();

        EnterpriseSession Authenticate(string username, string password, string adminGroup, string domain);

        long? AddHost(EnterpriseHostEdit editHost, string sessionID);

        bool UpdateHost(EnterpriseHostEdit editHost, string sessionID);

        bool DeleteHost(long hostID, string sessionID);

        EnterpriseHostEdit GetHost(long hostID, string sessionID);

        void Logout(string sessionID);

        List<EnterpriseHost> SessionHosts(string sessionID);

        EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey);

        string CreateUserSession(string sessionID, long hostID, string username, string password);
    }
}
