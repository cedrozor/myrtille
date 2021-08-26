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
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public enum BrowserResize
    {
        Scale = 0,
        Reconnect = 1,  // default
        None = 2
    }

    public class RemoteSession
    {
        public RemoteSessionManager Manager { get; private set; }

        public Guid Id;
        public RemoteSessionState State;
        public string HostName;
        public HostType HostType;                       // RDP or SSH
        public SecurityProtocol SecurityProtocol;
        public string ServerAddress;                    // :port, if specified
        public string VMGuid;                           // RDP over VM bus (Hyper-V)
        public string VMAddress;                        // RDP over VM bus (Hyper-V)
        public bool VMEnhancedMode;                     // RDP over VM bus (Hyper-V)
        public string UserDomain;
        public string UserName;
        public string UserPassword;
        public int ClientWidth;
        public int ClientHeight;
        public BrowserResize? BrowserResize;            // provided by the client
        public ImageEncoding? ImageEncoding;            // provided by the client
        public int? ImageQuality;                       // provided by the client
        public int? ImageQuantity;                      // provided by the client
        public AudioFormat? AudioFormat;                // provided by the client
        public int? AudioBitrate;                       // provided by the client
        public int ScreenshotIntervalSecs;              // capture API
        public CaptureFormat ScreenshotFormat;          // capture API
        public string ScreenshotPath;                   // capture API
        public string StartProgram;
        public bool AllowRemoteClipboard;               // set in web config + connection service
        public bool AllowFileTransfer;                  // set in web config + connection service
        public bool AllowPrintDownload;                 // set in web config + connection service
        public bool AllowSessionSharing;                // set in web config + connection service
        public bool AllowAudioPlayback;                 // set in web config + connection service
        public int ActiveGuests;                        // number of connected guests
        public int MaxActiveGuests;                     // maximum number of connected guests (0 to disable session sharing)
        public string OwnerSessionID;                   // the http session on which the remote session is bound to
        public string OwnerClientKey;                   // if the http session is shared between different clients, allows to identify the original owner
        public int ExitCode;
        public bool Reconnect;
        public bool ConnectionService;
        public string ClipboardText;                    // clipboard text
        public string LoadBalanceInfo;

        public RemoteSession(
            Guid id,
            string hostName,
            HostType hostType,
            SecurityProtocol securityProtocol,
            string serverAddress,
            string vmGuid,
            string vmAddress,
            bool vmEnhancedMode,
            string userDomain,
            string userName,
            string userPassword,
            int clientWidth,
            int clientHeight,
            string startProgram,
            bool allowRemoteClipboard,
            bool allowFileTransfer,
            bool allowPrintDownload,
            bool allowSessionSharing,
            bool allowAudioPlayback,
            int maxActiveGuests,
            string ownerSessionID,
            string ownerClientKey,
            bool connectionService,
            string loadBalanceInfoValue)
        {
            Id = id;
            State = RemoteSessionState.NotConnected;
            HostName = hostName;
            HostType = hostType;
            SecurityProtocol = securityProtocol;
            ServerAddress = serverAddress;
            VMGuid = vmGuid;
            VMAddress = vmAddress;
            VMEnhancedMode = vmEnhancedMode;
            UserDomain = userDomain;
            UserName = userName;
            UserPassword = userPassword;
            ClientWidth = clientWidth < 100 ? 100 : clientWidth;
            ClientHeight = clientHeight < 100 ? 100 : clientHeight;
            StartProgram = startProgram;
            AllowRemoteClipboard = allowRemoteClipboard;
            AllowFileTransfer = allowFileTransfer;
            AllowPrintDownload = allowPrintDownload;
            AllowSessionSharing = allowSessionSharing;
            AllowAudioPlayback = allowAudioPlayback;
            ActiveGuests = 0;
            MaxActiveGuests = maxActiveGuests;
            OwnerSessionID = ownerSessionID;
            OwnerClientKey = ownerClientKey;
            ConnectionService = connectionService;
            LoadBalanceInfo = loadBalanceInfoValue;

            // default capture API config
            ScreenshotIntervalSecs = 60;
            ScreenshotFormat = CaptureFormat.PNG;
            ScreenshotPath = string.Empty;

            Manager = new RemoteSessionManager(this);
        }
    }
}