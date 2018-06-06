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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class Default : Page
    {
        private MFAAuthenticationClient _mfaAuthClient;
        private EnterpriseServiceClient _enterpriseClient;

        private bool _authorizedRequest = true;

        private EnterpriseSession _enterpriseSession;
        protected RemoteSession RemoteSession;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            _mfaAuthClient = new MFAAuthenticationClient();
            _enterpriseClient = new EnterpriseServiceClient();
        }

        /// <summary>
        /// page load (postback data is now available)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // client ip protection
            if (ClientIPTracking())
            {
                var clientIP = ClientIPHelper.ClientIPFromRequest(new HttpContextWrapper(HttpContext.Current).Request, true, new string[] { });
                if (Session[HttpSessionStateVariables.ClientIP.ToString()] == null)
                {
                    Session[HttpSessionStateVariables.ClientIP.ToString()] = clientIP;
                }
                else if (!((string)Session[HttpSessionStateVariables.ClientIP.ToString()]).Equals(clientIP))
                {
                    System.Diagnostics.Trace.TraceWarning("Failed to validate the client ip");
                    _authorizedRequest = false;
                    UpdateControls();
                    return;
                }
            }

            // session spoofing protection
            if (IsCookielessSession())
            {
                if (Request.Cookies["clientKey"] == null)
                {
                    if (Session[HttpSessionStateVariables.ClientKey.ToString()] == null)
                    {
                        var cookie = new HttpCookie("clientKey");
                        cookie.Value = Guid.NewGuid().ToString();
                        cookie.Path = "/";
                        Response.Cookies.Add(cookie);
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning("Failed to validate the client key: missing key");
                        _authorizedRequest = false;
                        UpdateControls();
                        return;
                    }
                }
                else
                {
                    var clientKey = Request.Cookies["clientKey"].Value;
                    if (Session[HttpSessionStateVariables.ClientKey.ToString()] == null)
                    {
                        Session[HttpSessionStateVariables.ClientKey.ToString()] = clientKey;
                    }
                    else if (!((string)Session[HttpSessionStateVariables.ClientKey.ToString()]).Equals(clientKey))
                    {
                        System.Diagnostics.Trace.TraceWarning("Failed to validate the client key: key mismatch");
                        _authorizedRequest = false;
                        UpdateControls();
                        return;
                    }
                }
            }

            // retrieve the active enterprise session, if any
            if (Session[HttpSessionStateVariables.EnterpriseSession.ToString()] != null)
            {
                try
                {
                    _enterpriseSession = (EnterpriseSession)Session[HttpSessionStateVariables.EnterpriseSession.ToString()];
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the active enterprise session ({0})", exc);
                }
            }

            // retrieve the active remote session, if any
            if (Session[HttpSessionStateVariables.RemoteSession.ToString()] != null)
            {
                try
                {
                    RemoteSession = (RemoteSession)Session[HttpSessionStateVariables.RemoteSession.ToString()];
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
                }
            }
            // retrieve a shared remote session from url, if any
            else if (Request["SSE"] != null)
            {
                Session[HttpSessionStateVariables.RemoteSession.ToString()] = GetSharedRemoteSession(Request["SSE"]);

                try
                {
                    // remove the shared session guid from url
                    Response.Redirect("~/", true);
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
            }

            // postback events may redirect after execution; UI is updated from there
            if (!IsPostBack)
            {
                UpdateControls();
            }

            // disable the browser cache; in addition to a "noCache" dummy param, with current time, on long-polling and xhr requests
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
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

        /// <summary>
        /// page unload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Unload(
            object sender,
            EventArgs e)
        {
            // if there is a disconnected remote session with an unexpected exit code, reset the exit code (so that the related error message is displayed only once if the page is reloaded)
            if (RemoteSession != null && RemoteSession.State == RemoteSessionState.Disconnected && RemoteSession.ExitCode != 0)
            {
                RemoteSession.ExitCode = 0;
            }
        }

        /// <summary>
        /// update the UI
        /// </summary>
        private void UpdateControls()
        {
            // hosts list
            if (_enterpriseSession != null && _enterpriseSession.AuthenticationErrorCode == EnterpriseAuthenticationErrorCode.NONE && (RemoteSession == null || RemoteSession.State == RemoteSessionState.Disconnecting || RemoteSession.State == RemoteSessionState.Disconnected))
            {
                toolbar.Style["visibility"] = "hidden";
                toolbar.Style["display"] = "none";
                hosts.Visible = true;
                newHost.Visible = _enterpriseSession.IsAdmin;
                newSSHHost.Visible = _enterpriseSession.IsAdmin;
                hostsList.DataSource = _enterpriseClient.GetSessionHosts(_enterpriseSession.SessionID);
                hostsList.DataBind();
            }
            // active remote session
            else if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                toolbar.Style["visibility"] = "visible";
                toolbar.Style["display"] = "block";
                serverInfo.Value = (!string.IsNullOrEmpty(RemoteSession.ServerName) ? RemoteSession.ServerName : RemoteSession.ServerAddress);
                stat.Value = RemoteSession.StatMode ? "Hide Stat" : "Show Stat";
                stat.Disabled = false;
                debug.Value = RemoteSession.DebugMode ? "Hide Debug" : "Show Debug";
                debug.Disabled = false;
                browser.Value = RemoteSession.CompatibilityMode ? "HTML5" : "HTML4";
                browser.Disabled = false;
                scale.Value = RemoteSession.ScaleDisplay ? "Unscale" : "Scale";
                scale.Disabled = RemoteSession.HostType == HostTypeEnum.RDP;
                keyboard.Disabled = false;
                // disable clipboard for SSH or if set to disable in config
                clipboard.Disabled = RemoteSession.HostType == HostTypeEnum.SSH || !RemoteSession.AllowRemoteClipboard;
                // disable files for SSH or if set to disable in config
                files.Disabled = RemoteSession.HostType == HostTypeEnum.SSH || (RemoteSession.ServerAddress.ToLower() != "localhost" && RemoteSession.ServerAddress != "127.0.0.1" && RemoteSession.ServerAddress != "[::1]" && RemoteSession.ServerAddress != Request.Url.Host && string.IsNullOrEmpty(RemoteSession.UserDomain));
                cad.Disabled = RemoteSession.HostType == HostTypeEnum.SSH; // disable ctrl + alt + del for SSH
                mrc.Disabled = RemoteSession.HostType == HostTypeEnum.SSH; // disable mouse right click for SSH
                share.Disabled = !RemoteSession.AllowSessionSharing || !Session.SessionID.Equals(RemoteSession.OwnerSessionID);
                disconnect.Disabled = false;
            }
            // login screen
            else
            {
                login.Visible = true;

                // MFA
                if (_mfaAuthClient.GetState())
                {
                    mfaDiv.Visible = true;
                    mfaProvider.InnerText = _mfaAuthClient.GetPromptLabel();
                    mfaProvider.HRef = _mfaAuthClient.GetProviderURL();
                }

                // enterprise mode
                if (_enterpriseClient.GetState())
                {
                    domainServerDiv.Visible = false;
                }
                // standard mode
                else
                {
                    connect.Attributes["onclick"] = "showToolbar();";
                }
            }
        }

        /// <summary>
        /// enterprise mode from url: load the enterprise session (from querystring param) and proceed to connection; the user is non admin and the url is only usable once
        /// enterprise mode from login: authenticate the user against the enterprise active directory and list the servers available to the user; the user is admin if member of the "EnterpriseAdminGroup" defined into myrtille services config
        /// standard mode: connect the specified server; the rdp authentication is delegated to the rdp server or connection broker (if applicable)
        /// if MFA is enabled and not already processed, authenticate the user against the configured MFA provider (OTP preferred)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ConnectButtonClick(
            object sender,
            EventArgs e)
        {
            if (!_authorizedRequest)
                return;

            // one time usage enterprise session url
            if (Request["SI"] != null && Request["SD"] != null && Request["SK"] != null)
            {
                CreateEnterpriseSessionFromUrl();
            }

            // MFA (OTP passcode)
            if (_enterpriseSession == null && _mfaAuthClient.GetState())
            {
                var clientIP = ClientIPHelper.ClientIPFromRequest(new HttpContextWrapper(HttpContext.Current).Request, true, new string[] { });
                if (!_mfaAuthClient.Authenticate(user.Value, mfaPassword.Value, clientIP))
                {
                    connectError.InnerText = "MFA Authentication failed!";
                    UpdateControls();
                    return;
                }
            }

            // enterprise mode from login
            if (_enterpriseSession == null && _enterpriseClient.GetState())
            {
                CreateEnterpriseSessionFromLogin();
            }
            // connection from:
            // > standard mode
            // > enterprise mode: hosts list
            // > enterprise mode: one time session url
            else
            {
                // the display size is required to start a remote session
                // if missing, the client will provide it automatically
                if (string.IsNullOrEmpty(width.Value) || string.IsNullOrEmpty(height.Value))
                {
                    return;
                }

                // connect
                if (ConnectRemoteServer())
                {
                    // in enterprise mode from login, a new http session id was already generated (no need to do it each time an host is connected!)
                    // in standard mode or enterprise mode from url, a new http session id must be generated
                    if (_enterpriseSession == null || Request["SI"] != null)
                    {
                        // session fixation protection
                        if (IsCookielessSession())
                        {
                            // generate a new http session id
                            RemoteSession.OwnerSessionID = HttpSessionHelper.RegenerateSessionId();
                        }
                    }
                    try
                    {
                        // standard mode: switch to http get (standard login) or remove the connection params from url (auto-connect / start program from url)
                        // enterprise mode: remove the host id from url
                        Response.Redirect("~/", true);
                    }
                    catch (ThreadAbortException)
                    {
                        // occurs because the response is ended after redirect
                    }
                }
                // connection failed from the hosts list or from a one time session url
                else if (_enterpriseSession != null && Request["SD"] != null)
                {
                    try
                    {
                        // remove the host id from url
                        Response.Redirect("~/", true);
                    }
                    catch (ThreadAbortException)
                    {
                        // occurs because the response is ended after redirect
                    }
                }
            }
        }

        /// <summary>
        /// connect the rdp server
        /// </summary>
        /// <remarks>
        /// the rdp authentication is delegated to the rdp server or connection broker (if applicable)
        /// </remarks>
        private bool ConnectRemoteServer()
        {
            // connection parameters
            string loginHostName = null;
            var loginServer = string.IsNullOrEmpty(server.Value) ? "localhost" : server.Value;
            var loginDomain = domain.Value;
            var loginUser = user.Value;
            var loginPassword = string.IsNullOrEmpty(passwordHash.Value) ? password.Value : RDPCryptoHelper.DecryptPassword(passwordHash.Value);
            var loginProtocol = SecurityProtocolEnum.auto;
            var startProgram = program.Value;
            var loginHostType = hostType.SelectedValue;

            // connect an host from the hosts list or from a one time session url
            if (_enterpriseSession != null && Request["SD"] != null)
            {
                long hostId = 0;
                long lResult = 0;
                if (long.TryParse(Request["SD"], out lResult))
                {
                    hostId = lResult;
                }

                try
                {
                    // retrieve the host connection details
                    var connection = _enterpriseClient.GetSessionConnectionDetails(_enterpriseSession.SessionID, hostId, _enterpriseSession.SessionKey);
                    if (connection == null)
                    {
                        System.Diagnostics.Trace.TraceInformation("Unable to retrieve host {0} connection details (invalid host or one time session url already used?)", hostId);
                        return false;
                    }
                    loginHostName = connection.HostName;
                    loginServer = !string.IsNullOrEmpty(connection.HostAddress) ? connection.HostAddress : connection.HostName;
                    loginDomain = connection.Domain;
                    loginUser = connection.Username;
                    loginPassword = RDPCryptoHelper.DecryptPassword(connection.Password);
                    loginProtocol = connection.Protocol;
                    loginHostType = connection.HostType;
                    startProgram = connection.StartRemoteProgram;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0} connection details ({1})", hostId, exc);
                    return false;
                }
            }

            // remote clipboard
            var allowRemoteClipboard = true;
            bool bResult = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["allowRemoteClipboard"], out bResult))
            {
                allowRemoteClipboard = bResult;
            }

            // session sharing
            var allowSessionSharing = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["allowSessionSharing"], out bResult))
            {
                allowSessionSharing = bResult;
            }

            // remove any active remote session (disconnected?)
            if (RemoteSession != null)
            {
                // unset the remote session for the current http session
                Session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                RemoteSession = null;
            }

            // create a new remote session
            try
            {
                Application.Lock();

                // auto-increment the remote sessions counter
                // note that it doesn't really count the active remote sessions... it's just an auto-increment for the remote session id, ensuring it's unique...
                var remoteSessionsCounter = (int)Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()];
                remoteSessionsCounter++;

                // create the remote session
                RemoteSession = new RemoteSession
                {
                    Id = remoteSessionsCounter,
                    State = RemoteSessionState.NotConnected,
                    ServerName = loginHostName,
                    ServerAddress = loginServer,
                    UserDomain = loginDomain,
                    UserName = loginUser,
                    UserPassword = loginPassword,
                    ClientWidth = int.Parse(width.Value),
                    ClientHeight = int.Parse(height.Value),
                    StartProgram = startProgram,
                    AllowRemoteClipboard = allowRemoteClipboard,
                    SecurityProtocol = loginProtocol,
                    AllowSessionSharing = allowSessionSharing,
                    OwnerSessionID = Session.SessionID,
                    HostType = (HostTypeEnum)Enum.Parse(typeof(HostTypeEnum), loginHostType)
                };

                // bind the remote session to the current http session
                Session[HttpSessionStateVariables.RemoteSession.ToString()] = RemoteSession;

                // update the remote sessions auto-increment counter
                Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = remoteSessionsCounter;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);
                RemoteSession = null;
            }
            finally
            {
                Application.UnLock();
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
                    // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the rdp client through the inputs pipe
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    RemoteSession.Manager.Client.StartProcess(
                        RemoteSession.Id,
                        RemoteSession.ServerAddress,
                        RemoteSession.UserDomain,
                        RemoteSession.UserName,
                        RemoteSession.StartProgram,
                        RemoteSession.ClientWidth,
                        RemoteSession.ClientHeight,
                        RemoteSession.AllowRemoteClipboard,
                        RemoteSession.SecurityProtocol,
                        RemoteSession.HostType);
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to connect the remote session {0} ({1})", RemoteSession.Id, exc);
                    connectError.InnerText = "Failed to connect! ensure myrtille services are running";
                    return false;
                }
            }
            else
            {
                connectError.InnerText = "Failed to create remote session!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// disconnect the rdp server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DisconnectButtonClick(
            object sender,
            EventArgs e)
        {
            if (!_authorizedRequest)
                return;

            // disconnect the active remote session, if any and connecting/connected
            if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                try
                {
                    // prevent the remote session from being disconnected by a guest
                    if (Session.SessionID.Equals(RemoteSession.OwnerSessionID))
                    {
                        RemoteSession.State = RemoteSessionState.Disconnecting;
                        RemoteSession.Manager.SendCommand(RemoteSessionCommand.CloseRdpClient);
                    }
                    else
                    {
                        Session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                        RemoteSession = null;
                    }

                    // logout the enterprise session if single use only
                    if (_enterpriseSession != null && _enterpriseSession.SingleUseConnection)
                    {
                        LogoutButtonClick(this, EventArgs.Empty);
                    }

                    // enterprise mode: redirect to the hosts list
                    // standard mode: redirect to the login screen
                    Response.Redirect("~/", true);
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to disconnect the remote session {0} ({1})", RemoteSession.Id, exc);
                }
            }
        }

        #region enterprise mode

        /// <summary>
        /// create an enterprise session from a one time url
        /// </summary>
        private void CreateEnterpriseSessionFromUrl()
        {
            try
            {
                // create enterprise session from querystring params
                _enterpriseSession = new EnterpriseSession
                {
                    IsAdmin = false,    // simple host connection only (no hosts management)
                    SessionID = Request["SI"],
                    SessionKey = Request["SK"],
                    SingleUseConnection = true
                };

                // bind the enterprise session to the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = _enterpriseSession;

                // session fixation protection
                if (IsCookielessSession())
                {
                    // generate a new http session id
                    HttpSessionHelper.RegenerateSessionId();
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create enterprise session from url ({0})", exc);
            }
        }

        /// <summary>
        /// authenticate the user against the enterprise active directory and list the servers available to the user
        /// </summary>
        private void CreateEnterpriseSessionFromLogin()
        {
            try
            {
                // authenticate the user against the enterprise active directory
                _enterpriseSession = _enterpriseClient.Authenticate(user.Value, password.Value);

                if (_enterpriseSession.AuthenticationErrorCode != EnterpriseAuthenticationErrorCode.NONE)
                {
                    if (_enterpriseSession.AuthenticationErrorCode == EnterpriseAuthenticationErrorCode.PASSWORD_EXPIRED)
                    {
                        ClientScript.RegisterClientScriptBlock(GetType(), Guid.NewGuid().ToString(), string.Format("openPopup('changePasswordPopup', 'EnterpriseChangePassword.aspx?userName={0}');", user.Value), true);
                    }
                    else
                    {
                        connectError.InnerText = EnterpriseAuthenticationErrorHelper.GetErrorDescription(_enterpriseSession.AuthenticationErrorCode);
                    }
                    UpdateControls();
                    return;
                }

                // bind the enterprise session to the current http session
                Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = _enterpriseSession;

                // session fixation protection
                if (IsCookielessSession())
                {
                    // generate a new http session id
                    HttpSessionHelper.RegenerateSessionId();
                }

                // redirect to the hosts list
                Response.Redirect("~/", true);
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create enterprise session from login ({0})", exc);
            }
        }

        /// <summary>
        /// populate the enterprise session hosts list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void hostsList_ItemDataBound(
            object sender,
            RepeaterItemEventArgs e)
        {
            try
            {
                var host = e.Item.DataItem as EnterpriseHost;

                if (host.PromptForCredentials)
                {
                    var hostLink = e.Item.FindControl("hostLink") as HtmlAnchor;
                    hostLink.HRef = null;
                    hostLink.Attributes["onclick"] = string.Format("openPopup('editCredentialPopup', 'CredentialsPrompt.aspx?hostId={0}');", host.HostID);
                    hostLink.Attributes["class"] = "hostLink";
                }
                else
                {
                    var hostLink = e.Item.FindControl("hostLink") as HtmlAnchor;
                    hostLink.HRef = string.Format("?SD={0}&__EVENTTARGET=&__EVENTARGUMENT=&connect=Connect%21", host.HostID);
                    hostLink.Attributes["class"] = "hostLink";
                }

                var hostName = e.Item.FindControl("hostName") as HtmlGenericControl;
                hostName.InnerText = (_enterpriseSession.IsAdmin ? "Edit " : "") + host.HostName;
                if (_enterpriseSession.IsAdmin)
                {
                    hostName.Attributes["class"] = "hostName";
                    hostName.Attributes["onclick"] = string.Format("openPopup('editHostPopup', 'EditHost.aspx?hostId={0}');", host.HostID);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to populate hosts for the enterprise session {0} ({1})", _enterpriseSession.SessionID, exc);
            }
        }

        /// <summary>
        /// logout the enterprise session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void LogoutButtonClick(
            object sender,
            EventArgs e)
        {
            if (!_authorizedRequest)
                return;

            if (_enterpriseSession == null)
                return;

            try
            {
                // logout the enterprise session
                _enterpriseClient.Logout(_enterpriseSession.SessionID);
                Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = null;
                _enterpriseSession = null;

                // redirect to the login screen
                Response.Redirect("~/", true);
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to logout the enterprise session {0} ({1})", _enterpriseSession.SessionID, exc);
            }
        }

        #endregion

        #region session sharing

        /// <summary>
        /// retrieve a shared remote session
        /// </summary>
        /// <param name="guestGuid">guest guid</param>
        /// <returns></returns>
        private RemoteSession GetSharedRemoteSession(
            string guestGuid)
        {
            RemoteSession remoteSession = null;

            try
            {
                Application.Lock();

                var sharedSessions = (IDictionary<string, RemoteSession>)Application[HttpApplicationStateVariables.SharedRemoteSessions.ToString()];
                if (sharedSessions.ContainsKey(guestGuid))
                {
                    remoteSession = sharedSessions[guestGuid];
                    sharedSessions.Remove(guestGuid);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the shared remote session ({0})", exc);
            }
            finally
            {
                Application.UnLock();
            }

            return remoteSession;
        }

        #endregion

        #region client ip tracking

        private bool ClientIPTracking()
        {
            bool clientIPTracking;
            if (!bool.TryParse(ConfigurationManager.AppSettings["clientIPTracking"], out clientIPTracking))
            {
                clientIPTracking = false;
            }
            return clientIPTracking;
        }

        #endregion

        #region cookieless session

        private bool IsCookielessSession()
        {
            var sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            return sessionStateSection.Cookieless == HttpCookieMode.UseUri;
        }

        #endregion
    }
}