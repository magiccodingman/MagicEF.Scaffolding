using Magic.GeneralSystem.Toolkit.Attributes;
using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Settings
{
    public class ProjectSettings : JsonSettings<ProjectSettings>
    {
        public ProjectSettings(string directoryPath, string fileName)
            : base(directoryPath, fileName)
        {
            Load();
        }

        public bool HasCompletedBasicConfiguration { get; set; } = false;

        #region Database First Scaffolding
        [MagicSettingInfo("Db First Scaffolding", "Run Magic EF's database first scaffolding for database first configurations.")]
        public bool RunDatabaseFirstScaffolding { get; set; } = false;

        [MagicSettingInfo("Database Context Class Name", "The name that will be provided to your DbContext for LINQ to SQL use. Do not add any spaces!")]
        public string? DbContextClassName { get; set; }


        [MagicSettingInfo("Separate Virtual Properties", "When running the Db first scaffolding, you can choose to separate out the virtual properties for a cleaner GIT history experience.")]
        public bool? RunSeparateVirtualProperties { get; set; }

        [MagicSettingInfo("Scaffolded Models Directory Path", "Defaults to your primary project directory then, '.\\DbModels'. " + ProjectProfile.SuggestedRelativeDesc)]
        public string? ModelsDirectoryPath { get; set; }

        [JsonIgnore]
        public string FullModelsDirectoryPath
        {
            get => FullPath(ModelsDirectoryPath, "DbModels");
        }


        #endregion


        #region Separate Virtual Properties Commands

        [MagicSettingInfo("Separate Virtual Properties Path", "The directory Where you would like to place the separated virtual properties from the scaffolded classes. You may leave this empty if you want it to stay in the same folder as your scaffolded database class models.")]
        public string? SeparateVirtualPropertiesPath { get; set; }

        [JsonIgnore]
        public string FullSeparateVirtualPropertiesPath
        {
            get => FullPath(SeparateVirtualPropertiesPath);
        }

        #endregion


        [MagicSettingInfo("Share (Truth) Protocol", "Run Magic EF's share (truth) protocol scaffolding")]
        public bool RunShareProtocol { get; set; } = false;

        [MagicSettingInfo("Flattening Protocol", "Run Magic EF's flattening protocol scaffolding")]
        public bool RunFlatteningProtocol { get; set; } = false;

        [MagicSettingInfo("Share (Truth) Protocol Path", "The directory path where your project directory resides where your share protocol scaffolding will occur. " + ProjectProfile.SuggestedRelativeDesc)]
        public string ShareProjectPath { get; set; }

        [JsonIgnore]
        public string FullShareProjectPath
        {
            get => FullPath(ShareProjectPath);
        }

        [MagicSettingInfo("Flattening Protocol", "The directory path where your project directory resides where your flattening protocol scaffolding will occur." + ProjectProfile.SuggestedRelativeDesc)]
        public string FlattenProjectPath { get; set; }

        [JsonIgnore]
        public string FullFlattenProjectPath
        {
            get => FullPath(FlattenProjectPath);
        }

        private string FullPath(string? path, string? defaultFolderName = null)
        {
            if (!string.IsNullOrWhiteSpace(path)
                    && DirectoryHelper.IsFullPath(path))
                return path;
            else if (!string.IsNullOrWhiteSpace(path))
                return DirectoryHelper.GetResolvedPath(FullDirectoryPath, path);

            if (defaultFolderName == null)
                return FullDirectoryPath;
            else
                return Path.Combine(FullDirectoryPath, defaultFolderName);
        }
    }
}
