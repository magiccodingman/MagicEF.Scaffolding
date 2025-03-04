using Magic.GeneralSystem.Toolkit.Helpers;
using MagicEf.Scaffold.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Models
{
    public class PrimaryBaseProject
    {
        /// <summary>
        /// The file name with the csproj extension
        /// </summary>
        public string ProjectFileName { get; }
        public string NameSpace { get; }
        public string WorkingPath { get; }
        public string ScaffoldedModelsPath { get; }
        public string SeparatedVirtualPropertiesPath { get; }

        public PrimaryBaseProject(ProjectProfile profile)
        {
            WorkingPath = profile.PrimaryProjectPath;
            var files = FileHelper.GetFilesInDirectory(WorkingPath);

            var projectFilePath = FileHelper.FilterFilesByExtension(files, "csproj").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(projectFilePath))
                throw new Exception($"Could not find any csproj file within the working directory: {WorkingPath}");

            ProjectFileName = FileHelper.GetFileNameFromPath(projectFilePath);

            NameSpace = FileHelper.RemoveFileExtension(ProjectFileName);

            ScaffoldedModelsPath = profile.ProjectSpecificSettings.FullModelsDirectoryPath;

            SeparatedVirtualPropertiesPath = profile.ProjectSpecificSettings.FullSeparateVirtualPropertiesPath;
        }
    }
}
