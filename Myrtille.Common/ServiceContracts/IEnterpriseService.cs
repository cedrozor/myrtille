using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Myrtille.Common.Models;

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
    }
}
