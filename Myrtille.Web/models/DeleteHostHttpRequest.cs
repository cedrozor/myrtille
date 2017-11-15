using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class DeleteHostHttpRequest
    {
        public long HostID { get; set; }
        public string SessionID { get; set; }
    }

}