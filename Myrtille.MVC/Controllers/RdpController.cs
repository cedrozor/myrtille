using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Web.Mvc;
using Myrtille.Mvc.Models;
using Myrtille.Web;

namespace Myrtille.Mvc.Controllers
{
    [OutputCache(VaryByParam = "*", Duration = 0, NoStore = true)]
    public class RdpController : Controller
    {
        public ActionResult Index()
        {
            var model = new DefaultModel();

            var remoteSessionManager = GetRemoteSessionManager();

            var defaultFormData = (DefaultFormData)Session[nameof(DefaultFormData)];
            if (defaultFormData == null)
            {
                defaultFormData = new DefaultFormData
                {
                    Debug = "",
                    Domain = "",
                    Height = "",
                    Password = "",
                    Server = "localhost",
                    Stat = "",
                    User = "myrtille",
                    Width = ""
                };
                Session[nameof(DefaultFormData)] = defaultFormData;
            }

            model.DefaultFormData = defaultFormData;


            var startMyrtilleParams = (StartMyrtilleParams)Session[nameof(StartMyrtilleParams)];
            if (startMyrtilleParams == null)
            {
                startMyrtilleParams = new StartMyrtilleParams
                {
                    HttpSessionId = Session.SessionID,

                    StatEnabled = false,
                    DebugEnabled = false,

                    RemoteSessionActive = false,
                    WebSocketPort = int.Parse(ConfigurationManager.AppSettings[HttpApplicationStateVariables.WebSocketServerPort.ToString()]),
                    WebSocketPortSecured = string.IsNullOrEmpty(ConfigurationManager.AppSettings[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()])? 0 : int.Parse(ConfigurationManager.AppSettings[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()])
                };
                Session[nameof(StartMyrtilleParams)] = startMyrtilleParams;
            }
            model.StartMyrtilleParams = startMyrtilleParams;


            if (remoteSessionManager != null)
            {
                model.StartMyrtilleParams.RemoteSessionActive = remoteSessionManager.RemoteSession.State ==
                                                                 RemoteSessionState.Connecting ||
                                                                 remoteSessionManager.RemoteSession.State ==
                                                                 RemoteSessionState.Connected;
            }

            return View(model);
        }

        public ActionResult Connect(DefaultFormData defaultFormData, string command)
        {
            Trace.WriteLine($"Server = {defaultFormData.Server}, User = {defaultFormData.User}, command = {command}");

            if (command == "Connect!")
            {
                Session[nameof(DefaultFormData)] = defaultFormData;
                HandleConnect(defaultFormData);
            }
            else if (command == "Disconnect")
            {
                HandleDisconnect();
            }
            else
            {
                throw new Exception("Unknown command!");
            }


            return Redirect("~/");
        }

        public ActionResult VirtualKeyboard()
        {
            return View();
        }

        public ActionResult FileStorage()
        {
            return View();
        }

        private RemoteSessionManager GetRemoteSessionManager()
        {
            RemoteSessionManager remoteSessionManager = null;
            try
            {
                // retrieve the active remote session, if any
                if (System.Web.HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] != null)
                {
                    try
                    {
                        remoteSessionManager =
                            (RemoteSessionManager)
                                System.Web.HttpContext.Current.Session[
                                    HttpSessionStateVariables.RemoteSessionManager.ToString()];


                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve remote session manager ({0})", exc);
                    }
                }

            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to load myrtille ({0})", exc);
            }
            return remoteSessionManager;
        }

        private void HandleDisconnect()
        {
            var remoteSessionManager = GetRemoteSessionManager();

            // disconnect the active remote session, if any and connected
            if (remoteSessionManager != null && (remoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || remoteSessionManager.RemoteSession.State == RemoteSessionState.Connected))
            {
                try
                {
                    // update the remote session state
                    remoteSessionManager.RemoteSession.State = RemoteSessionState.Disconnecting;

                    // send a disconnect command to the rdp client
                    remoteSessionManager.SendCommand(RemoteSessionCommand.CloseRdpClient);

                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to disconnect remote session {0} ({1})", remoteSessionManager.RemoteSession.Id, exc);
                }
            }
        }

        private void HandleConnect(DefaultFormData defaultFormData)
        {
            var remoteSessionManager = GetRemoteSessionManager();

            var startMyrtilleParams = (StartMyrtilleParams)Session[nameof(StartMyrtilleParams)];
            startMyrtilleParams.StatEnabled = defaultFormData.Stat == "Stat enabled";
            startMyrtilleParams.DebugEnabled = defaultFormData.Debug == "Debug enabled";

            // remove a previously disconnected remote session
            if (remoteSessionManager != null && remoteSessionManager.RemoteSession.State == RemoteSessionState.Disconnected)
            {
                try
                {
                    HttpContext.Application.Lock();

                    // unset the remote session manager for the current http session
                    Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] = null;

                    // unregister it at application level; used when there is no http context (i.e.: websockets)
                    var remoteSessionsManagers = (Dictionary<string, RemoteSessionManager>)HttpContext.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()];
                    if (remoteSessionsManagers.ContainsKey(Session.SessionID))
                    {
                        remoteSessionsManagers.Remove(Session.SessionID);
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to remove remote session ({0})", exc);
                }
                finally
                {
                    remoteSessionManager = null;
                    HttpContext.Application.UnLock();
                }
            }

            // create a new remote session, if none active
            if (remoteSessionManager == null)
            {
                try
                {
                    HttpContext.Application.Lock();

                    // auto-increment the remote sessions counter
                    // note that it doesn't really count the active remote sessions... it's just an auto-increment for the remote session id, ensuring it's unique...
                    // the active remote sessions are registered in HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()]; count can be retrieved from there
                    var remoteSessionsCounter = (int)HttpContext.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()];
                    remoteSessionsCounter++;

                    // create the remote session manager
                    remoteSessionManager = new RemoteSessionManager(
                        new RemoteSession
                        {
                            Id = remoteSessionsCounter,
                            State = RemoteSessionState.NotConnected,
                            ServerAddress = defaultFormData.Server,
                            UserDomain = defaultFormData.Domain,
                            UserName = defaultFormData.User,
                            UserPassword = defaultFormData.Password,
                            ClientWidth = defaultFormData.Width,
                            ClientHeight = defaultFormData.Height,
                            DebugMode = defaultFormData.Debug == "Debug enabled"
                        }
                    );

                    // set the remote session manager for the current http session
                    Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] = remoteSessionManager;

                    // register it at application level; used when there is no http context (i.e.: websockets)
                    var remoteSessionsManagers = (Dictionary<string, RemoteSessionManager>)HttpContext.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()];
                    remoteSessionsManagers[Session.SessionID] = remoteSessionManager;

                    // update the remote sessions auto-increment counter
                    HttpContext.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = remoteSessionsCounter;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);
                    remoteSessionManager = null;
                }
                finally
                {
                    HttpContext.Application.UnLock();
                }
            }

            // connect it
            if (remoteSessionManager != null && remoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && remoteSessionManager.RemoteSession.State != RemoteSessionState.Connected)
            {
                try
                {
                    // update the remote session state
                    remoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;

                    // create pipes for this web gateway and the rdp client to talk
                    remoteSessionManager.RemoteSessionPipes.CreatePipes();

                    // the rdp client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the rdp session had ended), pipes are released
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    remoteSessionManager.Client.StartProcess(
                        remoteSessionManager.RemoteSession.Id,
                        remoteSessionManager.RemoteSession.ServerAddress,
                        remoteSessionManager.RemoteSession.UserDomain,
                        remoteSessionManager.RemoteSession.UserName,
                        remoteSessionManager.RemoteSession.UserPassword,
                        remoteSessionManager.RemoteSession.ClientWidth,
                        remoteSessionManager.RemoteSession.ClientHeight,
                        remoteSessionManager.RemoteSession.DebugMode);

                    // update controls
                    //UpateControls();
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to connect remote session {0} ({1})", remoteSessionManager.RemoteSession.Id, exc);
                }
            }

        }

    }
}