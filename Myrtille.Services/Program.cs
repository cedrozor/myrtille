/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste

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
using System.ServiceModel;
using System.ServiceProcess;
using log4net.Config;
using System.IO;
using System.Configuration;
using System.Reflection;
using Myrtille.Common.Interfaces;
namespace Myrtille.Services
{
    public class Program : ServiceBase
    {
        private static ServiceHost _remoteSessionProcess;
        private static ServiceHost _localFileStorage;
        private static ServiceHost _mfaAuthentication;
        private static ServiceHost _enterpriseServices;
        public static IMultifactorAuthenticationAdapter _multifactorAdapter;
        public static IEnterpriseAdapter _enterpriseAdapter;
        public static string _adminGroup;
        public static string _enterpriseDomain;

        private static ServiceHost OpenService(Type serviceType)
        {
            ServiceHost serviceHost = null;
            
            try
            {
                serviceHost = new ServiceHost(serviceType);
                serviceHost.Open();

                var description = serviceHost.Description;
                Trace.TraceInformation(string.Format("Service: {0}", description.ConfigurationName));
                foreach (var endpoint in description.Endpoints)
                {
                    Trace.TraceInformation(string.Format("Endpoint: {0}", endpoint.Name));
                    Trace.TraceInformation(string.Format("Address: {0}", endpoint.Address));
                    Trace.TraceInformation(string.Format("Binding: {0}", endpoint.Binding.Name));
                    Trace.TraceInformation(string.Format("Contract: {0}", endpoint.Contract.ConfigurationName));
                    Trace.TraceInformation("");
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to start service ({0})", exc);
                if (serviceHost != null)
                {
                    serviceHost.Abort();
                }
            }

            return serviceHost;
        }

        private static void CloseService(ref ServiceHost serviceHost)
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }

        private static void Main(string[] args)
        {
            //Set the data dir for SQLCE DB to be app dir/data
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            AppDomain.CurrentDomain.SetData("DataDirectory", dataDir);

            // logger
            XmlConfigurator.Configure();

            if (!Environment.UserInteractive)
            {
                Run(new Program());
            }
            else
            {
                LoadMFAAdapter();
                LoadEnterpriseServiceAdapter();
                var consoleTraceListener = new ConsoleTraceListener();
                consoleTraceListener.Filter = new EventTypeFilter(SourceLevels.Information);
                Trace.Listeners.Add(consoleTraceListener);

                _remoteSessionProcess = OpenService(typeof(RemoteSessionProcess));
                _localFileStorage = OpenService(typeof(FileStorage));
                _mfaAuthentication = OpenService(typeof(MFAAuthentication));
                _enterpriseServices = OpenService(typeof(EnterpriseService));
                Console.WriteLine("press any key to exit...");
                Console.ReadKey();

                CloseService(ref _remoteSessionProcess);
                CloseService(ref _localFileStorage);
                CloseService(ref _mfaAuthentication);
                CloseService(ref _enterpriseServices);
            }
        }

        protected override void OnStart(string[] args)
		{
            LoadMFAAdapter();
            LoadEnterpriseServiceAdapter();
            _remoteSessionProcess = OpenService(typeof(RemoteSessionProcess));
            _localFileStorage = OpenService(typeof(FileStorage));
            _mfaAuthentication = OpenService(typeof(MFAAuthentication));
            _enterpriseServices = OpenService(typeof(EnterpriseService));
        }
 
		protected override void OnStop()
		{
            CloseService(ref _remoteSessionProcess);
            CloseService(ref _localFileStorage);
            CloseService(ref _mfaAuthentication);
            CloseService(ref _enterpriseServices);
        }

        private static void LoadMFAAdapter()
        {
            string mfaConfiguration = ConfigurationManager.AppSettings["MFAAuthAdapter"];

            if (mfaConfiguration == null) return;

            string[] mfaAssemblyDetails = mfaConfiguration.Split(',');

            if (mfaAssemblyDetails.Length != 2) throw new FormatException("MFAAuthAdapter configuration is invalid!");

            var assembly = Assembly.Load(mfaAssemblyDetails[1].Trim());

            _multifactorAdapter = (IMultifactorAuthenticationAdapter)assembly.CreateInstance(mfaAssemblyDetails[0]);

            if (_multifactorAdapter == null) throw new InvalidOperationException(String.Format("Unable to create instance of {0}", mfaAssemblyDetails[0]));

        }

        private static void LoadEnterpriseServiceAdapter()
        {
            string enterpriseConfiguration = ConfigurationManager.AppSettings["EnterpriseAdapter"];
            _adminGroup = ConfigurationManager.AppSettings["EnterpriseAdminGroup"];
            _enterpriseDomain = ConfigurationManager.AppSettings["EnterpriseDomain"];

            if (enterpriseConfiguration == null) return;

            if (_enterpriseDomain == null) throw new Exception("EnterpriseDomain has not been configured!");
            if (_adminGroup == null) throw new Exception("EnterpriseAdminGroup has not been configured!");

            string[] assemblyDetails = enterpriseConfiguration.Split(',');

            if (assemblyDetails.Length != 2) throw new FormatException("EnterpriseAdapter configuration is invalid!");

            var assembly = Assembly.Load(assemblyDetails[1].Trim());

            _enterpriseAdapter = (IEnterpriseAdapter)assembly.CreateInstance(assemblyDetails[0]);

            if (_enterpriseAdapter == null) throw new InvalidOperationException(String.Format("Unable to create instance of {0}", assemblyDetails[0]));

            _enterpriseAdapter.Initialise();

        }
    }
}