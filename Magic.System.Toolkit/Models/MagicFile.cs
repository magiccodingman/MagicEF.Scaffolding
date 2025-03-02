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

            // Extract file name from path
            FullFileName = Path.GetFileName(filePath.Trim());

            if (string.IsNullOrWhiteSpace(FullFileName))
                throw new ArgumentException("Invalid file path, unable to extract file name.", nameof(filePath));
        }

        /// <summary>
        /// The full file name with extension (e.g., "document.txt").
        /// </summary>
        public string FullFileName { get; }

        public string GetFullPath(MagicDirectory magicDirectory)
        {
            return magicDirectory.GetFullFilePath(this);
        }

        /// <summary>
        /// The name without the extension.
        /// </summary>
        public string Name
        {
            get
            {
                int lastDotIndex = FullFileName.LastIndexOf('.');
                return lastDotIndex > 0 ? FullFileName.Substring(0, lastDotIndex) : FullFileName;
            }
        }

        /// <summary>
        /// The extension of the file without the period.
        /// </summary>
        public string Extension
        {
            get
            {
                int lastDotIndex = FullFileName.LastIndexOf('.');
                return lastDotIndex > 0 ? FullFileName.Substring(lastDotIndex + 1) : string.Empty;
            }
        }
    }

}
