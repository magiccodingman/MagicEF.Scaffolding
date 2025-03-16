using Magic.GeneralSystem.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Settings
{
    public class ProjectProfile
    {
        public const string SuggestedRelativeDesc = "It's highly suggested you utilize a relative path starting from where your primary project path is located (eg. '.\\' or '..\\..\\').";
        //private readonly string ProjectPath;
        public static readonly string ProjectSettingsName = "MagicProfile";
        //public ProjectSettings ProjectSpecificSettings;
        [JsonIgnore]
        private ProjectSettings? _projectSpecificSettings { get; set; }

        [JsonIgnore]
        public ProjectSettings ProjectSpecificSettings
        {
            get
            {
                if (_projectSpecificSettings == null && !string.IsNullOrWhiteSpace(PrimaryProjectPath))
                {
                    _projectSpecificSettings = new ProjectSettings(PrimaryProjectPath, ProjectSettingsName);
                }
                return _projectSpecificSettings;
            }
            set => _projectSpecificSettings = value;
        }

        public ProjectProfile()
        {
            //ProjectPath = projectPath;
            //ProjectSpecificSettings = new ProjectSettings(ProjectPath, ProjectSettingsName);
        }
        [MagicSettingEncrypt]
        [MagicSettingInfo("Database Connection String", "This is securely encrypted in the settings file with the password you created at the app launch.")]
        public string? DatabaseConnectionString { get; set; }
        public bool DbConnectionVerified { get; set; } = false;

        [MagicSettingInfo("Profile Name", "The name of the profile")]
        public string Name { get; set; }

        [MagicSettingInfo("Primary Project Directory", "Full path location to the primary project where your Csharp models live. Whether the models are custom, database first scaffolded, code first, or any other models reside. This will also be where your commands will run relative pathing.")]
        public string PrimaryProjectPath { get; set; }
    }
}
