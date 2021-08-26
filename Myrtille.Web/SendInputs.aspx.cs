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
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using Newtonsoft.Json;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class SendInputs : Page
    {
        /// <summary>
        /// send user input(s) (mouse, keyboard) to the remote session
        /// if long-polling is disabled (xhr only), also returns image data within the response
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

                var clientId = Request.QueryString["clientId"];

                if (!remoteSession.Manager.Clients.ContainsKey(clientId))
                {
                    lock (remoteSession.Manager.ClientsLock)
                    {
                        remoteSession.Manager.Clients.Add(clientId, new RemoteSessionClient(clientId));
                    }
                }

                var client = remoteSession.Manager.Clients[clientId];

                // filters out the dummy xhr calls (used with websocket to keep the http session alive)
                if (!string.IsNullOrEmpty(Request.QueryString["data"]))
                {
                    lock (client.Lock)
                    {
                        // register a message queue for the client (now using HTML4)
                        if (client.MessageQueue == null)
                        {
                            client.MessageQueue = new List<RemoteSessionMessage>();
                        }
                    }

                    // update guest information
                    if (!Session.SessionID.Equals(remoteSession.OwnerSessionID))
                    {
                        if (Session[HttpSessionStateVariables.GuestInfo.ToString()] != null)
                        {
                            ((GuestInfo)Session[HttpSessionStateVariables.GuestInfo.ToString()]).Websocket = false;
                        }
                    }
                    // connect the remote server
                    else if (remoteSession.State == RemoteSessionState.Connecting && !remoteSession.Manager.HostClient.ProcessStarted)
                    {
                        try
                        {
                            // create pipes for the web gateway and the host client to talk
                            remoteSession.Manager.Pipes.CreatePipes();

                            // the host client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the remote session had ended), pipes are released
                            // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the host client through the inputs pipe
                            // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                            remoteSession.Manager.HostClient.StartProcess(
                                remoteSession.Id,
                                remoteSession.HostType,
                                remoteSession.SecurityProtocol,
                                remoteSession.ServerAddress,
                                remoteSession.VMGuid,
                                remoteSession.UserDomain,
                                remoteSession.UserName,
                                remoteSession.StartProgram,
                                remoteSession.ClientWidth,
                                remoteSession.ClientHeight,
                                remoteSession.AllowRemoteClipboard,
                                remoteSession.AllowPrintDownload,
                                remoteSession.AllowAudioPlayback,
                                remoteSession.LoadBalanceInfo);
                        }
                        catch (Exception exc)
                        {
                            System.Diagnostics.Trace.TraceError("Failed to connect the remote session {0} ({1})", remoteSession.Id, exc);
                            throw;
                        }
                    }
                }

                try
                {
                    // retrieve params
                    var data = Request.QueryString["data"];
                    var imgIdx = int.Parse(Request.QueryString["imgIdx"]);
                    var latency = int.Parse(Request.QueryString["latency"]);
                    var imgReturn = int.Parse(Request.QueryString["imgReturn"]) == 1;

                    // process input(s)
                    if (!string.IsNullOrEmpty(data))
                    {
                        remoteSession.Manager.ProcessInputs(Session, clientId, data);
                    }

                    client.ImgIdx = imgIdx;
                    client.Latency = latency;

                    // xhr only
                    if (imgReturn)
                    {
                        // concatenate text for terminal output to avoid a slow rendering
                        // if another message type is in the queue, it will be given priority over the terminal
                        // the terminal is refreshed often, so it shouldn't be an issue...
                        var msgText = string.Empty;
                        var msgComplete = false;

                        if (client.MessageQueue != null)
                        {
                            while (client.MessageQueue.Count > 0 && !msgComplete)
                            {
                                var message = client.MessageQueue[0];

                                switch (message.Type)
                                {
                                    case MessageType.TerminalOutput:
                                        msgText += message.Text;
                                        break;

                                    default:
                                        msgText = JsonConvert.SerializeObject(message);
                                        msgComplete = true;
                                        break;
                                }

                                lock (((ICollection)client.MessageQueue).SyncRoot)
                                {
                                    client.MessageQueue.RemoveAt(0);
                                }
                            }
                        }

                        // message
                        if (!string.IsNullOrEmpty(msgText))
                        {
                            if (!msgComplete)
                            {
                                Response.Write(JsonConvert.SerializeObject(new RemoteSessionMessage { Type = MessageType.TerminalOutput, Text = msgText }));
                            }
                            else
                            {
                                Response.Write(msgText);
                            }
                        }
                        // image
                        else
                        {
                            var image = remoteSession.Manager.GetNextUpdate(imgIdx);
                            if (image != null)
                            {
                                System.Diagnostics.Trace.TraceInformation("Returning image {0} ({1}), client {2}, remote session {3}", image.Idx, (image.Fullscreen ? "screen" : "region"), clientId, remoteSession.Id);

                                var imgData =
                                    image.Idx + "," +
                                    image.PosX + "," +
                                    image.PosY + "," +
                                    image.Width + "," +
                                    image.Height + "," +
                                    image.Format.ToString().ToLower() + "," +
                                    image.Quality + "," +
                                    image.Fullscreen.ToString().ToLower() + "," +
                                    Convert.ToBase64String(image.Data);

                                // write the output
                                Response.Write(imgData);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to send user input(s), client {0}, remote session {1} ({2})", clientId, remoteSession.Id, exc);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
            }
        }
    }
}