using System;
using System.Data.SqlServerCe;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Myrtille.Common.Interfaces;
using Myrtille.Common.Models;

namespace Myrtille.Enterprise
{

    public class ActiveDirectory : IEnterpriseAdapter
    {
        public void Initialise()
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                db.Session.RemoveRange(db.Session);
                db.SaveChanges();
            }
        }

        public long? AddHost(EnterpriseHostEdit editHost, string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (!db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now)) return null;

                if (db.Host.Any(m => m.HostName.Equals(editHost.HostName,StringComparison.InvariantCultureIgnoreCase))) return null;

                List<string> groups = editHost.DirectoryGroups.Split(',').ToList();

                var host = new Host
                {
                    HostName = editHost.HostName,
                    HostAddress = editHost.HostAddress
                };

                db.Host.Add(host);

                db.SaveChanges();

                var hostAccess = groups.Select(x => new HostAccessGroups
                {
                    HostID = host.ID,
                    AccessGroup = x.Trim()
                });

                db.HostAccessGroups.AddRange(hostAccess.Where(m => m.AccessGroup != ""));
                db.SaveChanges();
                return host.ID;
            }
        }

        public bool UpdateHost(EnterpriseHostEdit editHost, string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (!db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now)) return false;

                var host = db.Host.FirstOrDefault(m => m.ID == editHost.HostID);

                host.HostName = editHost.HostName;
                host.HostAddress = editHost.HostAddress;

                var currentGroups = db.HostAccessGroups
                                        .Where(m => m.HostID == editHost.HostID)
                                        .ToList();

                IEnumerable<string> groups = editHost.DirectoryGroups.Split(',').ToList();

                var hostsToDelete = currentGroups.Where(m => !groups.Any(p => p.Equals(m.AccessGroup, StringComparison.InvariantCultureIgnoreCase)));

                db.HostAccessGroups.RemoveRange(hostsToDelete);

                var hostAccess = groups
                                    .Where(m => !currentGroups.Any(p => p.AccessGroup.Equals(m,StringComparison.InvariantCultureIgnoreCase)))
                                    .Select(x => new HostAccessGroups
                                        {
                                            HostID = host.ID,
                                            AccessGroup = x.Trim()
                                        });

                db.HostAccessGroups.AddRange(hostAccess.Where(m => m.AccessGroup != ""));

                db.SaveChanges();

                return true;
            }
        }

        public EnterpriseHostEdit GetHost(long hostID, string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (!db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now)) return null;

                var host = db.Host.FirstOrDefault(m => m.ID == hostID);

                if (host == null) return null;

                var directoryGroupList = db.HostAccessGroups
                            .Where(m => m.HostID == hostID)
                            .Select(m => m.AccessGroup)
                            .ToList();

                StringBuilder directoryGroups = new StringBuilder();
                var isFirst = true;
                foreach(string group in directoryGroupList)
                {
                    if (!isFirst) directoryGroups.Append(", ");
                    isFirst = false;
                    directoryGroups.Append(group);
                }
                return new EnterpriseHostEdit
                {
                    HostID = host.ID,
                    HostName = host.HostName,
                    HostAddress = host.HostAddress,
                    DirectoryGroups = directoryGroups.ToString()
                };
            }
        }

        public bool DeleteHost(long hostID, string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (!db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now)) return false;

                var host = db.Host.FirstOrDefault(m => m.ID == hostID);

                if (host == null) return false;

                db.Host.Remove(host);
                db.SaveChanges();
                return true;
            }
        }
        
        public EnterpriseSession Authenticate(string username, string password, string adminGroup, string domain)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domain, username, password))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                    DirectoryEntry entry = (DirectoryEntry)user.GetUnderlyingObject();
                    var directoryGroups = GetDirectoryGroups(entry);

                    bool isAdmin = directoryGroups.Any(m => m.Equals(adminGroup, StringComparison.InvariantCultureIgnoreCase));

                    string sessionID = Guid.NewGuid().ToString();
                    string sessionKey = Guid.NewGuid().ToString("n");
                    using (var db = new MyrtilleEnterpriseDBContext())
                    {
                        var session = db.Session.FirstOrDefault(m => m.Username == username);
                        if(session != null)
                        {
                            db.Session.Remove(session);
                            db.SaveChanges();
                        }

                        session = new Session
                        {
                            Username = username,
                            Password = AES_Encrypt(Myrtille.Helpers.RDPCryptoHelper.EncryptPassword(password),sessionKey),
                            SessionID = sessionID,
                            IsAdmin = isAdmin
                        };

                        db.Session.Add(session);
                        db.SaveChanges();

                        var groups = directoryGroups.Select(x => new SessionGroup
                        {
                            SessionID = session.ID,
                            DirectoryGroup = x
                        });

                        db.SessionGroup.AddRange(groups);
                        db.SaveChanges();
                        return new EnterpriseSession
                        {
                            SessionID = sessionID,
                            SessionKey = sessionKey,
                            IsAdmin = isAdmin
                            
                        };
                    }
                }
            }
            catch(Exception e)
            {
                return null;
            }

        }

        public void Logout(string sessionID)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var session = db.Session.FirstOrDefault(m => m.SessionID == sessionID);

                if(session != null)
                {
                    db.Session.Remove(session);
                    db.SaveChanges();
                }
            }
        }

        private List<string> GetDirectoryGroups(DirectoryEntry entry)
        {
            var directoryGroups = new List<string>();

            var groups = entry.Properties["memberOf"];

            foreach (string group in groups)
            {
                var startIndex = group.IndexOf("CN=");

                if (startIndex < 0)
                    continue;
                else
                    startIndex += 3;

                var endIndex = group.IndexOf("=", startIndex);

                if (endIndex < 0)
                    endIndex = group.Length - 3;
                else
                    endIndex = endIndex - 6;

                var length = endIndex - startIndex;

                directoryGroups.Add(group.Substring(startIndex, endIndex));
            }

            return directoryGroups;
        }

        public List<EnterpriseHost> SessionHosts(string sessionID)
        {
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

                if (sessionInfo == null) return new List<EnterpriseHost>();

                if (sessionInfo.IsAdmin)
                {
                    return (from s in db.Session
                            from h in db.Host 
                            where s.SessionID == sessionID
                            && s.Expire > DateTime.Now
                            select new EnterpriseHost
                            {
                                HostID = h.ID,
                                HostName = h.HostName,
                                HostAddress = h.HostAddress
                            })
                        .Distinct()
                        .OrderBy(m => m.HostName)
                        .ToList();
                }
                else
                {
                    return (from s in db.Session
                            join sg in db.SessionGroup on s.ID equals sg.SessionID
                            join hag in db.HostAccessGroups on sg.DirectoryGroup equals hag.AccessGroup
                            join h in db.Host on hag.HostID equals h.ID
                            where s.SessionID == sessionID
                            && s.Expire > DateTime.Now
                            select new EnterpriseHost
                            {
                                HostID = h.ID,
                                HostName = h.HostName,
                                HostAddress = h.HostAddress
                            })
                            .Distinct()
                            .OrderBy(m => m.HostName)
                            .ToList();
                }
            }
        }

        public EnterpriseConnectionDetails GetSessionConnectionDetails(string sessionID, long hostID, string sessionKey)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                var session = db.Session
                                .Where(m => m.SessionID == sessionID && m.Expire > DateTime.Now)
                                .Select(m => new
                                {
                                    SessionID = m.SessionID
                                    ,
                                    OneTime = m.OneTime
                                })
                                .FirstOrDefault();

                EnterpriseConnectionDetails result = null;
                if (session != null)
                {
                    if (session.OneTime)
                    {
                        result = (from s in db.Session
                                  from h in db.Host
                                  where s.SessionID == sessionID
                                     && h.ID == hostID
                                     && s.Expire > DateTime.Now
                                  select new EnterpriseConnectionDetails
                                  {
                                      HostID = h.ID
                                      ,
                                      HostName = h.HostName
                                      ,
                                      HostAddress = h.HostAddress
                                      ,
                                      Username = s.Username
                                      ,
                                      Password = s.Password
                                  })
                                .FirstOrDefault();
                    }
                    else
                    {
                        result = (from s in db.Session
                                  join sg in db.SessionGroup on s.ID equals sg.SessionID
                                  join hag in db.HostAccessGroups on sg.DirectoryGroup equals hag.AccessGroup
                                  join h in db.Host on hag.HostID equals h.ID
                                  where s.SessionID == sessionID
                                     && h.ID == hostID
                                     && s.Expire > DateTime.Now
                                  select new EnterpriseConnectionDetails
                                  {
                                      HostID = h.ID
                                      ,
                                      HostName = h.HostName
                                      ,
                                      HostAddress = h.HostAddress
                                      ,
                                      Username = s.Username
                                      ,
                                      Password = s.Password
                                  })
                                .FirstOrDefault();
                    }

                    if (result != null)
                    {
                        result.Password = AES_Decrypt(result.Password, sessionKey);
                    }
                }
                Logout(sessionID);

                return result;
            }
        }

        public string CreateUserSession(string sessionID, long hostID, string username, string password)
        {
            using (var db = new MyrtilleEnterpriseDBContext())
            {
                if (!db.Session.Any(m => m.SessionID == sessionID && m.IsAdmin && m.Expire > DateTime.Now)) return null;

                if (!db.Host.Any(m => m.ID == hostID)) return null;

                string newSessionID = Guid.NewGuid().ToString();
                string sessionKey = Guid.NewGuid().ToString("n");

                var session = new Session
                {
                    Username = username,
                    Password = AES_Encrypt(Myrtille.Helpers.RDPCryptoHelper.EncryptPassword(password), sessionKey),
                    SessionID = newSessionID,
                    IsAdmin = false,
                    Expire = DateTime.Now.AddHours(1),
                    OneTime = true
                };

                db.Session.Add(session);
                db.SaveChanges();

                return string.Format("?SI={0}&SD={1}&SK={2}",newSessionID,hostID,sessionKey);
            }
        }
        #region aes encryption
        private static string AES_Encrypt(string stringToBeEncrypted, string passwordString)
        {
            string encrypted;
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordString);
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(stringToBeEncrypted);

            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encrypted = Convert.ToBase64String(ms.ToArray());
                    
                }
            }

            return encrypted;
        }

        public string AES_Decrypt(string stringToBeDecrypted, string passwordString)
        {
            string decryptedString;
            byte[] bytesToBeDecrypted = Convert.FromBase64String(stringToBeDecrypted);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordString);

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedString = Encoding.UTF8.GetString(ms.ToArray());
                }
            }

            return decryptedString;
        }
        #endregion
    }
}
