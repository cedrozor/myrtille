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
using System.IO;
using System.Security.AccessControl;

namespace Myrtille.Helpers
{
    public static class PermissionsHelper
    {
        // add permission(s) to the specified folder for the specified account
        public static void AddDirectorySecurity(
            string directoryPath,
            string accountName,
            FileSystemRights rights,
            InheritanceFlags inheritance,
            PropagationFlags propagation,
            AccessControlType controlType)
        {
            try
            {
                var info = new DirectoryInfo(directoryPath);
                var security = info.GetAccessControl();
                security.AddAccessRule(
                    new FileSystemAccessRule(
                        accountName,
                        rights,
                        inheritance,
                        propagation,
                        controlType));
                info.SetAccessControl(security);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to add permission(s) to folder {0} for account {1} ({2})", directoryPath, accountName, exc);
                throw;
            }
        }

        // remove permission(s) from the specified folder for the specified account
        public static void RemoveDirectorySecurity(
            string directoryPath,
            string accountName,
            FileSystemRights rights,
            AccessControlType controlType)
        {
            try
            {
                var info = new DirectoryInfo(directoryPath);
                var security = info.GetAccessControl();
                security.RemoveAccessRule(
                    new FileSystemAccessRule(
                        accountName,
                        rights,
                        controlType));
                info.SetAccessControl(security);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to remove permission(s) from folder {0} for account {1} ({2})", directoryPath, accountName, exc);
                throw;
            }
        }

        // remove inherited permission(s) from the specified folder
        public static void RemoveDirectoryInheritedSecurity(
            string directoryPath)
        {
            try
            {
                var info = new DirectoryInfo(directoryPath);
                var security = info.GetAccessControl();
                security.SetAccessRuleProtection(true, false);
                info.SetAccessControl(security);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to remove inherited permission(s) from folder {0} ({1})", directoryPath, exc);
                throw;
            }
        }
    }
}