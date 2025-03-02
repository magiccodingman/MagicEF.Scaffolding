using MagicEf.Scaffold.MagicCli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class MagicCliHandler
    {
        public async Task HandleAsync(string[] args)
        {
            try
            {
                await new InitialSetup().Initialize();





            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.InnerException?.Message??ex.Message}");
            }
        }
    }
}
