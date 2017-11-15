using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Enterprise
{
    public class HostAccessGroups
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public long HostID { get; set; }
        [ForeignKey("HostID")]
        public virtual Host Host { get; set; }

        public string AccessGroup { get; set; }
    }
}
