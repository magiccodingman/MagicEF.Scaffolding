using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CLI.Models
{
    public class MagicCliResponse
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
    }
}
