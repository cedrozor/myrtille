using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class StartSessionHttpResponse : StandardHttpResponse
    {
        public string ServerAddress { get; set; }
        public AdditionalControls AdditionalControls { get; set; }
        public RemoteSessionDetails RemoteSessionDetails { get; set; }
    }

}