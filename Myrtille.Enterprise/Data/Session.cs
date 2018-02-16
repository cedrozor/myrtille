using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Enterprise
{
    public class Session
    {
        public Session() { Expire = DateTime.Now.AddHours(12); OneTime = false; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Index(IsClustered = true), StringLength(100)]
        public string SessionID { get; set; }

        [Index, StringLength(250)]
        public string Username { get; set; }

        [StringLength(2000)]
        public string Password { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime Expire { get; set; }

        public bool OneTime { get; set; }
    }
}
