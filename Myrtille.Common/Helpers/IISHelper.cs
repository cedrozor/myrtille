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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;
using Microsoft.Win32;

namespace Myrtille.Helpers
{
    public static class IISHelper
    {
        /// <summary>
        /// retrieve the IIS version from registry
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns>version</returns>
        private static Version GetIISVersionFromRegistry(
            string keyName)
        {
            using (var registryKey = Registry.LocalMachine.OpenSubKey(keyName, false))
            {
                if (registryKey != null)
                {
                    var majorVersion = (int)registryKey.GetValue("MajorVersion", -1);
                    var minorVersion = (int)registryKey.GetValue("MinorVersion", -1);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// retrieve the local IIS version
        /// </summary>
        /// <returns>version</returns>
        public static Version GetIISVersion()
        {
            Trace.TraceInformation("Checking version of IIS on the local machine");

            // IIS 6.0 and 7.0+
            var version = GetIISVersionFromRegistry(@"SYSTEM\CurrentControlSet\Services\W3SVC\Parameters") ??
                          GetIISVersionFromRegistry(@"SOFTWARE\Microsoft\InetStp");

            if (version == null)
            {
                Trace.TraceWarning("Unknown IIS version");
                return new Version(0, 0);
            }

            return version;
        }

        #region IIS 7.0+

        #region application pool

        /// <summary>
        /// check existence of a IIS application pool
        /// </summary>
        /// <param name="poolName"></param>
        /// <returns>exists or not</returns>
        public static bool IsIISApplicationPoolExists(
            string poolName)
        {
            Trace.TraceInformation("Checking existence of IIS application pool {0}", poolName);

            bool exists;

            try
            {
                var serverManager = new ServerManager();
                var pool = serverManager.ApplicationPools[poolName];
                exists = pool != null;
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to check existence of IIS application pool {0} ({1})", poolName, exc);
                throw;
            }

            return exists;
        }

        /// <summary>
        /// create a IIS application pool
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="version"></param>
        /// <param name="enable32BitAppOnWin64"></param>
        /// <param name="loadUserProfile">true for IIS > 6</param>
        public static void CreateIISApplicationPool(
            string poolName,
            string version,
            bool enable32BitAppOnWin64 = false,
            bool loadUserProfile = true)
        {
            Trace.TraceInformation("Creating IIS application pool {0}, .NET framework {1}", poolName, version);

            try
            {
                // by default, the new pool properties are inherited from DefaultAppPool
                // the new pool runtime can be set to a specific .net framework version (i.e.: "v2.0", "v4.0", etc...)

                var serverManager = new ServerManager();
                var pool = serverManager.ApplicationPools.Add(poolName);
                pool.ManagedRuntimeVersion = version;
                pool.Enable32BitAppOnWin64 = enable32BitAppOnWin64;
                pool.ProcessModel.LoadUserProfile = loadUserProfile;
                serverManager.CommitChanges();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create IIS application pool {0} ({1})", poolName, exc);
                throw;
            }
        }

        /// <summary>
        /// delete a IIS application pool
        /// </summary>
        /// <param name="poolName"></param>
        public static void DeleteIISApplicationPool(
            string poolName)
        {
            Trace.TraceInformation("Deleting IIS application pool {0}", poolName);

            try
            {
                var serverManager = new ServerManager();
                var pool = serverManager.ApplicationPools[poolName];
                if (pool != null)
                {
                    try
                    {
                        serverManager.ApplicationPools.Remove(pool);
                        serverManager.CommitChanges();
                    }
                    // this exception is thrown if the application pool still have application(s) bound on it
                    catch (COMException)
                    {
                        throw new Exception("The application pool still have application(s) bound on it, please remove them first");
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete IIS application pool {0} ({1})", poolName, exc);
                throw;
            }
        }

        #endregion

        // the below methods use the default web site; extend if needed

        #region application

        /// <summary>
        /// check existence of a IIS application
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns>exists or not</returns>
        public static bool IsIISApplicationExists(
            string applicationName)
        {
            Trace.TraceInformation("Checking existence of IIS application {0}", applicationName);

            bool exists;

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications[applicationName];
                exists = application != null;
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to check existence of IIS application {0} ({1})", applicationName, exc);
                throw;
            }

            return exists;
        }

        /// <summary>
        /// create a IIS application
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="applicationPath"></param>
        /// <param name="applicationPoolName"></param>
        public static void CreateIISApplication(
            string applicationName,
            string applicationPath,
            string applicationPoolName)
        {
            Trace.TraceInformation("Creating IIS application {0} on path {1} for pool {2}", applicationName, applicationPath, applicationPoolName);

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications.Add(applicationName, applicationPath);
                application.ApplicationPoolName = applicationPoolName;
                serverManager.CommitChanges();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create IIS application {0} ({1})", applicationName, exc);
                throw;
            }
        }

        /// <summary>
        /// delete a IIS application
        /// </summary>
        /// <param name="applicationName"></param>
        public static void DeleteIISApplication(
            string applicationName)
        {
            Trace.TraceInformation("Deleting IIS application {0}", applicationName);

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications[applicationName];
                if (application != null)
                {
                    serverManager.Sites[0].Applications.Remove(application);
                    serverManager.CommitChanges();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete IIS application {0} ({1})", applicationName, exc);
                throw;
            }
        }

        #endregion

        #region virtual directory

        /// <summary>
        /// check existence of a IIS virtual directory
        /// </summary>
        /// <param name="virtualDirectoryName"></param>
        /// <param name="applicationName"></param>
        /// <returns>exists or not</returns>
        public static bool IsIISVirtualDirectoryExists(
            string virtualDirectoryName,
            string applicationName)
        {
            Trace.TraceInformation("Checking existence of IIS virtual directory {0} for application {1}", virtualDirectoryName, applicationName);

            var exists = false;

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications[applicationName];
                if (application != null && application.VirtualDirectories.Count > 0)
                {
                    exists = application.VirtualDirectories[virtualDirectoryName] != null;
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to check existence of IIS virtual directory {0} ({1})", virtualDirectoryName, exc);
                throw;
            }

            return exists;
        }

        /// <summary>
        /// create a IIS virtual directory
        /// </summary>
        /// <param name="virtualDirectoryName"></param>
        /// <param name="virtualDirectoryPath"></param>
        /// <param name="applicationName"></param>
        public static void CreateIISVirtualDirectory(
            string virtualDirectoryName,
            string virtualDirectoryPath,
            string applicationName)
        {
            Trace.TraceInformation("Creating IIS virtual directory {0} on path {1} for application {2}", virtualDirectoryName, virtualDirectoryPath, applicationName);

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications[applicationName];
                if (application != null && application.VirtualDirectories[virtualDirectoryName] == null)
                {
                    application.VirtualDirectories.Add(virtualDirectoryName, virtualDirectoryPath);
                    serverManager.CommitChanges();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create IIS virtual directory {0} ({1})", virtualDirectoryName, exc);
                throw;
            }
        }

        /// <summary>
        /// delete a IIS virtual directory
        /// </summary>
        /// <param name="virtualDirectoryName"></param>
        /// <param name="applicationName"></param>
        public static void DeleteIISVirtualDirectory(
            string virtualDirectoryName,
            string applicationName)
        {
            Trace.TraceInformation("Deleting IIS virtual directory {0} for application {1}", virtualDirectoryName, applicationName);

            try
            {
                var serverManager = new ServerManager();
                var application = serverManager.Sites[0].Applications[applicationName];
                if (application != null && application.VirtualDirectories[virtualDirectoryName] != null)
                {
                    var virtualDirectory = application.VirtualDirectories[virtualDirectoryName];
                    application.VirtualDirectories.Remove(virtualDirectory);
                    serverManager.CommitChanges();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete IIS virtual directory {0} ({1})", virtualDirectoryName, exc);
                throw;
            }
        }

        #endregion

        #region SSL certificate

        public static void BindCertificate(
            X509Certificate2 certificate)
        {
            try
            {
                // if not exists already, add an https binding and link it to the certificate
                var serverManager = new ServerManager();
                var binding = serverManager.Sites[0].Bindings.FirstOrDefault(b => b.Protocol == "https");
                if (binding == null)
                {
                    binding = serverManager.Sites[0].Bindings.Add("*:443:", certificate.GetCertHash(), "");
                    binding.Protocol = "https";
                    serverManager.CommitChanges();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to bind certificate to the default website ({0})", exc);
                throw;
            }
        }

        public static void UnbindCertificate(
            X509Certificate2 certificate)
        {
            try
            {
                // if exists and linked to the certificate, remove the https binding
                var serverManager = new ServerManager();
                var binding = serverManager.Sites[0].Bindings.FirstOrDefault(b => b.Protocol == "https");
                if (binding != null && Convert.ToBase64String(binding.CertificateHash) == Convert.ToBase64String(certificate.GetCertHash()))
                {
                    serverManager.Sites[0].Bindings.Remove(binding);
                    serverManager.CommitChanges();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to unbind certificate from the default website ({0})", exc);
                throw;
            }
        }

        #endregion

        #endregion
    }
}