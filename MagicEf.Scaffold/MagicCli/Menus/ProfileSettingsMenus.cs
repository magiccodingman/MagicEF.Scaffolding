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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            var settingFuncTasks = new List<(string Name, string Description, string Value, Func<Task> Task)>();

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
                var task = SetProfileSetting(true, AdvancedSettings, profile, generalSettings, lambda);

                // Store name, description, value, and task
                settingTasks.Add((name, description, value, task));
            }

            // Get the ProjectSpecificSettings property from ProjectProfile
            var projectSettingsProperty = typeof(ProjectProfile).GetProperty("ProjectSpecificSettings");

            if (projectSettingsProperty != null)
            {
                var projectSettingsInstance = projectSettingsProperty.GetValue(profile) as ProjectSettings;

                if (projectSettingsInstance != null)
                {
                    var projSpecificProperties = typeof(ProjectSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(prop => prop.GetCustomAttribute<MagicSettingInfoAttribute>() != null);

                    foreach (var property in projSpecificProperties)
                    {
                        var attribute = property.GetCustomAttribute<MagicSettingInfoAttribute>();
                        string name = attribute?.Name ?? property.Name;
                        string description = attribute?.Description ?? "No description provided";

                        // Retrieve the value of the property from the ProjectSettings instance
                        object propertyValue = property.GetValue(projectSettingsInstance);
                        string value = propertyValue?.ToString() ?? "null";

                        // Get the property type dynamically
                        Type propertyType = property.PropertyType;

                        // Create the lambda expression: x => x.ProjectSpecificSettings.SomeProperty
                        var profileParameter = Expression.Parameter(typeof(ProjectProfile), "x");
                        var settingsPropertyAccess = Expression.Property(profileParameter, projectSettingsProperty);
                        var propertyAccess = Expression.Property(settingsPropertyAccess, property);

                        // Create the correct generic delegate type Func<ProjectProfile, TProperty>
                        Type funcType = typeof(Func<,>).MakeGenericType(typeof(ProjectProfile), propertyType);

                        // Use the correct Lambda overload dynamically
                        var lambdaMethod = typeof(Expression)
                            .GetMethods()
                            .FirstOrDefault(m => m.Name == nameof(Expression.Lambda) &&
                                                 m.GetGenericArguments().Length == 1 &&
                                                 m.GetParameters().Length == 2);

                        if (lambdaMethod == null)
                            throw new InvalidOperationException("Failed to locate correct Expression.Lambda method.");

                        var lambdaGenericMethod = lambdaMethod.MakeGenericMethod(funcType);
                        var lambda = lambdaGenericMethod.Invoke(null, new object[] { propertyAccess, new ParameterExpression[] { profileParameter } });

                        // Locate the SetProfileSetting<TProperty> method (now using NonPublic)
                        var setProfileSettingMethod = typeof(MagicCliHandler)
                        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == nameof(SetProfileSetting) &&
                                             m.IsGenericMethod &&
                                             m.GetParameters().Length == 5); // ✅ Now correctly expecting 5 parameters


                        if (setProfileSettingMethod == null)
                            throw new InvalidOperationException($"SetProfileSetting<TProperty> method not found!");

                        // Invoke the method with the correct type
                        var genericMethod = setProfileSettingMethod.MakeGenericMethod(propertyType);
                        // Store the function reference without executing it
                        Func<Task> deferredTask = async () =>
                        {
                            var result = genericMethod.Invoke(this, new object[] { true, AdvancedSettings, profile, generalSettings, lambda });
                            if (result is Task task)
                            {
                                await task;
                            }
                        };

                        // Add to settingTasks, storing the **function itself**, not its result
                        settingFuncTasks.Add((name, description, value, deferredTask));

                    }
                }
            }




            StringBuilder sb = new StringBuilder();

            foreach (var task in settingTasks)
            {
                sb.AppendLine($"{task.Name} - '{task.Description}'");
                sb.AppendLine();
            }

            foreach (var task in settingFuncTasks)
            {
                sb.AppendLine($"{task.Name} - '{task.Description}'");
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

            foreach (var task in settingFuncTasks)
            {
                menu.AddOption($"{task.Name} - {task.Value ?? "NOT SET"}",
                async () => await task.Task.Invoke());
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
                || profile.ProjectSpecificSettings.RunMagicIndexedDbScaffolding == true
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
                await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.PrimaryProjectPath);

            string indexedDbDescription = "Magic IndexedDB - runs the Magic IndexedDB scaffolding protocol for Blazor. " +
                "This will auto scaffold your IndexedDB migration scripts for easy use with the Magic.IndexedDB library.";


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

{indexedDbDescription}

{dbFirstDescription}

{shareProtocol}

{flattenProtocol}";
            var menu = new CliMenu("Creating New Profile", description);

            menu.AddOption($"Profile Name (Set to '{profile.Name??"REQUIRED TO SET"}')",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, generalSettings, x => x.Name));

            menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunMagicIndexedDbScaffolding)} Magic IndexedDB",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, generalSettings, x => x.ProjectSpecificSettings.RunMagicIndexedDbScaffolding));

            menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding)} Database first Scaffold",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, generalSettings, x => x.ProjectSpecificSettings.RunDatabaseFirstScaffolding));

            menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunShareProtocol)} Share (Truth) Protocol",
                async () => await SetProfileSetting(false, CreateNewProfile, profile, generalSettings, x => x.ProjectSpecificSettings.RunShareProtocol));

            if (profile.ProjectSpecificSettings.RunShareProtocol)
            {
                menu.AddOption($"{GetCheckbox(profile.ProjectSpecificSettings.RunFlatteningProtocol)} Flatten Protocol",
                    async () => await SetProfileSetting(false, CreateNewProfile, profile, generalSettings, x => x.ProjectSpecificSettings.RunFlatteningProtocol));
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

        public async Task SetRequiredPrimary(ProjectProfile _profile)
        {
            var profile = generalSettings.projectProfiles.FirstOrDefault(x => x.ProjectSpecificSettings.Id == _profile.ProjectSpecificSettings.Id);

            if (profile == null)
                return;

            // This must always exist!
            if (string.IsNullOrWhiteSpace(profile.PrimaryProjectPath)
                    || !appConfig.FileSystem.DirectoryExists(profile.PrimaryProjectPath))
            {
                await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.PrimaryProjectPath);
            }

            if (profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true)
            {
                if (string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.DbContextClassName))
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.DbContextClassName);
                }


                if (profile.ProjectSpecificSettings.RunSeparateVirtualProperties == null)
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.RunSeparateVirtualProperties);
                    await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.SeparateVirtualPropertiesPath);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.SeparateVirtualPropertiesPath)
                    && !appConfig.FileSystem.DirectoryExists(profile.ProjectSpecificSettings.FullSeparateVirtualPropertiesPath))
                        await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.SeparateVirtualPropertiesPath);
                }
            }

            if (profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true)
            {
                while (true)
                {
                    if (string.IsNullOrWhiteSpace(profile.DatabaseConnectionString) || profile.DbConnectionVerified == false)
                    {
                        if (string.IsNullOrWhiteSpace(profile.DatabaseConnectionString))
                            await SetProfileSetting(false, SetRequiredPrimary, profile, generalSettings, x => x.DatabaseConnectionString);

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
                    await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.ShareProjectPath);
                }
            }
            if (profile.ProjectSpecificSettings.RunFlatteningProtocol == true)
            {
                if (string.IsNullOrWhiteSpace(profile.ProjectSpecificSettings.FlattenProjectPath)
                    || !appConfig.FileSystem.DirectoryExists(profile.ProjectSpecificSettings.FullFlattenProjectPath))
                {
                    await SetProfileSetting(true, SetRequiredPrimary, profile, generalSettings, x => x.ProjectSpecificSettings.FlattenProjectPath);
                }
            }
        }



        private async Task SetProfileSetting<TProperty>(
bool ImmediateSave,
Func<ProjectProfile, Task>? action,  // Action to execute, if provided
ProjectProfile profile,              // The profile being modified
GeneralCliSettings _generalSettings,
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

            if(profile.ProjectSpecificSettings == null || profile.ProjectSpecificSettings.HasCompletedBasicConfiguration == false)
            {
                MagicSettingHelper.SetPropertyValue(profile, propertyExpression, newValue);
                await action(profile);
                return;
            }
            
            // Set the property dynamically
            var _profile = _generalSettings.projectProfiles.FirstOrDefault(x => x.ProjectSpecificSettings.Id == profile.ProjectSpecificSettings.Id);
            MagicSettingHelper.SetPropertyValue(_profile, propertyExpression, newValue);


            // Save settings
            if (ImmediateSave)
                _generalSettings.Save();

            // Execute the provided action with the modified profile
            if (action != null)
            {
                await action(_profile);
            }
        }
    }
}
