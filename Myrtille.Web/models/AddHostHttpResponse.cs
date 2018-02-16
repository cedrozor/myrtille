using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class AddHostHttpResponse : StandardHttpResponse
    {
        public long? ServerId { get; set; }
        public string ServerName { get; set; }
    }

}