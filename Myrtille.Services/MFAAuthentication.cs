using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class MFAAuthentication : IMFAAuthentication
    {
        public string GetProviderURL()
        {
            return Program._multifactorAdapter.ProviderURL;
        }

        public bool GetState()
        {
            return Program._multifactorAdapter != null;
        }

        public string GetPromptLabel()
        {
            return Program._multifactorAdapter.PromptLabel;
        }

        public bool Authenticate(string username, string password)
        {
            return Program._multifactorAdapter.Authenticate(username, password);
        }
    }
}
