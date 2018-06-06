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
using System.Threading;
using System.Web;
using System.Web.UI;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class EditHost : Page
    {
        private EnterpriseServiceClient _enterpriseClient;
        private EnterpriseSession _enterpriseSession;
        private long? _hostId = null;
        private string _hostType = null;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
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
            try
            {
                if (HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] == null)
                    throw new NullReferenceException();

                _enterpriseSession = (EnterpriseSession)HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()];

                try
                {
                    if (!_enterpriseSession.IsAdmin)
                    {
                        Response.Redirect("~/", true);
                    }

                    if (Request["hostType"] != null)
                    {
                        _hostType = Request["hostType"];
                    }
                    else
                    {
                        _hostType = "RDP";
                    }

                    // retrieve the host, if any (create if empty)
                    if (Request["hostId"] != null)
                    {
                        long hostId;
                        if (!long.TryParse(Request["hostId"], out hostId))
                        {
                            hostId = 0;
                        }

                        if (hostId != 0)
                        {
                            _hostId = hostId;

                            if (!IsPostBack && Request["edit"] == null)
                            {
                                try
                                {
                                    var host = _enterpriseClient.GetHost(_hostId.Value, _enterpriseSession.SessionID);
                                    if (host != null)
                                    {
                                        hostName.Value = host.HostName;
                                        hostAddress.Value = host.HostAddress;
                                        groupsAccess.Value = host.DirectoryGroups;
                                        securityProtocol.SelectedIndex = (int)host.Protocol;
                                        _hostType = host.HostType.ToString();
                                        promptCredentials.Checked = host.PromptForCredentials;
                                    }
                                }
                                catch (Exception exc)
                                {
                                    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0}, ({1})", _hostId, exc);
                                }
                            }

                            createSessionUrl.Attributes["onclick"] = string.Format("parent.openPopup('editHostSessionPopup', 'EditHostSession.aspx?hostId={0}');", _hostId);
                        }
                    }
                    else
                    {
                        createSessionUrl.Disabled = true;
                        deleteHost.Disabled = true;
                    }

                    rdpSecurityInput.Visible = (_hostType == "RDP");
                    startProgramInput.Visible = (_hostType == "RDP");
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the active enterprise session ({0})", exc);
            }
        }

        /// <summary>
        /// create or edit a host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SaveHostButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || string.IsNullOrEmpty(hostName.Value))
                return;

            try
            {
                var enterpriseHost = new EnterpriseHostEdit
                {
                    HostID = _hostId ?? 0,
                    HostName = hostName.Value,
                    HostAddress = hostAddress.Value,
                    DirectoryGroups = groupsAccess.Value,
                    Protocol = (SecurityProtocolEnum)securityProtocol.SelectedIndex,
                    HostType = _hostType,
                    StartRemoteProgram = startProgram.Value,
                    PromptForCredentials = promptCredentials.Checked
                };

                if(_hostId != null)
                {
                    _enterpriseClient.UpdateHost(enterpriseHost, _enterpriseSession.SessionID);
                }
                else
                {
                    _enterpriseClient.AddHost(enterpriseHost, _enterpriseSession.SessionID);
                }

                // refresh the hosts list
                Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "edit=success");
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to save host ({0})", exc);
            }
        }

        /// <summary>
        /// delete a host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DeleteHostButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || !_hostId.HasValue)
                return;

            try
            {
                _enterpriseClient.DeleteHost(_hostId.Value, _enterpriseSession.SessionID);

                // refresh the hosts list
                Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "edit=success");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to delete host {0} ({1})", _hostId.Value, exc);
            }
        }
    }
}