using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class LoginHttpResponse : StandardHttpResponse
    {
        public string EnterpriseUser { get; set; }
        public string EnterpriseSessionID { get; set; }
        public string EnterpriseSessionKey { get; set; }
        public bool IsAdmin { get; set; }
        public AdditionalControls AdditionalControls { get; set; }
        public IList<ServerConfiguration> ServerList { get; set; }

    }
}