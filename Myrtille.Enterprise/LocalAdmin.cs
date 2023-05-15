/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2021 Cedric Coste
    Copyright(c) 2018 Paul Oliver (Olive Innovations)

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS and is as" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;

namespace Myrtille.Enterprise
{
    public class LocalAdmin : IEnterpriseAdapter
    {
        public void Initialize()
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                db.Session.RemoveRange(db.Session);
                db.SaveChanges();
            }
        }

        public EnterpriseSession Authenticate(string username, string password, string adminGroup, string domain, string netbiosDomain)
        {
            EnterpriseSession enterpriseSession = null;

            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var localAdminUser = ((AppSettingsSection)config.GetSection("localAdmin")).Settings["LocalAdminUser"].Value;
                var localAdminPassword = ((AppSettingsSection)config.GetSection("localAdmin")).Settings["localAdminPassword"].Value;
                if (!username.Equals(localAdminUser))
                {
                    enterpriseSession = new EnterpriseSession
                    {
                        AuthenticationErrorCode = EnterpriseAuthenticationErrorCode.USER_NOT_FOUND
                    };
                }
                else
                {
                    if (!localAdminPassword.Equals("admin"))
                    {
                        localAdminPassword = CryptoHelper.AES_Decrypt(localAdminPassword, localAdminUser);
                    }
                    if (!password.Equals(localAdminPassword))
                    {
                        enterpriseSession = new EnterpriseSession
                        {
                            AuthenticationErrorCode = EnterpriseAuthenticationErrorCode.INVALID_LOGIN_CREDENTIALS
                        };
                    }
                    else
                    {
                        if (password.Equals("admin"))
                        {
                            enterpriseSession = new EnterpriseSession
                            {
                                AuthenticationErrorCode = EnterpriseAuthenticationErrorCode.PASSWORD_EXPIRED
                            };
                        }
                        else
                        {
                            using (var db = new MyrtilleEnterpriseDBContext())
                            {
                                var session = db.Session.FirstOrDefault(m => m.Username == username);
                                if (session != null)
                                {
                                    db.Session.Remove(session);
                                    db.SaveChanges();
                                }

                                string sessionID = Guid.NewGuid().ToString();
                                string sessionKey = Guid.NewGuid().ToString("n");

                                session = new Session
                                {
                                    Domain = netbiosDomain,
                                    Username = username,
                                    Password = CryptoHelper.AES_Encrypt(CryptoHelper.RDP_Encrypt(password), sessionKey),
                                    SessionID = sessionID,
                                    IsAdmin = true
                                };

                                db.Session.Add(session);
                                db.SaveChanges();

                                enterpriseSession = new EnterpriseSession
                                {
                                    Domain = netbiosDomain,
                                    UserName = username,
                                    SessionID = sessionID,
                                    SessionKey = sessionKey,
                                    IsAdmin = true,
                                    SingleUseConnection = false
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                enterpriseSession = new EnterpriseSession
                {
                    AuthenticationErrorCode = EnterpriseAuthenticationErrorCode.UNKNOWN_ERROR
                };
            }

            return enterpriseSession;
        }

        public void Logout(string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var session = db.Session.FirstOrDefault(m => m.SessionID == sessionID);
                if (session != null)
                {
                    db.Session.Remove(session);
                    db.SaveChanges();
                }
            }
        }

        public long? AddHost(EnterpriseHostEdit editHost, string sessionID)
        {
            long? hostId = null;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now) &&
                   !db.Host.Any(m => m.HostName.Equals(editHost.HostName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var host = new Host
                    {
                        HostName = editHost.HostName,
                        HostAddress = editHost.HostAddress,
                        VMGuid = editHost.VMGuid,
                        VMEnhancedMode = editHost.VMEnhancedMode,
                        Protocol = editHost.Protocol,
                        HostType = editHost.HostType,
                        StartRemoteProgram = editHost.StartRemoteProgram,
                        PromptForCredentials = editHost.PromptForCredentials
                    };

                    db.Host.Add(host);
                    db.SaveChanges();

                    hostId = host.ID;
                }
            }

            return hostId;
        }

        public EnterpriseHostEdit GetHost(long hostID, string sessionID)
        {
            EnterpriseHostEdit hostEdit = null;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now))
                {
                    var host = db.Host.FirstOrDefault(m => m.ID == hostID);
                    if (host != null)
                    {
                        hostEdit = new EnterpriseHostEdit
                        {
                            HostID = host.ID,
                            HostName = host.HostName,
                            HostAddress = host.HostAddress,
                            VMGuid = host.VMGuid,
                            VMEnhancedMode = host.VMEnhancedMode,
                            Protocol = host.Protocol,
                            HostType = host.HostType,
                            StartRemoteProgram = host.StartRemoteProgram,
                            PromptForCredentials = host.PromptForCredentials
                        };
                    }
                }
            }

            return hostEdit;
        }

        public bool UpdateHost(EnterpriseHostEdit editHost, string sessionID)
        {
            var success = false;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now))
                {
                    var host = db.Host.FirstOrDefault(m => m.ID == editHost.HostID);
                    if (host != null)
                    {
                        host.HostName = editHost.HostName;
                        host.HostAddress = editHost.HostAddress;
                        host.VMGuid = editHost.VMGuid;
                        host.VMEnhancedMode = editHost.VMEnhancedMode;
                        host.Protocol = editHost.Protocol;
                        host.StartRemoteProgram = editHost.StartRemoteProgram;
                        host.PromptForCredentials = editHost.PromptForCredentials;

                        db.SaveChanges();
                        success = true;
                    }
                }
            }

            return success;
        }

        public bool DeleteHost(long hostID, string sessionID)
        {
            var success = false;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now))
                {
                    var host = db.Host.FirstOrDefault(m => m.ID == hostID);
                    if (host != null)
                    {
                        db.Host.Remove(host);
                        db.SaveChanges();
                        success = true;
                    }
                }
            }

            return success;
        }

        public List<EnterpriseHost> SessionHosts(string sessionID)
        {
            var hosts = new List<EnterpriseHost>();

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var sessionInfo = db.Session
                                    .Where(m => m.SessionID == sessionID && m.Expire > DateTime.Now)
                                    .Select(m => new
                                    {
                                        SessionID = m.SessionID,
                                        IsAdmin = m.IsAdmin
                                    })
                                    .FirstOrDefault();

                if (sessionInfo != null && sessionInfo.IsAdmin)
                {
                    hosts = (from s in db.Session
                             from h in db.Host
                             where s.SessionID == sessionID
                             && s.Expire > DateTime.Now
                             select new EnterpriseHost
                             {
                                 HostID = h.ID,
                                 HostName = h.HostName,
                                 HostAddress = h.HostAddress,
                                 VMGuid = h.VMGuid,
                                 VMEnhancedMode = h.VMEnhancedMode,
                                 HostType = h.HostType,
                                 StartRemoteProgram = h.StartRemoteProgram,
                                 PromptForCredentials = h.PromptForCredentials
                             })
                        .Distinct()
                        .OrderBy(m => m.HostName)
                        .ToList();
                }
            }

            return hosts;
        }

        public EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey)
        {
            EnterpriseConnectionDetails connection = null;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var sessionInfo = db.Session
                                    .Where(m => m.SessionID == sessionID && m.Expire > DateTime.Now)
                                    .Select(m => new
                                    {
                                        SessionID = m.SessionID,
                                        OneTime = m.OneTime,
                                        IsAdmin = m.IsAdmin
                                    })
                                    .FirstOrDefault();

                if (sessionInfo != null)
                {
                    if (sessionInfo.OneTime)
                    {
                        connection = (from s in db.Session
                                      from h in db.Host
                                      where s.SessionID == sessionID
                                      && h.ID == hostID
                                      && s.Expire > DateTime.Now
                                      select new EnterpriseConnectionDetails
                                      {
                                          HostID = h.ID,
                                          HostName = h.HostName,
                                          HostAddress = h.HostAddress,
                                          VMGuid = h.VMGuid,
                                          VMEnhancedMode = h.VMEnhancedMode,
                                          HostType = h.HostType,
                                          Domain = s.Domain,
                                          Username = s.Username,
                                          Password = s.Password,
                                          Protocol = h.Protocol,
                                          StartRemoteProgram = h.StartRemoteProgram
                                      })
                                    .FirstOrDefault();
                    }
                    else if (sessionInfo.IsAdmin)
                    {
                        connection = (from s in db.Session
                                      from h in db.Host
                                      join sc in db.SessionHostCredentials on new { x1 = s.ID, x2 = h.ID } equals new { x1 = sc.SessionID, x2 = sc.HostID } into scl
                                      from sc in scl.DefaultIfEmpty()
                                      where s.SessionID == sessionID
                                      && h.ID == hostID
                                      && s.Expire > DateTime.Now
                                      select new EnterpriseConnectionDetails
                                      {
                                          HostID = h.ID,
                                          HostName = h.HostName,
                                          HostAddress = h.HostAddress,
                                          VMGuid = h.VMGuid,
                                          VMEnhancedMode = h.VMEnhancedMode,
                                          HostType = h.HostType,
                                          Domain = sc.Domain,
                                          Username = sc.Username,
                                          Password = sc.Password,
                                          Protocol = h.Protocol,
                                          StartRemoteProgram = h.StartRemoteProgram
                                      })
                                    .FirstOrDefault();
                    }

                    if (connection != null)
                    {
                        connection.Password = CryptoHelper.AES_Decrypt(connection.Password, sessionKey);
                    }

                    // when connected from the login page, the session logout is based on expiration or user action
                    // when connected from a one time url, the logout is done immediately
                    if (sessionInfo.OneTime)
                    {
                        Logout(sessionID);
                    }
                }
            }

            return connection;
        }

        public string CreateUserSession(string sessionID, long hostID, string username, string password, string domain)
        {
            string sessionUrl = null;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now) &&
                    db.Host.Any(m => m.ID == hostID))
                {
                    string newSessionID = Guid.NewGuid().ToString();
                    string sessionKey = Guid.NewGuid().ToString("n");

                    var session = new Session
                    {
                        Domain = domain,
                        Username = username,
                        Password = CryptoHelper.AES_Encrypt(CryptoHelper.RDP_Encrypt(password), sessionKey),
                        SessionID = newSessionID,
                        IsAdmin = false,
                        Expire = DateTime.Now.AddHours(1),
                        OneTime = true
                    };

                    db.Session.Add(session);
                    db.SaveChanges();

                    sessionUrl = string.Format("?SI={0}&SD={1}&SK={2}", newSessionID, hostID, sessionKey);
                }
            }

            return sessionUrl;
        }

        public bool ChangeUserPassword(string username, string oldPassword, string newPassword, string domain)
        {
            var success = false;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var localAdminUser = ((AppSettingsSection)config.GetSection("localAdmin")).Settings["LocalAdminUser"].Value;
            var localAdminPassword = ((AppSettingsSection)config.GetSection("localAdmin")).Settings["localAdminPassword"].Value;
            if (username.Equals(localAdminUser))
            {
                if (!localAdminPassword.Equals("admin"))
                {
                    localAdminPassword = CryptoHelper.AES_Decrypt(localAdminPassword, localAdminUser);
                }
                if (oldPassword.Equals(localAdminPassword))
                {
                    ((AppSettingsSection)config.GetSection("localAdmin")).Settings["localAdminPassword"].Value = CryptoHelper.AES_Encrypt(newPassword, localAdminUser);
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("localAdmin");
                    success = true;
                }
            }

            return success;
        }

        public bool AddSessionHostCredentials(EnterpriseHostSessionCredentials credentials)
        {
            var success = false;

            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var session = db.Session.FirstOrDefault(m => m.SessionID == credentials.SessionID);
                if (session != null && db.Host.Any(m => m.ID == credentials.HostID))
                {
                    var sessionHost = db.SessionHostCredentials.FirstOrDefault(m =>
                        m.SessionID == session.ID &&
                        m.HostID == credentials.HostID);

                    if (sessionHost != null)
                    {
                        db.SessionHostCredentials.Remove(sessionHost);
                    }

                    sessionHost = new SessionHostCredential
                    {
                        SessionID = session.ID,
                        HostID = credentials.HostID,
                        Domain = credentials.Domain,
                        Username = credentials.Username,
                        Password = CryptoHelper.AES_Encrypt(CryptoHelper.RDP_Encrypt(credentials.Password), credentials.SessionKey)
                    };

                    db.SessionHostCredentials.Add(sessionHost);
                    db.SaveChanges();
                    success = true;
                }
            }

            return success;
        }
    }
}
