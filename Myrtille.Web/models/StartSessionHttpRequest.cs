using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class StartSessionHttpRequest : LoginHttpRequest
    {
        public string SessionID { get; set; }
        public string SessionKey { get; set; }
        public string ServerID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ProgramValue { get; set; }
    }

}