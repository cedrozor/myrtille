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
using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract(CallbackContract = typeof(IRemoteSessionProcessCallback))]
    public interface IRemoteSessionProcess
    {
        /// <summary>
        /// start the host client process
        /// </summary>
        [OperationContract]
        void StartProcess(
            Guid remoteSessionId,
            HostType HostType,
            SecurityProtocol securityProtocol,
            string serverAddress,
            string vmGuid,
            string userDomain,
            string userName,
            string startProgram,
            int clientWidth,
            int clientHeight,
            bool allowRemoteClipboard,
            bool allowPrintDownload,
            bool allowAudioPlayback,
            string loadBalanceInfo);

        /// <summary>
        /// stop the host client process
        /// CAUTION! in order to close the host session, killing the client process is a bit harsh...
        /// better ask it to exit normally, from the remote session manager, using a close command
        /// </summary>
        [OperationContract]
        void StopProcess();

        /// <summary>
        /// retrieve the user account the host client process is running on
        /// </summary>
        [OperationContract]
        string GetProcessIdentity();
    }

    public interface IRemoteSessionProcessCallback
    {
        /// <summary>
        /// process exited callback
        /// </summary>
        [OperationContract]
        void ProcessExited(int exitCode);
    }
}