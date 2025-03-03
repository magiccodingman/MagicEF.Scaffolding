using Magic.Cli.Toolkit;
using Magic.GeneralSystem.Toolkit.Attributes;
using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit;
using MagicEf.Scaffold.Helpers;
using MagicEf.Scaffold.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public partial class MagicCliHandler
    {
        private async Task MainMenu()
        {
            var menu = new CliMenu("Main Menu");

            menu.AddOption("Project Profiles", async () => await ProjectProfileMenu());
            menu.AddOption("Change Settings", async () => await GeneralSettings());
            menu.AddOption("Exit App", () => Environment.Exit(0));

            await menu.ShowAsync();
        }

        private async Task ProjectProfileMenu()
        {
            if (generalSettings.projectProfiles == null
                || !generalSettings.projectProfiles.Any())
            {
                await CreateNewProfile(new ProjectProfile());
            }
            var menu = new CliMenu("Project Profiles");
            foreach (var profile in generalSettings.projectProfiles)
            {
                menu.AddOption(profile.Name, async () => await SelectProfile(profile));
            }
            menu.AddOption("Add Profile", async () => await CreateNewProfile(new ProjectProfile()));
            menu.AddOption("Go back to 'Main Menu'", async () => await MainMenu());

            await menu.ShowAsync();
        }

        private async Task GeneralSettings()
        {
            string description =
            Environment.NewLine + "Current settings set to:"
            + Environment.NewLine + $"Auto Update: {generalSettings.GetAutoUpdate()}"
            +Environment.NewLine + Environment.NewLine+
            "Which setting would you like to change?";

            var menu = new CliMenu("General Settings", description);
            menu.AddOption($"Auto Update", async () => await SetGeneralSetting(() => generalSettings.SetAutoUpdate()));
            menu.AddOption("Go back to 'Main Menu'", async () => await MainMenu());

            await menu.ShowAsync();
        }

        private async Task SetGeneralSetting(Action action)
        {
            try
            {
                action?.Invoke(); // Safely invoke the action
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                Console.WriteLine($"Error in SetGeneralSetting: {ex.Message}");
            }
            await GeneralSettings();
        }
    }
}
