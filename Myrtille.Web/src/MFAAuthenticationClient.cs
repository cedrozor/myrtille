using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class MFAAuthenticationClient : ClientBase<IMFAAuthentication>, IMFAAuthentication
    {
        public string GetProviderURL()
        {
            try
            {
                return Channel.GetProviderURL();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get provideURL", exc);
                throw;
            }
        }
        public bool Authenticate(string username, string password)
        {
            try
            {
                return Channel.Authenticate(username, password);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to authentication from user {0}", username, exc);
                throw;
            }
            
        }

        public string GetPromptLabel()
        {
            try
            {
                return Channel.GetPromptLabel();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get mfa prompt label", exc);
                throw;
            }
        }

        public bool GetState()
        {
            try
            {
                return Channel.GetState();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to authentication adapter state", exc);
                throw;
            }
        }
    }
}