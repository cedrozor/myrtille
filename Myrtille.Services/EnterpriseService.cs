/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

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
using System.Configuration;
using System.Diagnostics;
using System.Net;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class EnterpriseService : IEnterpriseService
    {
        public bool GetState()
        {
            return Program._enterpriseAdapter != null;
        }

        public EnterpriseSession Authenticate(string username, string password)
        {
            try
            {
                Trace.TraceInformation("Requesting authentication of user {0}", username);
                return Program._enterpriseAdapter.Authenticate(username, password, Program._adminGroup, Program._enterpriseDomain);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to authenticate user {0}, ({1})", username, ex);
                return null;
            }
        }

        public void Logout(string sessionID)
        {
            Program._enterpriseAdapter.Logout(sessionID);
        }

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

        public List<EnterpriseHost> GetSessionHosts(string sessionID)
        {
            try
            {
                Trace.TraceInformation("Requesting session host list");
                return Program._enterpriseAdapter.SessionHosts(sessionID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to get host list {0}", ex);
                return new List<EnterpriseHost>();
            }
        }

        public EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey)
        {
            try
            {
                Trace.TraceInformation("Requesting session details");
                var result = Program._enterpriseAdapter.GetSessionConnectionDetails(sessionID, hostID, sessionKey);
                var domain = ConfigurationManager.AppSettings["EnterpriseDomain"];
                if (result != null && domain != null 
                    && IPAddress.TryParse(domain, out IPAddress address) //check if domain is IP, prevent login failure if FQDN not used
                    && !result.PromptForCredentials) //no need to set this automatically if the user is prompted for credentials
                {
                    result.Domain = domain;
                }
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to get session connection details {0}", ex);
                return null;
            }
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

        public bool ChangeUserPassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                Trace.TraceInformation("Change password for user {0}", username);
                return Program._enterpriseAdapter.ChangeUserPassword(username, oldPassword, newPassword, Program._enterpriseDomain);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to change password for user {0}, ({1})", username, ex);
                return false;
            }
        }

        public bool AddSessionHostCredentials(EnterpriseHostSessionCredentials credentials)
        {
            try
            {
                Trace.TraceInformation("creating session credentials for {0}", credentials.Username);
                return Program._enterpriseAdapter.AddSessionHostCredentials(credentials);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to set session credentials for user {0}, ({1})", credentials.Username, ex);
                return false;
            }
        }
    }
}