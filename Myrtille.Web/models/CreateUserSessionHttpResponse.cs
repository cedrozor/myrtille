using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class CreateUserSessionHttpResponse :StandardHttpResponse
    {
        public string SessionURL { get; set; }
    }
}