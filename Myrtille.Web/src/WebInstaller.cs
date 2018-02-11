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
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    [RunInstaller(true)]
    public class WebInstaller : Installer
    {
        public override void Install(
            IDictionary stateSaver)
        {
            // enable the line below to debug this installer; disable otherwise
            //MessageBox.Show("Attach the .NET debugger to the 'MSI Debug' msiexec.exe process now for debug. Click OK when ready...", "MSI Debug");

            // if the installer is running in repair mode, it will try to re-install Myrtille... which is fine
            // problem is, it won't uninstall it first... which is not fine because some components can't be installed twice!
            // thus, prior to any install, try to uninstall first

            Trace.TraceInformation("Myrtille.Web is being installed, cleaning first");

            try
            {
                Uninstall(null);
            }
            catch (Exception exc)
            {
                Trace.TraceInformation("Failed to clean Myrtille.Web ({0})", exc);
            }

            base.Install(stateSaver);

            Trace.TraceInformation("Installing Myrtille.Web");

            try
            {
                // register Myrtille.Web to local IIS
                if (!IISHelper.IsIISApplicationPoolExists("MyrtilleAppPool"))
                {
                    IISHelper.CreateIISApplicationPool("MyrtilleAppPool", "v4.0");
                }

                if (!IISHelper.IsIISApplicationExists("/Myrtille"))
                {
                    IISHelper.CreateIISApplication("/Myrtille", Path.GetFullPath(Context.Parameters["targetdir"]), "MyrtilleAppPool");
                }

                // create a self signed certificate
                var cert = CertificateHelper.CreateSelfSignedCertificate(Environment.MachineName, "Myrtille self-signed certificate");

                // bind it to the default website
                IISHelper.BindCertificate(cert);

                // add write permission to the targetdir "log" folder for MyrtilleAppPool, so that Myrtille.Web can save logs into it
                PermissionsHelper.AddDirectorySecurity(
                    Path.Combine(Path.GetFullPath(Context.Parameters["targetdir"]), "log"),
                    "IIS AppPool\\MyrtilleAppPool",
                    FileSystemRights.Write,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                Trace.TraceInformation("Installed Myrtille.Web");
            }
            catch (Exception exc)
            {
                Context.LogMessage(exc.InnerException != null ? exc.InnerException.Message: exc.Message);
                Trace.TraceError("Failed to install Myrtille.Web ({0})", exc);
                throw;
            }
        }

        public override void Commit(
            IDictionary savedState)
        {
            base.Commit(savedState);
            // insert code as needed
        }

        public override void Rollback(
            IDictionary savedState)
        {
            base.Rollback(savedState);
            DoUninstall();
        }

        public override void Uninstall(
            IDictionary savedState)
        {
            base.Uninstall(savedState);
            DoUninstall();
        }

        private void DoUninstall()
        {
            // enable the line below to debug this installer; disable otherwise
            //MessageBox.Show("Attach the .NET debugger to the 'MSI Debug' msiexec.exe process now for debug. Click OK when ready...", "MSI Debug");

            Trace.TraceInformation("Uninstalling Myrtille.Web");

            try
            {
                // unregister Myrtille.Web from local IIS
                if (IISHelper.IsIISApplicationExists("/Myrtille"))
                {
                    IISHelper.DeleteIISApplication("/Myrtille");
                }

                if (IISHelper.IsIISApplicationPoolExists("MyrtilleAppPool"))
                {
                    IISHelper.DeleteIISApplicationPool("MyrtilleAppPool");
                }

                // retrieve the myrtille self signed certificate
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                var certs = store.Certificates.Find(X509FindType.FindByIssuerName, Environment.MachineName, false);
                if (certs.Count > 0)
                {
                    foreach (var cert in certs)
                    {
                        if (cert.FriendlyName == "Myrtille self-signed certificate")
                        {
                            // unbind it from the default website
                            IISHelper.UnbindCertificate(cert);

                            // remove it
                            store.Remove(cert);

                            // normally, there should be only one myrtille self-signed certificate, but let's check further (just in case)...
                            //break;
                        }
                    }
                }

                store.Close();

                Trace.TraceInformation("Uninstalled Myrtille.Web");
            }
            catch (Exception exc)
            {
                Context.LogMessage(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
                Trace.TraceError("Failed to uninstall Myrtille.Web ({0})", exc);
                throw;
            }
        }
    }
}