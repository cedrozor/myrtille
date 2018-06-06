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
using System.Web.UI;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    public partial class GetUpdate : Page
    {
        /// <summary>
        /// retrieve an image update (region or fullscreen) from the rdp session and send it to the browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // if cookies are enabled, the http session id is added to the http request headers; otherwise, it's added to the http request url
            // in both cases, the given http session is automatically bound to the current http context

            RemoteSession remoteSession = null;

            try
            {
                if (Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the current http session
                remoteSession = (RemoteSession)Session[HttpSessionStateVariables.RemoteSession.ToString()];

                try
                {
                    // retrieve params
                    var imgIdx = int.Parse(Request.QueryString["imgIdx"]);

                    // retrieve image data
                    var img = remoteSession.Manager.GetCachedUpdate(imgIdx);

                    // if the image isn't available (removed from cache?), request a fullscreen update (resync display)
                    if (img == null)
                    {
                        remoteSession.Manager.SendCommand(RemoteSessionCommand.RequestFullscreenUpdate);
                    }

                    var imgData = img != null ? img.Data : null;
                    if (imgData != null && imgData.Length > 0)
                    {
                        // write the output
                        Response.OutputStream.Write(imgData, 0, imgData.Length);
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve display update, remote session {0} ({1})", remoteSession.Id, exc);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
            }
        }
    }
}