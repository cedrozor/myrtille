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

using System.ServiceModel;
using Myrtille.Common;
namespace Myrtille.Services.Contracts
{
    [ServiceContract(CallbackContract = typeof(IRemoteSessionProcessCallback))]
    public interface IRemoteSessionProcess
    {
        /// <summary>
        /// start the rdp client process
        /// </summary>
        [OperationContract]
        void StartProcess(
            int remoteSessionId,
            int clientWidth,
            int clientHeight
            ,SecurityProtocolEnum protocol);

        /// <summary>
        /// stop the rdp client process
        /// CAUTION! in order to close the rdp session, killing the client process is a bit harsh...
        /// better ask it to exit normally, from the remote session manager, using a close rdp command
        /// </summary>
        [OperationContract]
        void StopProcess();
    }

    public interface IRemoteSessionProcessCallback
    {
        /// <summary>
        /// process exited callback
        /// </summary>
        [OperationContract]
        void ProcessExited();
    }
}