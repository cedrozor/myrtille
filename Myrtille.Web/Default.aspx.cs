/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste

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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using Myrtille.Helpers;
using Myrtille.Common.Models;
using System.Web.Services;

namespace Myrtille.Web
{
    public partial class Default : Page
    {
        /// <summary>
        /// initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            try
            {
                var MFAAuthClient = new MFAAuthenticationClient();
                var EnterpriseClient = new EnterpriseServiceClient();

                if (MFAAuthClient.GetState())
                {
                    mfaDiv.Visible = true;
                    mfaProvider.HRef = MFAAuthClient.GetProviderURL();
                    mfaProvider.InnerText = MFAAuthClient.GetPromptLabel();
                }

                if (EnterpriseClient.GetState())
                {
                    domainServerDiv.Visible = false;
                    login.Attributes.Add("class", "enterpriseLogin");
                }
                else
                {
                    domainServerDiv.Visible = true;
                    login.Attributes.Add("class", "standardLogin");
                }

                // update controls
                UpdateControls();

                // disable the browser cache; in addition to a "noCache" dummy param, with current time, on long-polling and xhr requests
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to load myrtille ({0})", exc);
            }
        }

        /// <summary>
        /// force remove the .net viewstate hidden fields from page (large bunch of unwanted data in url)
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(
            HtmlTextWriter writer)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var tw = new HtmlTextWriter(sw);
            base.Render(tw);
            var html = sb.ToString();
            html = Regex.Replace(html, "<input[^>]*id=\"(__VIEWSTATE)\"[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<input[^>]*id=\"(__VIEWSTATEGENERATOR)\"[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            writer.Write(html);
        }


        #region helper methods
        /// <summary>
        /// update the UI
        /// </summary>
        private static AdditionalControls UpdateControls()
        {
            var additionalControls = new AdditionalControls();
            var remoteSession = RetrieveRemoteSession();

            if (remoteSession != null)
            {
                // login screen
                additionalControls.LoginControlsIsVisible = remoteSession.State != RemoteSessionState.Connecting && remoteSession.State != RemoteSessionState.Connected;

                additionalControls.ToolBarIsVisible = additionalControls.LoginControlsIsVisible || remoteSession.State == RemoteSessionState.Disconnected;
                additionalControls.ServerAddress = remoteSession.ServerAddress;
                additionalControls.StatValue = remoteSession.StatMode ? "Hide Stat" : "Show Stat";
                additionalControls.StatIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.DebugValue = remoteSession.DebugMode ? "Hide Debug" : "Show Debug";
                additionalControls.DebugIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.BrowserValue = remoteSession.CompatibilityMode ? "HTML5" : "HTML4";
                additionalControls.BrowserIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.ScaleValue = remoteSession.ScaleDisplay ? "Unscale" : "Scale";
                additionalControls.ScaleIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.KeyboardIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.ClipboardIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.FilesIsDisabled = additionalControls.LoginControlsIsVisible || (remoteSession.ServerAddress.ToLower() != "localhost" && remoteSession.ServerAddress != "127.0.0.1" && remoteSession.ServerAddress != HttpContext.Current.Request.Url.Host && string.IsNullOrEmpty(remoteSession.UserDomain)) || string.IsNullOrEmpty(remoteSession.UserName) || string.IsNullOrEmpty(remoteSession.UserPassword);
                additionalControls.CadIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.DisconnectIsDisabled = additionalControls.LoginControlsIsVisible;
                additionalControls.MrcIsDisabled = additionalControls.LoginControlsIsVisible;
            }
            else
            {
                additionalControls.ToolBarIsVisible = false;
            }

            return additionalControls;
        }


        private static IList<ServerConfiguration> LoadServerList(string enterpriseSessionID)
        {
            var EnterpriseClient = new EnterpriseServiceClient();
            List<EnterpriseHost> hosts = EnterpriseClient.GetSessionHosts(enterpriseSessionID);
            var serverList = new List<ServerConfiguration>();

            foreach (var host in hosts)
            {
                serverList.Add(new ServerConfiguration()
                {
                    ServerId = host.HostID,
                    ServerName = host.HostName
                });
            }

            return serverList;
        }

        private static RemoteSession RetrieveRemoteSession()
        {
            RemoteSession localRemoteSession = null;

            try
            {
                localRemoteSession = (RemoteSession)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()];
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", HttpContext.Current.Session.SessionID, exc);
            }

            return localRemoteSession;
        }

        protected static void DisconnectSession(bool enterpriseEnabled, string enterpriseSessionId)
        {
            var EnterpriseClient = new EnterpriseServiceClient();
            var RemoteSession = RetrieveRemoteSession();
            // always logout if session exists
            if (!String.IsNullOrEmpty(enterpriseSessionId))
                EnterpriseClient.Logout(enterpriseSessionId);

            // disconnect the active remote session, if any and connected
            if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                try
                {
                    // update the remote session state
                    RemoteSession.State = RemoteSessionState.Disconnecting;

                    // send a disconnect command to the rdp client
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.CloseRdpClient);
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to disconnect the remote session {0} ({1})", RemoteSession.Id, exc);
                }
            }

            // always update controls
            UpdateControls();
        }

        #endregion

        #region webmethods
        [WebMethod(EnableSession = true)]
        public static StandardHttpResponse ConnectLogin(LoginHttpRequest loginRequest)
        {
            try
            {
                var MFAAuthClient = new MFAAuthenticationClient();
                var EnterpriseClient = new EnterpriseServiceClient();
                if (MFAAuthClient.GetState())
                {
                    if (!MFAAuthClient.Authenticate(loginRequest.Username, loginRequest.MFAPassword))
                    {
                        return new StandardHttpResponse
                        {
                            Success = false,
                            Message = "MFA Authentication failed!"
                        };
                    }
                }

                EnterpriseSession enterpriseSession = EnterpriseClient.Authenticate(loginRequest.Username, loginRequest.Password);

                if (enterpriseSession != null)
                {
                    var loginResponse = new LoginHttpResponse
                    {
                        Success = true,
                        Message = "Successful",
                        EnterpriseUser = loginRequest.Username,
                        EnterpriseSessionID = enterpriseSession.SessionID,
                        EnterpriseSessionKey = enterpriseSession.SessionKey,
                        IsAdmin = enterpriseSession.IsAdmin,
                        ServerList = LoadServerList(enterpriseSession.SessionID),
                        AdditionalControls = UpdateControls() // display the toolbar?
                    };

                    return loginResponse;
                }
                else
                {
                    return new StandardHttpResponse
                    {
                        Success = false,
                        Message = "Invalid login credentials"
                    };

                }
            }
            catch (Exception exc)
            {
                // todo - extract error messages into separate class
                System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);

                return new StandardHttpResponse
                {
                    Success = false,
                    Message = "Failed to create remote session ({0})"
                };
            }
        }

        [WebMethod(EnableSession = true)]
        public static StartSessionHttpResponse StartServerSession(StartSessionHttpRequest startSessionRequest)
        {
            var MFAAuthClient = new MFAAuthenticationClient();
            var EnterpriseClient = new EnterpriseServiceClient();
            if (MFAAuthClient.GetState() && !EnterpriseClient.GetState())
            {
                var result = MFAAuthClient.Authenticate(startSessionRequest.Username, startSessionRequest.MFAPassword);

                if (!result)
                {
                    return new Web.StartSessionHttpResponse
                    {
                        Success = false,
                        Message = "MFA Authentication failed!"
                    };
                }
            }

            var RemoteSession = RetrieveRemoteSession();
            var startSessionResponse = new StartSessionHttpResponse();

            if (RemoteSession != null)
            {
                try
                {
                    // unset the remote session for the current http session
                    HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to remove remote session ({0})", exc);
                }
                finally
                {
                    RemoteSession = null;
                }
            }

            // create a new remote session
            try
            {
                HttpContext.Current.Application.Lock();

                var loginUsername = startSessionRequest.Username;
                var loginPassword = startSessionRequest.Password;
                var loginDomain = startSessionRequest.Domain;
                var loginServer = startSessionRequest.Server;

                if (EnterpriseClient.GetState())
                {
                    var enterpriseConnection = EnterpriseClient.GetSessionConnectionDetails(startSessionRequest.SessionID, int.Parse(startSessionRequest.ServerID), startSessionRequest.SessionKey);

                    if (enterpriseConnection == null)
                    {
                        return new StartSessionHttpResponse { Success = false };
                    }
                    else
                    {
                        loginUsername = enterpriseConnection.Username;
                        loginPassword = RDPCryptoHelper.DecryptPassword(enterpriseConnection.Password);
                        loginDomain = "";
                        loginServer = (!string.IsNullOrEmpty(enterpriseConnection.HostAddress) ? enterpriseConnection.HostAddress : enterpriseConnection.HostName);
                    }
                }

                // auto-increment the remote sessions counter
                // note that it doesn't really count the active remote sessions... it's just an auto-increment for the remote session id, ensuring it's unique...
                var remoteSessionsCounter = (int)HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()];
                remoteSessionsCounter++;

                // create the remote session
                RemoteSession = new RemoteSession
                {
                    Id = remoteSessionsCounter,
                    State = RemoteSessionState.NotConnected,
                    ServerAddress = loginServer,
                    UserDomain = loginDomain,
                    UserName = loginUsername,
                    UserPassword = loginPassword,
                    ClientWidth = startSessionRequest.Width,
                    ClientHeight = startSessionRequest.Height,
                    Program = startSessionRequest.ProgramValue
                };

                // set the remote session for the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = RemoteSession;

                // register the http session at application level
                var httpSessions = (IDictionary<string, HttpSessionState>)HttpContext.Current.Application[HttpApplicationStateVariables.HttpSessions.ToString()];
                httpSessions[HttpContext.Current.Session.SessionID] = HttpContext.Current.Session;

                // update the remote sessions auto-increment counter
                HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = remoteSessionsCounter;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);
                RemoteSession = null;
            }
            finally
            {
                HttpContext.Current.Application.UnLock();
            }

            // connect it
            if (RemoteSession != null)
            {
                try
                {
                    // update the remote session state
                    RemoteSession.State = RemoteSessionState.Connecting;

                    // create pipes for the web gateway and the rdp client to talk
                    RemoteSession.Manager.Pipes.CreatePipes();

                    // the rdp client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the rdp session had ended), pipes are released
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    RemoteSession.Manager.Client.StartProcess(
                        RemoteSession.Id,
                        RemoteSession.ClientWidth,
                        RemoteSession.ClientHeight);

                    // update controls
                    startSessionResponse.AdditionalControls = UpdateControls();
                    startSessionResponse.RemoteSessionDetails = RemoteSession.RemoteSessionDetails;
                    startSessionResponse.Success = true;

                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to connect the remote session {0} ({1})", RemoteSession.Id, exc);
                }
            }


            return startSessionResponse;
        }

        [WebMethod(EnableSession = true)]
        public static StandardHttpResponse ConnectLogout(LogoutHttpRequest logoutRequest)
        {
            var EnterpriseClient = new EnterpriseServiceClient();
            // clear session            
            DisconnectSession(EnterpriseClient.GetState(), logoutRequest.EnterpriseSessionId);

            return new StandardHttpResponse()
            {
                Success = true,
                Message = "Logged out successfully"
            };
        }

        [WebMethod(EnableSession = true)]
        public static AddHostHttpResponse SaveHost(AddHostHttpRequest addHostRequest)
        {
            try
            {
                var EnterpriseClient = new EnterpriseServiceClient();
                long? serverID;
                if (addHostRequest.HostID == null)
                {
                    serverID = EnterpriseClient.AddHost(new EnterpriseHostEdit
                    {
                        HostID = 0,
                        HostName = addHostRequest.HostName,
                        HostAddress = addHostRequest.HostAddress,
                        DirectoryGroups = addHostRequest.DirectoryGroups
                    },
                                addHostRequest.SessionID);

                    return new AddHostHttpResponse
                    {
                        Success = serverID != null,
                        Message = (serverID == null ? string.Format("Failed to add host {0}, check it does not already exist!", addHostRequest.HostName) : ""),
                        ServerId = serverID,
                        ServerName = addHostRequest.HostName
                    };
                }
                else
                {
                    var result = EnterpriseClient.UpdateHost(new EnterpriseHostEdit
                    {
                        HostID = (long)addHostRequest.HostID,
                        HostName = addHostRequest.HostName,
                        HostAddress = addHostRequest.HostAddress,
                        DirectoryGroups = addHostRequest.DirectoryGroups
                    }, addHostRequest.SessionID);

                    return new AddHostHttpResponse
                    {
                        Success = result,
                        Message = (result ? "" : string.Format("Failed to update host {0}!", addHostRequest.HostName))
                    };
                }

            }
            catch (Exception e)
            {
                return new AddHostHttpResponse
                {
                    Success = false,
                    Message = string.Format("Failed to add host {0}, check it does not already exist!", addHostRequest.HostName)
                };
            }
        }

        [WebMethod(EnableSession = true)]
        public static DeleteHostHttpResponse DeleteHost(DeleteHostHttpRequest deleteHostRequest)
        {
            try
            {
                var EnterpriseClient = new EnterpriseServiceClient();
                var result = EnterpriseClient.DeleteHost(deleteHostRequest.HostID, deleteHostRequest.SessionID);

                return new DeleteHostHttpResponse
                {
                    Success = result,
                    Message = (result ? "" : "An error occured deleting host!"),
                    HostID = (result ? deleteHostRequest.HostID : 0)
                };
            }
            catch (Exception e)
            {
                return new DeleteHostHttpResponse
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        [WebMethod(EnableSession = true)]
        public static EditHostHttpResponse GetHost(EditHostHttpRequest editHostRequest)
        {
            try
            {
                var EnterpriseClient = new EnterpriseServiceClient();
                var result = EnterpriseClient.GetHost(editHostRequest.HostID, editHostRequest.SessionID);

                return new EditHostHttpResponse
                {
                    Success = (result != null),
                    Message = (result == null ? "" : "An error occured deleting host!"),
                    HostID = result?.HostID,
                    HostName = result?.HostName,
                    HostAddress = result?.HostAddress,
                    DirectoryGroups = result?.DirectoryGroups
                };
            }
            catch (Exception e)
            {
                return new EditHostHttpResponse
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        [WebMethod(EnableSession = true)]
        public static CreateUserSessionHttpResponse CreateSession(CreateUserSessionHttpRequest createSessionRequest)
        {
            try
            {
                var EnterpriseClient = new EnterpriseServiceClient();
                var result = EnterpriseClient.CreateUserSession(createSessionRequest.SessionID, createSessionRequest.HostID, createSessionRequest.Username, createSessionRequest.Password);


                if (result == null)
                {
                    return new CreateUserSessionHttpResponse
                    {
                        Success = false
                    };
                }
                else
                {
                    Uri uri = HttpContext.Current.Request.Url;

                    var sessionURL = uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port == 443 || uri.Port == 80 ? "" : ":" + uri.Port) + "/Default.aspx" + result;

                    return new CreateUserSessionHttpResponse
                    {
                        Success = true,
                        Message = "",
                        SessionURL = sessionURL
                    };
                }
            }
            catch (Exception e)
            {
                return new CreateUserSessionHttpResponse
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }
        #endregion
    }
}