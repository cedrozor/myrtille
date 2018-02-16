using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class DeleteHostHttpResponse : StandardHttpResponse
    {
        public long HostID { get; set; }
    }

}