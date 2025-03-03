using Magic.GeneralSystem.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Settings
{
    public class ProjectProfile
    {
        [MagicSettingEncrypt]
        [MagicSettingInfo("Database Connection String", "This is securely encrypted in the settings file with the password you created at the app launch.")]
        public string? DatabaseConnectionString { get; set; }
        public bool DbConnectionVerified { get; set; } = false;

        [MagicSettingInfo("Profile Name", "The name of the profile")]
        public string Name { get; set; }

        public bool HasCompletedBasicConfiguration { get; set; } = false;

        #region Database First Scaffolding
        [MagicSettingInfo("Db First Scaffolding", "Run Magic EF's database first scaffolding for database first configurations.")]
        public bool RunDatabaseFirstScaffolding { get; set; } = false;

        [MagicSettingInfo("Separate Virtual Properties", "When running the Db first scaffolding, you can choose to separate out the virtual properties for a cleaner GIT history experience.")]
        public bool? RunSeparateVirtualProperties { get; set; }

        [MagicSettingInfo("Separate Virtual Properties Path", "The directory Where you would like to place the separated virtual properties from the scaffolded classes. You may leave this empty if you want it to stay in the same folder as your scaffolded database class models.")]
        public string? SeparateVirtualPropertiesPath { get; set; }

        #endregion


        [MagicSettingInfo("Share (Truth) Protocol", "Run Magic EF's share (truth) protocol scaffolding")]
        public bool RunShareProtocol { get; set; } = false;

        [MagicSettingInfo("Flattening Protocol", "Run Magic EF's flattening protocol scaffolding")]
        public bool RunFlatteningProtocol { get; set; } = false;

        [MagicSettingInfo("Db First Scaffolding Path", "The directory path where your project directory resides where the database first scaffolding will occur.")]
        public string PrimaryProjectPath { get; set; }

        [MagicSettingInfo("Share (Truth) Protocol Path", "The directory path where your project directory resides where your share protocol scaffolding will occur.")]
        public string ShareProjectPath { get; set; }

        [MagicSettingInfo("Flattening Protocol", "The directory path where your project directory resides where your flattening protocol scaffolding will occur.")]
        public string FlattenProjectPath { get; set; }
    }
}
