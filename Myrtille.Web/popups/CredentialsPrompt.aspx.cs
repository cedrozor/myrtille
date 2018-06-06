using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;
using Myrtille.Services.Contracts;
namespace Myrtille.Web.popups
{
    public partial class CredentialsPrompt : System.Web.UI.Page
    {
        private EnterpriseServiceClient _enterpriseClient;
        private EnterpriseSession _enterpriseSession;
        private long _hostId = 0;

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
                    if (Request["hostId"] != null)
                    {
                        if (!long.TryParse(Request["hostId"], out _hostId))
                        {
                            hostID.Value = _hostId.ToString();
                        }
                    }

                    if(Request["edit"] != null)
                    {
                        hostID.Value = _hostId.ToString();
                    }
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to load credentials prompt ({0})", exc);
            }
        }

        /// <summary>
        /// create session host credentials and connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ConnectButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || _hostId == 0)
                return;

            try
            {
                var enterpriseCredentials = new EnterpriseHostSessionCredentials
                {
                    HostID = _hostId,
                    SessionID = _enterpriseSession.SessionID,
                    SessionKey = _enterpriseSession.SessionKey,
                    Username = promptUserName.Value,
                    Password = promptPassword.Value
                };

                if (!_enterpriseClient.AddSessionHostCredentials(enterpriseCredentials))
                {
                    throw new Exception("Failed to add session host credentials");
                }


                // connect to remote host
                Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "edit=success",false);
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create host session credentials ({0})", exc);
            }
        }
    }
}