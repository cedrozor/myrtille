using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net.Config;
using Myrtille.Fleck;
using Myrtille.Web;

namespace Myrtille.Mvc
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            try
            {
                // logger
                XmlConfigurator.Configure();

                Trace.TraceInformation("starting application");


                // application cache
                Application[HttpApplicationStateVariables.Cache.ToString()] = Context.Cache;

                // remote sessions auto-incremented counter
                Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = 0;

                // remote sessions managers
                Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()] = new Dictionary<string, RemoteSessionManager>();

                // start a websocket server on a standard port; the port must be opened into your firewall (incoming traffic)
                var webSocketServerPort = 8181;
                if (!string.IsNullOrEmpty(WebConfigurationManager.AppSettings["WebSocketServerPort"]))
                {
                    webSocketServerPort = int.Parse(WebConfigurationManager.AppSettings["WebSocketServerPort"]);
                }
                Application[HttpApplicationStateVariables.WebSocketServerPort.ToString()] = webSocketServerPort;
                var server = new WebSocketServer(string.Format("ws://0.0.0.0:{0}", webSocketServerPort));
                new RemoteSessionSocketServer(Application, server);

                // the installer creates a self-signed certificate for myrtille, but you can set your own certificate (if you wish) as follow:
                // - export your SSL certificate in .PFX format, with the private key
                // - save it into the myrtille "ssl" folder with the name "PKCS12Cert.pfx"

                // check existence of a certificate (note that ".pfx" MIME type is blocked for download for the application...)
                if (!File.Exists(Path.Combine(Server.MapPath("~"), "ssl", "PKCS12Cert.pfx")))
                {
                    Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()] = null;
                }
                else
                {
                    // start a websocket server on a secured port; the port must be opened into your firewall (incoming traffic)
                    var webSocketServerPortSecured = 8431;
                    if (!string.IsNullOrEmpty(WebConfigurationManager.AppSettings["WebSocketServerPortSecured"]))
                    {
                        webSocketServerPortSecured = int.Parse(WebConfigurationManager.AppSettings["WebSocketServerPortSecured"]);
                    }
                    Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()] = webSocketServerPortSecured;
                    server = new WebSocketServer(string.Format("wss://0.0.0.0:{0}", webSocketServerPortSecured));

                    server.Certificate = new X509Certificate2(Path.Combine(Server.MapPath("~"), "ssl", "PKCS12Cert.pfx"), "");
                    new RemoteSessionSocketServer(Application, server);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to start Myrtille.Web application ({0})", exc);
                throw;
            }

        }
    }
}
