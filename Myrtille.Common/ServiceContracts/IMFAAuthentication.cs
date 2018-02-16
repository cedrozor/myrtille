using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract]
    public interface IMFAAuthentication
    {
        [OperationContract]
        bool GetState();

        [OperationContract]
        string GetPromptLabel();

        [OperationContract]
        bool Authenticate(string username, string password, string clientIP = null);

        [OperationContract]
        string GetProviderURL();
    }
}
