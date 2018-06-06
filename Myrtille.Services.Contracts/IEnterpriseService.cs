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
using System.Collections.Generic;
using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract]
    public interface IEnterpriseService
    {
        [OperationContract]
        bool GetState();

        [OperationContract]
        EnterpriseSession Authenticate(string username, string password);

        [OperationContract]
        void Logout(string sessionID);

        [OperationContract]
        long? AddHost(EnterpriseHostEdit editHost, string sessionID);

        [OperationContract]
        EnterpriseHostEdit GetHost(long hostID, string sessionID);

        [OperationContract]
        bool UpdateHost(EnterpriseHostEdit editHost, string sessionID);

        [OperationContract]
        bool DeleteHost(long hostID, string sessionID);

        [OperationContract]
        List<EnterpriseHost> GetSessionHosts(string sessionID);

        [OperationContract]
        EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey);

        [OperationContract]
        string CreateUserSession(string sessionID, long hostID, string username, string password);

        [OperationContract]
        bool ChangeUserPassword(string username, string oldPassword, string newPassword);

        [OperationContract]
        bool AddSessionHostCredentials(EnterpriseHostSessionCredentials credentials);
    }
}