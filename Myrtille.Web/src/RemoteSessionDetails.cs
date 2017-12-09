using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Myrtille.Common;
namespace Myrtille.Web
{
    public class RemoteSessionDetails
    {
        public int Id;
        public RemoteSessionState State;
        public string ServerAddress;
        public string UserDomain;
        public string UserName;
        public string UserPassword;
        public int ClientWidth;
        public int ClientHeight;
        public bool ScaleDisplay;
        public ImageEncoding? ImageEncoding;            // provided by the client
        public int? ImageQuality;                       // provided by the client
        public int? ImageQuantity;                      // provided by the client
        public bool StatMode;
        public bool DebugMode;
        public bool CompatibilityMode;
        public string Program;
        public bool AllowRemoteClipboard;               // provided by the client to determine if remote access to clipboard is allowed
        public SecurityProtocolEnum Protocol;
    }
}