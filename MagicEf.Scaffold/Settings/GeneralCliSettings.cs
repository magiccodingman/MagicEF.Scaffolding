using Magic.GeneralSystem.Toolkit.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Settings
{
    public class GeneralCliSettings : JsonSettings<GeneralCliSettings>
    {
        public GeneralCliSettings(string directoryPath, string fileName)
            : base(directoryPath, fileName)
        {
            Load();

            if (AutoUpdate == null)
            {
                SetAutoUpdate();
            }
        }

        public void SetAutoUpdate()
        {
            Console.WriteLine();
            Console.WriteLine("Would you like to have MagicEF auto-update when ran? It's highly suggested you do. (y/n)");

            while (true)
            {
                Console.Write("> "); // Visual indicator for input
                string? input = Console.ReadLine()?.Trim().ToLower();

                if (input == "y")
                {
                    AutoUpdate = true;
                    break;
                }
                else if (input == "n")
                {
                    AutoUpdate = false;
                    break;
                }
                else
                {
                    Console.WriteLine("Incorrect input. Please enter 'y' for Yes or 'n' for No.");
                }
            }

            Save(); // Save the chosen setting after getting valid input
        }

        [JsonInclude]
        private bool? AutoUpdate { get; set; }

        public bool GetAutoUpdate()
        {
            return AutoUpdate??false;
        }

        public List<ProjectProfile> projectProfiles { get; set; } = new List<ProjectProfile>();
    }
}
