using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit
{
    public class MagicFile
    {
        public MagicFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string fileName = Path.GetFileName(filePath.Trim());

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Invalid file path, unable to extract file name.", nameof(filePath));

            // Extract last extension
            int lastDotIndex = fileName.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                Name = fileName.Substring(0, lastDotIndex); // Everything before the last dot
                Extension = fileName.Substring(lastDotIndex + 1); // Everything after the last dot
            }
            else
            {
                Name = fileName; // No extension found
                Extension = string.Empty;
            }

            Name = Name.Trim();
            Extension = Extension.Trim();
        }

        public string GetFullFileName()
        {
            return string.IsNullOrWhiteSpace(Extension) ? Name : $"{Name}.{Extension}";
        }

        /// <summary>
        /// The name without the extension
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The extension of the file without the period
        /// </summary>
        public string Extension { get; set; }
    }
}
