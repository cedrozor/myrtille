using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class LoginHttpRequest
    {
        public string Server { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string MFAPassword { get; set; }
        public string Program { get; set; }
    }

}