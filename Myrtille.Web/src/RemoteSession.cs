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

namespace Myrtille.Web
{
    public class RemoteSession : RemoteSessionDetails
    {
        public RemoteSessionManager Manager { get; private set; }

        public RemoteSessionDetails RemoteSessionDetails
        {
            get
            {
                return new RemoteSessionDetails
                {
                    ClientHeight = this.ClientHeight,
                    ClientWidth = this.ClientWidth,
                    CompatibilityMode = this.CompatibilityMode,
                    DebugMode = this.DebugMode,
                    Id = this.Id,
                    ImageEncoding = this.ImageEncoding,
                    ImageQuality = this.ImageQuality,
                    ImageQuantity = this.ImageQuantity,
                    Program = this.Program,
                    ScaleDisplay = this.ScaleDisplay,
                    ServerAddress = this.ServerAddress,
                    State = this.State,
                    StatMode = this.StatMode,
                    UserDomain = this.UserDomain,
                    UserName = this.UserName,
                    UserPassword = this.UserPassword,
                    AllowRemoteClipboard = this.AllowRemoteClipboard
                };
            }
        }

        public RemoteSession()
        {
            Manager = new RemoteSessionManager(this);
        }
    }
}