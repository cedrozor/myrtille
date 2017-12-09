﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myrtille.Common.Models
{
    public class EnterpriseHost
    {
        public long HostID { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public SecurityProtocolEnum Protocol { get; set; }
    }
}