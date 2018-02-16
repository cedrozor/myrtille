using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myrtille.Common.Models
{
    public class EnterpriseSession
    {
        public bool IsAdmin { get; set; }
        public string SessionID { get; set; }
        public string SessionKey { get; set; }
    }
}
