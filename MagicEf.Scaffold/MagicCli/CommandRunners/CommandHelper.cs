using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public static class CommandHelper
    {
        public static string CommandBuilder(params string[] strings)
        {
            return string.Join(" ", strings.Select(s => s.Replace("\n", "").Replace("\r", "").Trim()));
        }
    }
}
