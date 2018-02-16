using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.ServiceModel;
using Myrtille.Services.Contracts;
using Myrtille.Common.Models;

namespace Myrtille.Web
{
    public class EnterpriseServiceClient : ClientBase<IEnterpriseService>, IEnterpriseService
    {
        public long? AddHost(EnterpriseHostEdit editHost, string sessionID)
        {
            try
            {
                return Channel.AddHost(editHost,sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to add host {0}, {1}", editHost.HostName, exc);
                throw;
            }
        }

        public bool UpdateHost(EnterpriseHostEdit editHost, string sessionID)
        {
            try
            {
                return Channel.UpdateHost(editHost, sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to update host {0}, {1}", editHost.HostName, exc);
                throw;
            }
        }

        public EnterpriseHostEdit GetHost(long hostID, string sessionID)
        {
            try
            {
                return Channel.GetHost(hostID, sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get host {0}, {1}", hostID, exc);
                throw;
            }
        }
        public bool DeleteHost(long hostID, string sessionID)
        {
            try
            {
                return Channel.DeleteHost(hostID, sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete host {0}, {1}", hostID, exc);
                throw;
            }
        }

        public EnterpriseSession Authenticate(string username, string password)
        {
            try
            {
                return Channel.Authenticate(username,password);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get authenticate user {0}, {1}",username, exc);
                throw;
            }
        }



        public EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey)
        {
            try
            {
                return Channel.GetSessionConnectionDetails(sessionID, hostID, sessionKey);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to gets session connection details for session {0}, {1}", sessionID, exc);
                throw;
            }
        }

        public List<EnterpriseHost> GetSessionHosts(string sessionID)
        {
            try
            {
                return Channel.GetSessionHosts(sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to gets host for session {0}, {1}", sessionID, exc);
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
                Trace.TraceError("Failed to get enterprise state {0}", exc);
                throw;
            }
        }

        public void Logout(string sessionID)
        {
            try
            {
                Channel.Logout(sessionID);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get enterprise state {0}", exc);
                throw;
            }
        }

        public string CreateUserSession(string sessionID, long hostID, string username, string password)
        {
            try
            {
                return Channel.CreateUserSession(sessionID,hostID,username,password);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create user session {0}", exc);
                throw;
            }
        }
    }
}