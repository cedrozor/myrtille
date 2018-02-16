using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Myrtille.Common;
namespace Myrtille.Web
{
    public class AddHostHttpRequest
    {
        public string SessionID { get; set; }
        public long? HostID { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public string DirectoryGroups { get; set; }
        public SecurityProtocolEnum Protocol { get; set; }
    }

}