using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using Myrtille.Services.Contracts;
using Myrtille.Common.Models;

namespace Myrtille.Services
{
    public class EnterpriseService : IEnterpriseService
    {
        public long? AddHost(EnterpriseHostEdit editHost, string sessionID)
        {
            try
            {
                Trace.TraceInformation("Add host requested, host {0}", editHost.HostName);
                return Program._enterpriseAdapter.AddHost(editHost, sessionID);
            }catch(Exception ex)
            {
                Trace.TraceError("Failed to add host {0}, ({1})",editHost.HostName, ex);
                return null;
            }
        }

        public bool UpdateHost(EnterpriseHostEdit editHost, string sessionID)
        {
            try
            {
                Trace.TraceInformation("Update host requested, host {0}", editHost.HostName);
                return Program._enterpriseAdapter.UpdateHost(editHost, sessionID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to update host {0}, ({1})", editHost.HostName, ex);
                return false;
            }
        }

        public bool DeleteHost(long hostID, string sessionID)
        {
            try
            {
                Trace.TraceInformation("Deleting host");
                return Program._enterpriseAdapter.DeleteHost(hostID, sessionID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to delete host {0} ({1})", hostID, ex);
                return false;
            }
        }

        public EnterpriseHostEdit GetHost(long hostID, string sessionID)
        {
            try
            {
                Trace.TraceInformation("Edit host requested, host {0}", hostID);
                return Program._enterpriseAdapter.GetHost(hostID, sessionID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to get host {0}, ({1})", hostID, ex);
                return null;
            }
        }
        public EnterpriseSession Authenticate(string username, string password)
        {
            try
            {
                Trace.TraceInformation("Requesting authentication of user {0}", username);
                return Program._enterpriseAdapter.Authenticate(username, password, Program._adminGroup, Program._enterpriseDomain);
            }catch(Exception ex)
            {
                Trace.TraceError("Failed to authenticate user {0}, ({1})",username, ex);
                return null;
            }
        }

        
        public EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey)
        {
            try
            {
                Trace.TraceInformation("Requesting session details");
                return Program._enterpriseAdapter.GetSessionConnectionDetails(sessionID, hostID, sessionKey);
            }
            catch(Exception ex)
            {
                Trace.TraceError("Unable to get session connection details {0}", ex);
                return null;
            }
        }

        public List<EnterpriseHost> GetSessionHosts(string sessionID)
        {
            try
            {
                Trace.TraceInformation("Requesting session host list");
                return Program._enterpriseAdapter.SessionHosts(sessionID);
            }catch(Exception ex)
            {
                Trace.TraceError("Unable to get host list {0}", ex);
                return new List<EnterpriseHost>();
            }
        }

        public bool GetState()
        {
            return Program._enterpriseAdapter != null;
        }

        public void Logout(string sessionID)
        {
            Program._enterpriseAdapter.Logout(sessionID);
        }

        public string CreateUserSession(string sessionID, long hostID, string username, string password)
        {
            try
            {
                Trace.TraceInformation("Create user session requested, host {0}, user {1}", hostID, username);
                return Program._enterpriseAdapter.CreateUserSession(sessionID,hostID,username,password);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to create session {0}, ({1})", hostID, ex);
                return null;
            }
        }
    }
}
