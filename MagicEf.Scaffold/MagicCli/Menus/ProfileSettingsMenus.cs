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
        private async Task SelectProfile(ProjectProfile profile)
        {
            if (profile.ProjectSpecificSettings.HasCompletedBasicConfiguration == false)
            {
                await CreateNewProfile(profile);
            }

            await SetRequiredPrimary(profile);

            var menu = new CliMenu(profile.Name);

            menu.AddOption("Run Automated Protocols", async () => await ProfileCommandRun(profile));
            menu.AddOption("Advanced Settings", async () => await AdvancedSettings(profile));
            menu.AddOption("Change Primary Command Settings", async () => await AlterProfileExistingProtocols(profile));
            menu.AddOption("Go back to 'Profiles'", async () => await ProjectProfileMenu());
            await menu.ShowAsync();
        }

        private async Task AdvancedSettings(ProjectProfile profile)
        {
            var settingTasks = new List<(string Name, string Description, string Value, Task Task)>();

            // Get all properties in the profile that have the [MagicSettingInfo] attribute
            var properties = typeof(ProjectProfile).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttribute<MagicSettingInfoAttribute>() != null);

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<MagicSettingInfoAttribute>();
                string name = attribute?.Name ?? property.Name;
                string description = attribute?.Description ?? "No description provided";

                // Retrieve the value of the property from the profile instance
                object propertyValue = property.GetValue(profile);
                string value = propertyValue?.ToString() ?? "null";

                // Create the lambda expression: x => x.PropertyName
                var parameter = Expression.Parameter(typeof(ProjectProfile), "x");
                var propertyAccess = Expression.Property(parameter, property);
                var lambda = Expression.Lambda<Func<ProjectProfile, object>>(Expression.Convert(propertyAccess, typeof(object)), parameter);

                // Create the SetProfileSetting task but don't await it yet
                var task = SetProfileSetting(true, SetRequiredPrimary, profile, lambda);

                // Store name, description, value, and task
                settingTasks.Add((name, description, value, task));
            }


            StringBuilder sb = new StringBuilder();

            foreach (var task in settingTasks)
            {
                sb.AppendLine($"{task.Name} - '{task.Value}'");
                sb.AppendLine();
            }
            sb.AppendLine();

            string fullDescription = sb.ToString();

            var menu = new CliMenu("Advanced Settings", fullDescription);
            foreach (var task in settingTasks)
            {
                menu.AddOption($"{task.Name} - {task.Value??"NOT SET"}",
                async () => await task.Task);
            }
            menu.AddOption($"Go back to '{profile.Name}'", async () => await SelectProfile(profile));
            await menu.ShowAsync();
        }

        private async Task CreateNewProfile(ProjectProfile profile)
        {
            var menu = await AlterProfileProtocols(profile);
            if (!string.IsNullOrWhiteSpace(profile.Name) &&
                (
                profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true
                || profile.ProjectSpecificSettings.RunShareProtocol == true
                || profile.ProjectSpecificSettings.RunFlatteningProtocol == true
                ))
            {
                menu.AddOption($"SAVE PROFILE",
                    async () => await SaveNewProfile(profile, true));
            }

            menu.AddOption("Go back to 'Profiles' (do not save)", async () => await ProjectProfileMenu());
            await menu.ShowAsync();
        }

        private async Task<CliMenu> AlterProfileProtocols(ProjectProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.PrimaryProjectPath))
                await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.PrimaryProjectPath);

            string dbFirstDescription = "Database first Scaffold - runs the 'scaffoldProtocol' for database first projects. " +
                "Fixes commonplace errors while additionally scaffolding extensions, concrete classes, and powerful " +
                "LINQ to SQL repositories.";

            string shareProtocol = "Share (Truth) Protocol - The highly specialized protocol for creating truth " +
                "based mappings. Which allows powerful data transportation, flattening with zero mappings, " +
                "and opens up a world of possibilities. It's highly encouraged you enable the Flattening " +
                "Protocol alongside this. As they pair together.";

            string flattenProtocol = "Flatten Protocol - (Enabled when share is set protocol to true) Requires share protocol to utilize." +
                "This will scaffold and build your share mappings into flattened DTO's that're completely separated " +
                "and require ZERO auto mapping.";

            string description =
$@"To create a profile, you must create a profile name and select at least one protocol to save.

Profile Name (REQUIRED) - The name you'd like to give this profile.

{dbFirstDescription}

{shareProtocol}

{flattenProtocol}";
            var menu = new CliMenu("Creating New Profile", description);

            menu.AddOption($"Profile Name (Set to '{profile.Name??"REQUIRED TO SET"}')",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, x => x.Name));

            menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding)} Database first Scaffold",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, x => x.ProjectSpecificSettings.RunDatabaseFirstScaffolding));

            menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunShareProtocol)} Share (Truth) Protocol",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, x => x.ProjectSpecificSettings.RunShareProtocol));

            if (profile.ProjectSpecificSettings.RunShareProtocol)
            {
                menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunFlatteningProtocol)} Flatten Protocol",
                    async () => await SetProfileSetting(false, CreateNewProfile, profile, x => x.ProjectSpecificSettings.RunFlatteningProtocol));
            }
            else
            {
                menu.AddOption($"[Disabled Unless Share Enabled] Flatten Protocol",
                    async () => await CreateNewProfile(profile));
            }

            /*if (!string.IsNullOrWhiteSpace(profile.Name) &&
                (
                profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true
                || profile.ProjectSpecificSettings.RunShareProtocol == true
                || profile.ProjectSpecificSettings.RunFlatteningProtocol == true
                ))
            {
                menu.AddOption($"SAVE PROFILE",
                    async () => await SaveNewProfile(profile));
            }

            menu.AddOption("Go back to 'Profiles' (do not save)", async () => await ProjectProfileMenu());
            await menu.ShowAsync();*/

            return menu;
        }

        private async Task AlterProfileExistingProtocols(ProjectProfile profile)
        {
            var menu = await AlterProfileProtocols(profile);
            if (!string.IsNullOrWhiteSpace(profile.Name) &&
                (
                profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true
                || profile.ProjectSpecificSettings.RunShareProtocol == true
                || profile.ProjectSpecificSettings.RunFlatteningProtocol == true
                ))
            {
                menu.AddOption($"SAVE PROFILE",
                    async () => await SaveNewProfile(profile, false));
            }

            menu.AddOption("Go back to 'Profiles' (do not save)", async () => await ProjectProfileMenu());
            await menu.ShowAsync();
        }

        private async Task SaveNewProfile(ProjectProfile profile, bool isFirstProfileSave)
        {
            profile.ProjectSpecificSettings.HasCompletedBasicConfiguration = true;
            if (isFirstProfileSave)
                generalSettings.projectProfiles.Add(profile);
            generalSettings.Save();
            await ProjectProfileMenu();
        }


        private string GetCheckbox(bool isChecked)
        {
            return isChecked ? "[Enabled]" : "[Disabled]";
        }

        public async Task SetRequiredPrimary(ProjectProfile profile)
        {
            if (profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true)
            {
                if (string.IsNullOrWhiteSpace(profile.PrimaryProjectPath)
                    || !appConfig.FileSystem.DirectoryExists(profile.PrimaryProjectPath))
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.PrimaryProjectPath);
                }

                if (profile.ProjectSpecificSettings.RunSeparateVirtualProperties == null)
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.ProjectSpecificSettings.RunSeparateVirtualProperties);
                    await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.ProjectSpecificSettings.SeparateVirtualPropertiesPath);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.SeparateVirtualPropertiesPath)
                    && !appConfig.FileSystem.DirectoryExists(profile.ProjectSpecificSettings.FullSeparateVirtualPropertiesPath))
                        await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.ProjectSpecificSettings.SeparateVirtualPropertiesPath);
                }
            }

            if (profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true)
            {
                while (true)
                {
                    if (string.IsNullOrWhiteSpace(profile.DatabaseConnectionString) || profile.DbConnectionVerified == false)
                    {
                        if (string.IsNullOrWhiteSpace(profile.DatabaseConnectionString))
                            await SetProfileSetting(false, SetRequiredPrimary, profile, x => x.DatabaseConnectionString);

                        bool ConnectionStringSet = DatabaseHelper.ValidateConnectionString(profile.DatabaseConnectionString);
                        bool overrideConnectionString = false;

                        while (!ConnectionStringSet)
                        {
                            Console.Clear();
                            Console.WriteLine("The provided connection string did not work.");
                            Console.WriteLine("Type 'override' to keep it anyway or 'try again' to enter a new one:");

                            string userInput = Console.ReadLine()?.Trim().ToLower();

                            if (userInput == "override")
                            {
                                overrideConnectionString = true;
                                Console.WriteLine("Connection string override enabled.");
                                break;
                            }
                            else if (userInput == "try again")
                            {
                                profile.DatabaseConnectionString = null;
                            }
                            else
                            {
                                Console.WriteLine("Invalid input. Please type 'override' or 'try again'.");
                                Task.Delay(1500).Wait(); // Pause briefly before clearing to avoid flashing
                            }
                        }


                        if (ConnectionStringSet == true || overrideConnectionString == true)
                        {
                            profile.DbConnectionVerified = true;
                            generalSettings.Save();
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (profile.ProjectSpecificSettings.RunShareProtocol == true)
            {
                if (string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.ShareProjectPath)
                    || !appConfig.FileSystem.DirectoryExists(profile.ProjectSpecificSettings.FullShareProjectPath))
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.ProjectSpecificSettings.ShareProjectPath);
                }
            }
            if (profile.ProjectSpecificSettings.RunFlatteningProtocol == true)
            {
                if (string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.FlattenProjectPath)
                    || !appConfig.FileSystem.DirectoryExists(profile.ProjectSpecificSettings.FullFlattenProjectPath))
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, x => x.ProjectSpecificSettings.FlattenProjectPath);
                }
            }
        }



        private async Task SetProfileSetting<TProperty>(
bool ImmediateSave,
Func<ProjectProfile, Task>? action,  // Action to execute, if provided
ProjectProfile profile,              // The profile being modified
Expression<Func<ProjectProfile, TProperty>> propertyExpression)
        {
            // Get the name and description dynamically
            var (name, description) = MagicSettingHelper.GetMagicSettingInfo(propertyExpression);
            var menu = new CliMenu(name, description);
            Console.Clear();
            Console.WriteLine($"=== {name} ===");
            Console.WriteLine(description);
            Console.WriteLine();

            TProperty newValue;
            string? path = null;

            // Special handling for "PrimaryProjectPath"
            if (propertyExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member.Name == nameof(ProjectProfile.PrimaryProjectPath) &&
                typeof(TProperty) == typeof(string))
            {
                while (true) // Loop until a valid input or override
                {
                    path = MagicSettingHelper.ReadUserInput<string>(name);

                    if (!Directory.Exists(path))
                    {
                        Console.WriteLine("Error: The provided path does not exist. Please enter a valid directory.");
                        continue; // Prompt user again
                    }

                    bool hasCsproj = Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Any();

                    if (hasCsproj)
                    {
                        break; // Proceed with setting the property
                    }

                    Console.WriteLine("Warning: No .csproj file was found in the provided directory.");
                    Console.WriteLine("Type 'override' to proceed anyway, or 'try again' to enter a different path.");

                    string response = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
                    if (response == "override")
                    {
                        break; // Allow user to continue with the provided path
                    }
                    else if (response == "try again")
                    {
                        continue; // Loop to prompt user again
                    }
                }

                newValue = (TProperty)(object)path!;
            }
            else
            {
                // Read and parse user input safely for other properties
                newValue = MagicSettingHelper.ReadUserInput<TProperty>(name);
            }

            // Set the property dynamically
            MagicSettingHelper.SetPropertyValue(profile, propertyExpression, newValue);

            // Save settings
            if (ImmediateSave)
                generalSettings.Save();

            // Execute the provided action with the modified profile
            if (action != null)
            {
                await action(profile);
            }
        }
    }
}
