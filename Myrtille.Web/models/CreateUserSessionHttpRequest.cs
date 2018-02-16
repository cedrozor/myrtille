using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class CreateUserSessionHttpRequest
    {
        public string SessionID { get; set; }
        public long HostID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}