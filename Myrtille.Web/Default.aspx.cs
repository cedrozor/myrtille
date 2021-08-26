/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2021 Cedric Coste

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
using Myrtille.Web.Properties;

namespace Myrtille.Web
{
    public partial class Default : Page
    {
        private MFAAuthenticationClient _mfaAuthClient = new MFAAuthenticationClient();
        private EnterpriseClient _enterpriseClient = new EnterpriseClient();
        private ConnectionClient _connectionClient = new ConnectionClient(Settings.Default.ConnectionServiceUrl);

        private bool _allowRemoteClipboard;
        private bool _allowFileTransfer;
        private bool _allowPrintDownload;
        private bool _allowSessionSharing;
        private bool _allowAudioPlayback;
        private bool _allowShareSessionUrl;
        private bool _clientIPTracking;
        private bool _toolbarEnabled;
        private bool _loginEnabled;
        private string _loginUrl;
        private bool _httpSessionUseUri;

        private bool _authorizedRequest = true;

        private bool _localAdmin;

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
            // remote clipboard
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowRemoteClipboard"], out _allowRemoteClipboard))
            {
                _allowRemoteClipboard = true;
            }

            // file transfer
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowFileTransfer"], out _allowFileTransfer))
            {
                _allowFileTransfer = true;
            }

            // print download
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowPrintDownload"], out _allowPrintDownload))
            {
                _allowPrintDownload = true;
            }

            // session sharing
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowSessionSharing"], out _allowSessionSharing))
            {
                _allowSessionSharing = true;
            }

            // audio playback
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowAudioPlayback"], out _allowAudioPlayback))
            {
                _allowAudioPlayback = true;
            }

            // share session by url (session spoofing protection if disabled)
            if (!bool.TryParse(ConfigurationManager.AppSettings["AllowShareSessionUrl"], out _allowShareSessionUrl))
            {
                _allowShareSessionUrl = true;
            }

            // client ip tracking
            if (!bool.TryParse(ConfigurationManager.AppSettings["ClientIPTracking"], out _clientIPTracking))
            {
                _clientIPTracking = false;
            }

            // toolbar control
            if (!bool.TryParse(ConfigurationManager.AppSettings["ToolbarEnabled"], out _toolbarEnabled))
            {
                _toolbarEnabled = true;
            }

            // connect from a login page or url
            if (!bool.TryParse(ConfigurationManager.AppSettings["LoginEnabled"], out _loginEnabled))
            {
                _loginEnabled = true;
            }

            // if enabled, url of the login page
            if (_loginEnabled)
            {
                _loginUrl = ConfigurationManager.AppSettings["LoginUrl"];
            }

            // cookieless session
            var sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            _httpSessionUseUri = sessionStateSection.Cookieless == HttpCookieMode.UseUri;
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
            if (_clientIPTracking)
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
            if (_httpSessionUseUri)
            {
                if (Request.Cookies[HttpRequestCookies.ClientKey.ToString()] == null)
                {
                    if (Session[HttpSessionStateVariables.ClientKey.ToString()] == null || _allowShareSessionUrl)
                    {
                        var cookie = new HttpCookie(HttpRequestCookies.ClientKey.ToString());
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
                    var clientKey = Request.Cookies[HttpRequestCookies.ClientKey.ToString()].Value;
                    if (Session[HttpSessionStateVariables.ClientKey.ToString()] == null)
                    {
                        Session[HttpSessionStateVariables.ClientKey.ToString()] = clientKey;
                    }
                    else if (!((string)Session[HttpSessionStateVariables.ClientKey.ToString()]).Equals(clientKey) && !_allowShareSessionUrl)
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

                    // if using a connection service, send the connection state
                    if (Session.SessionID.Equals(RemoteSession.OwnerSessionID) && RemoteSession.ConnectionService)
                    {
                        _connectionClient.SetConnectionState(RemoteSession.Id, string.IsNullOrEmpty(RemoteSession.VMAddress) ? RemoteSession.ServerAddress : RemoteSession.VMAddress, GuidHelper.ConvertFromString(RemoteSession.VMGuid), RemoteSession.State);
                    }

                    if (RemoteSession.State == RemoteSessionState.Disconnected)
                    {
                        // if connecting from a login page or url, show any connection failure into a dialog box
                        // otherwise, this is delegated to the connection API used and its related UI
                        if (_loginEnabled)
                        {
                            // handle connection failure
                            var script = string.Format("handleRemoteSessionExit({0});", RemoteSession.ExitCode);

                            // redirect to login page
                            if (!string.IsNullOrEmpty(_loginUrl))
                            {
                                script += string.Format("window.location.href = '{0}';", _loginUrl);
                            }

                            ClientScript.RegisterClientScriptBlock(GetType(), Guid.NewGuid().ToString(), script, true);
                        }

                        // cleanup
                        Session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                        if (Session[HttpSessionStateVariables.GuestInfo.ToString()] != null)
                        {
                            Session[HttpSessionStateVariables.GuestInfo.ToString()] = null;
                        }
                        RemoteSession = null;
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
                }
            }
            // retrieve a shared remote session from url, if any
            else if (!string.IsNullOrEmpty(Request["gid"]))
            {
                var guestId = Guid.Empty;
                if (Guid.TryParse(Request["gid"], out guestId))
                {
                    var sharingInfo = GetSharingInfo(guestId);
                    if (sharingInfo != null)
                    {
                        Session[HttpSessionStateVariables.RemoteSession.ToString()] = sharingInfo.RemoteSession;
                        Session[HttpSessionStateVariables.GuestInfo.ToString()] = sharingInfo.GuestInfo;

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
                }
            }

            if (_httpSessionUseUri)
            {
                // if running myrtille into an iframe, the iframe url is registered (into a cookie) after the remote session is connected
                // this is necessary to prevent a new http session from being generated for the iframe if the page is reloaded, due to the missing http session id into the iframe url (!)
                // multiple iframes (on the same page), like multiple connections/tabs, requires cookieless="UseUri" for sessionState into web.config

                // problem is, there can be many cases where the cookie is not removed after the remote session is disconnected (network issue, server down, etc?)
                // if the page is reloaded, the iframe will use it's previously registered http session... which may not exist anymore or have its active remote session disconnected
                // if that happens, unregister the iframe url (from the cookie) and reload the page; that will provide a new connection identifier to the iframe and reconnect it

                if (!string.IsNullOrEmpty(Request["fid"]) && RemoteSession == null)
                {
                    if (Request.Cookies[Request["fid"]] != null)
                    {
                        // remove the cookie for the given iframe
                        Response.Cookies[Request["fid"]].Expires = DateTime.Now.AddDays(-1);

                        // reload the page
                        ClientScript.RegisterClientScriptBlock(GetType(), Guid.NewGuid().ToString(), "parent.location.href = parent.location.href;", true);
                    }
                }
            }

            // local admin
            if (_enterpriseSession == null && RemoteSession == null && _enterpriseClient.GetMode() == EnterpriseMode.Local && !string.IsNullOrEmpty(Request["mode"]) && Request["mode"].Equals("admin"))
            {
                _localAdmin = true;
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
        /// update the UI
        /// </summary>
        private void UpdateControls()
        {
            // remote session toolbar
            if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                if (_toolbarEnabled)
                {
                    // interacting with the remote session is available to guests with control access, but only the remote session owner should have control on the remote session itself
                    var controlEnabled = Session.SessionID.Equals(RemoteSession.OwnerSessionID) || (Session[HttpSessionStateVariables.GuestInfo.ToString()] != null && ((GuestInfo)Session[HttpSessionStateVariables.GuestInfo.ToString()]).Control);

                    toolbar.Visible = true;
                    toolbarToggle.Visible = true;
                    serverInfo.Value = !string.IsNullOrEmpty(RemoteSession.VMGuid) ? RemoteSession.VMGuid : (!string.IsNullOrEmpty(RemoteSession.HostName) ? RemoteSession.HostName : RemoteSession.ServerAddress);
                    userInfo.Value = !string.IsNullOrEmpty(RemoteSession.VMGuid) || RemoteSession.SecurityProtocol == SecurityProtocol.rdp ? string.Empty : (string.IsNullOrEmpty(RemoteSession.UserDomain) ? RemoteSession.UserName : string.Format("{0}\\{1}", RemoteSession.UserDomain, RemoteSession.UserName));
                    userInfo.Visible = !string.IsNullOrEmpty(userInfo.Value);
                    scale.Value = RemoteSession.BrowserResize == BrowserResize.Scale ? "Scale ON" : "Scale OFF";
                    scale.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID) || RemoteSession.HostType == HostType.SSH;
                    reconnect.Value = RemoteSession.BrowserResize == BrowserResize.Reconnect ? "Reconnect ON" : "Reconnect OFF";
                    reconnect.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID) || RemoteSession.HostType == HostType.SSH;
                    keyboard.Disabled = !controlEnabled || (!string.IsNullOrEmpty(RemoteSession.VMGuid) && !RemoteSession.VMEnhancedMode);
                    osk.Disabled = !controlEnabled || RemoteSession.HostType == HostType.SSH;
                    clipboard.Disabled = !controlEnabled || RemoteSession.HostType == HostType.SSH || !RemoteSession.AllowRemoteClipboard || (!string.IsNullOrEmpty(RemoteSession.VMGuid) && !RemoteSession.VMEnhancedMode);
                    files.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID) || RemoteSession.HostType == HostType.SSH || !RemoteSession.AllowFileTransfer || (RemoteSession.ServerAddress.ToLower() != "localhost" && RemoteSession.ServerAddress != "127.0.0.1" && RemoteSession.ServerAddress != "[::1]" && RemoteSession.ServerAddress != Request.Url.Host && string.IsNullOrEmpty(RemoteSession.UserDomain)) || !string.IsNullOrEmpty(RemoteSession.VMGuid);
                    cad.Disabled = !controlEnabled || RemoteSession.HostType == HostType.SSH;
                    mrc.Disabled = !controlEnabled || RemoteSession.HostType == HostType.SSH;
                    vswipe.Disabled = !controlEnabled || RemoteSession.HostType == HostType.SSH;
                    share.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID) || !RemoteSession.AllowSessionSharing;
                    disconnect.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID);
                    imageQuality.Disabled = !Session.SessionID.Equals(RemoteSession.OwnerSessionID) || RemoteSession.HostType == HostType.SSH;
                }
            }
            // hosts list
            else if (_enterpriseSession != null && _enterpriseSession.AuthenticationErrorCode == EnterpriseAuthenticationErrorCode.NONE)
            {
                hosts.Visible = true;
                enterpriseUserInfo.Value = string.IsNullOrEmpty(_enterpriseSession.Domain) ? _enterpriseSession.UserName : string.Format("{0}\\{1}", _enterpriseSession.Domain, _enterpriseSession.UserName);
                enterpriseUserInfo.Visible = !string.IsNullOrEmpty(enterpriseUserInfo.Value);
                newRDPHost.Visible = _enterpriseSession.IsAdmin;
                newSSHHost.Visible = _enterpriseSession.IsAdmin;
                hostsList.DataSource = _enterpriseClient.GetSessionHosts(_enterpriseSession.SessionID);
                hostsList.DataBind();
            }
            // login screen
            else
            {
                // connection params are sent when the login form is submitted, either through http post (the default form method) or http get (querystring)
                login.Visible = _loginEnabled;

                // MFA
                if (_mfaAuthClient.GetState())
                {
                    mfaDiv.Visible = true;
                    mfaProvider.InnerText = _mfaAuthClient.GetPromptLabel();
                    mfaProvider.HRef = _mfaAuthClient.GetProviderURL();
                }

                // enterprise mode
                if (_enterpriseClient.GetMode() == EnterpriseMode.Domain || _localAdmin)
                {
                    hostConnectDiv.Visible = false;
                    adminDiv.Visible = _localAdmin;
                    if (adminDiv.Visible)
                    {
                        adminText.InnerText = "Home";
                        adminUrl.HRef = "~/";
                    }
                }
                // standard mode
                else
                {
                    connect.Attributes["onclick"] = "initDisplay();";
                    adminDiv.Visible = _enterpriseClient.GetMode() == EnterpriseMode.Local;
                }
            }
        }

        /// <summary>
        /// enterprise mode from url: load the enterprise session (from querystring param) and proceed to connection; the user is non admin and the url is only usable once
        /// enterprise mode from login: authenticate the user against the enterprise active directory and list the servers available to the user; the user is admin if member of the "EnterpriseAdminGroup" defined into myrtille services config
        /// standard mode: connect the specified server; authentication is delegated to the remote server or connection broker (if applicable)
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
            if (_enterpriseSession == null && Request["SI"] != null && Request["SD"] != null && Request["SK"] != null)
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
            if (_enterpriseSession == null && (_enterpriseClient.GetMode() == EnterpriseMode.Domain || _localAdmin))
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
                        if (_httpSessionUseUri)
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
        /// connect the remote server
        /// </summary>
        /// <remarks>
        /// authentication is delegated to the remote server or connection broker (if applicable)
        /// </remarks>
        private bool ConnectRemoteServer()
        {
            // connection parameters
            string loginHostName = null;
            var loginHostType = (HostType)Convert.ToInt32(hostType.Value);
            var loginProtocol = (SecurityProtocol)securityProtocol.SelectedIndex;
            var loginServer = string.IsNullOrEmpty(server.Value) ? "localhost" : server.Value;
            var loginVMGuid = vmGuid.Value;
            var loginVMAddress = string.Empty;
            var loginVMEnhancedMode = vmEnhancedMode.Checked;
            var loginDomain = domain.Value;
            var loginUser = user.Value;
            var loginPassword = string.IsNullOrEmpty(passwordHash.Value) ? password.Value : CryptoHelper.RDP_Decrypt(passwordHash.Value);
            var startProgram = program.Value;
            var loadBalanceInfoValue = loadBalanceInfo.Value;

            // allowed features
            var allowRemoteClipboard = _allowRemoteClipboard;
            var allowFileTransfer = _allowFileTransfer;
            var allowPrintDownload = _allowPrintDownload;
            var allowSessionSharing = _allowSessionSharing;
            var allowAudioPlayback = _allowAudioPlayback;

            // sharing parameters
            int maxActiveGuests = int.MaxValue;

            var connectionId = Guid.NewGuid();

            // connect an host from the hosts list or from a one time session url
            if (_enterpriseSession != null && (!string.IsNullOrEmpty(Request["SD"])))
            {
                long hostId;
                if (!long.TryParse(Request["SD"], out hostId))
                {
                    hostId = 0;
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
                    loginHostType = connection.HostType;
                    loginProtocol = connection.Protocol;
                    loginServer = !string.IsNullOrEmpty(connection.HostAddress) ? connection.HostAddress : connection.HostName;
                    loginVMGuid = connection.VMGuid;
                    loginVMEnhancedMode = connection.VMEnhancedMode;
                    loginDomain = connection.Domain;
                    loginUser = connection.Username;
                    loginPassword = CryptoHelper.RDP_Decrypt(connection.Password);
                    startProgram = connection.StartRemoteProgram;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0} connection details ({1})", hostId, exc);
                    return false;
                }
            }
            // by using a connection service on a backend (connection API), the connection details can be hidden from querystring and mapped to a connection identifier
            else if (!string.IsNullOrEmpty(Request["cid"]))
            {
                if (!Guid.TryParse(Request["cid"], out connectionId))
                {
                    System.Diagnostics.Trace.TraceInformation("Invalid connection id {0}", Request["cid"]);
                    return false;
                }

                try
                {
                    // retrieve the connection details
                    var connection = _connectionClient.GetConnectionInfo(connectionId);
                    if (connection == null)
                    {
                        System.Diagnostics.Trace.TraceInformation("Unable to retrieve connection info {0}", connectionId);
                        return false;
                    }

                    // ensure the user is allowed to connect the host
                    if (!_connectionClient.IsUserAllowedToConnectHost(connection.User.Domain, connection.User.UserName, connection.Host.IPAddress, connection.VM != null ? connection.VM.Guid : Guid.Empty))
                    {
                        System.Diagnostics.Trace.TraceInformation("User: domain={0}, name={1} is not allowed to connect host {2}", connection.User.Domain, connection.User.UserName, connection.Host.IPAddress);
                        return false;
                    }

                    loginHostType = connection.Host.HostType;
                    loginProtocol = connection.Host.SecurityProtocol;
                    loginServer = connection.Host.IPAddress;
                    loginVMGuid = connection.VM != null ? connection.VM.Guid.ToString() : string.Empty;
                    loginVMAddress = connection.VM != null ? connection.VM.IPAddress : string.Empty;
                    loginVMEnhancedMode = connection.VM != null ? connection.VM.EnhancedMode : false;
                    loginDomain = connection.User.Domain;
                    loginUser = connection.User.UserName;
                    loginPassword = connection.User.Password;
                    startProgram = connection.StartProgram;

                    allowRemoteClipboard = allowRemoteClipboard && connection.AllowRemoteClipboard;
                    allowFileTransfer = allowFileTransfer && connection.AllowFileTransfer;
                    allowPrintDownload = allowPrintDownload && connection.AllowPrintDownload;
                    allowSessionSharing = allowSessionSharing && connection.MaxActiveGuests > 0;
                    allowAudioPlayback = allowAudioPlayback && connection.AllowAudioPlayback;

                    maxActiveGuests = connection.MaxActiveGuests;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve connection info {0} ({1})", connectionId, exc);
                    return false;
                }
            }
            // if the connection from login screen or url is disabled, the connection must be done either by using a connection API or from the enterprise mode
            else if (!_loginEnabled)
            {
                return false;
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

                // create the remote session
                RemoteSession = new RemoteSession(
                    connectionId,
                    loginHostName,
                    loginHostType,
                    loginProtocol,
                    loginServer,
                    loginVMGuid,
                    loginVMAddress,
                    loginVMEnhancedMode,
                    !string.IsNullOrEmpty(loginDomain) ? loginDomain : AccountHelper.GetDomain(loginUser, loginPassword),
                    AccountHelper.GetUserName(loginUser),
                    loginPassword,
                    int.Parse(width.Value),
                    int.Parse(height.Value),
                    startProgram,
                    allowRemoteClipboard,
                    allowFileTransfer,
                    allowPrintDownload,
                    allowSessionSharing,
                    allowAudioPlayback,
                    maxActiveGuests,
                    Session.SessionID,
                    (string)Session[HttpSessionStateVariables.ClientKey.ToString()],
                    Request["cid"] != null,
                    loadBalanceInfoValue
                );

                // bind the remote session to the current http session
                Session[HttpSessionStateVariables.RemoteSession.ToString()] = RemoteSession;

                // register the remote session at the application level
                var remoteSessions = (IDictionary<Guid, RemoteSession>)Application[HttpApplicationStateVariables.RemoteSessions.ToString()];
                remoteSessions.Add(RemoteSession.Id, RemoteSession);
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
                RemoteSession.State = RemoteSessionState.Connecting;
            }
            else
            {
                connectError.InnerText = "Failed to create remote session!";
                return false;
            }

            return true;
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
                Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = _enterpriseSession;

                // session fixation protection
                if (_httpSessionUseUri)
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
                // authenticate the user
                _enterpriseSession = _enterpriseClient.Authenticate(user.Value, password.Value);

                if (_enterpriseSession == null || _enterpriseSession.AuthenticationErrorCode != EnterpriseAuthenticationErrorCode.NONE)
                {
                    if (_enterpriseSession == null)
                    {
                        connectError.InnerText = EnterpriseAuthenticationErrorHelper.GetErrorDescription(EnterpriseAuthenticationErrorCode.UNKNOWN_ERROR);
                    }
                    else if (_enterpriseSession.AuthenticationErrorCode == EnterpriseAuthenticationErrorCode.PASSWORD_EXPIRED)
                    {
                        ClientScript.RegisterClientScriptBlock(GetType(), Guid.NewGuid().ToString(), "window.onload = function() { " + string.Format("openPopup('changePasswordPopup', 'EnterpriseChangePassword.aspx?userName={0}" + (_localAdmin ? "&mode=admin" : string.Empty) + "');", user.Value) + " }", true);
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
                if (_httpSessionUseUri)
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

                if (host.PromptForCredentials || string.IsNullOrEmpty(_enterpriseSession.Domain))
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
                hostName.InnerText = (_enterpriseSession.IsAdmin ? "Edit " : string.Empty) + host.HostName;
                if (_enterpriseSession.IsAdmin)
                {
                    hostName.Attributes["class"] = "hostName";
                    hostName.Attributes["title"] = "edit";
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
        /// retrieve a shared remote session information
        /// </summary>
        /// <param name="guestId"></param>
        /// <returns></returns>
        private SharingInfo GetSharingInfo(
            Guid guestId)
        {
            SharingInfo sharingInfo = null;

            try
            {
                Application.Lock();

                var sharedSessions = (IDictionary<Guid, SharingInfo>)Application[HttpApplicationStateVariables.SharedRemoteSessions.ToString()];
                if (!sharedSessions.ContainsKey(guestId))
                {
                    connectError.InnerText = "Invalid sharing link";
                }
                else
                {
                    sharingInfo = sharedSessions[guestId];
                    if (sharingInfo.GuestInfo.Active)
                    {
                        connectError.InnerText = "The sharing link was already used";
                        sharingInfo = null;
                    }
                    else if (sharingInfo.RemoteSession.State != RemoteSessionState.Connected)
                    {
                        connectError.InnerText = "The session is not connected";
                        sharingInfo = null;
                    }
                    else if (sharingInfo.RemoteSession.ActiveGuests >= sharingInfo.RemoteSession.MaxActiveGuests)
                    {
                        connectError.InnerText = "The maximum number of active guests was reached for the session";
                        sharingInfo = null;
                    }
                    else
                    {
                        sharingInfo.HttpSession = Session;
                        sharingInfo.RemoteSession.ActiveGuests++;
                        sharingInfo.GuestInfo.Active = true;
                    }
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the shared remote session for guest {0} ({1})", guestId, exc);
            }
            finally
            {
                Application.UnLock();
            }

            return sharingInfo;
        }

        #endregion
    }
}