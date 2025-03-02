using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.FileSystem.Toolkit.Models
{
    public class Permissions
    {
        public bool Write { get; set; } = false;
        public bool Read { get; set; } = false;
        public bool Execute { get; set; } = false;
        public bool Modify { get; set; } = false;
    }
}
