using Magic.Cli.Toolkit;
using Magic.GeneralSystem.Toolkit;
using MagicEf.Scaffold.MagicCli;
using MagicEf.Scaffold.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magic.GeneralSystem.Toolkit.Helpers;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Magic.GeneralSystem.Toolkit.Attributes;
using System.Reflection;
using MagicEf.Scaffold.Helpers;

namespace MagicEf.Scaffold.CommandActions
{
    // Multiple partially connected classes (for better organization) in MagicCli/Menus
    public partial class MagicCliHandler
    {
        private AppConfig appConfig;
        private static GeneralCliSettings? _instance;

        public static GeneralCliSettings generalSettings
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("GeneralSettings has not been initialized.");
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public async Task HandleAsync(string[] args)
        {
            try
            {
                var result = await new InitialSetup().Initialize();
                appConfig = result.Item1;
                generalSettings = result.Item2;

                await MainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.InnerException?.Message??ex.Message}");
            }
        }

        
    }
}
