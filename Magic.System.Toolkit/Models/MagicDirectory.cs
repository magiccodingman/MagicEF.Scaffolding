using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit
{
    public class MagicDirectory
    {
        private readonly string _originalPath;
        private readonly string _normalizedPath;
        private readonly bool _isUNCPath;

        public List<string> Folders { get; set; } = new List<string>();

        public List<MagicFile> Files { get; set; } = new List<MagicFile>();

        public string FullPath => _normalizedPath;

        public Permissions Permissions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assumeFileInheritsPermissions">Assume that files directly in this directory inherits the folder permissions</param>
        public MagicDirectory(string path, bool throwErrorIfNotExists = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Environment.CurrentDirectory; // Default to current directory

            _originalPath = path.Trim();
            _isUNCPath = DetectUNCPath(_originalPath);
            _normalizedPath = NormalizePath(_originalPath, _isUNCPath);
            _normalizedPath = EnsureDirectoryOnly(_normalizedPath);
            _normalizedPath = GetAbsolutePath(_normalizedPath);

            if (throwErrorIfNotExists && !Directory.Exists(_normalizedPath))
                throw new DirectoryNotFoundException($"Directory does not exist: {_originalPath}");
        }

        private bool DetectUNCPath(string path)
        {
            return path.StartsWith("\\\\");
        }

        private string NormalizePath(string path, bool useBackslashes)
        {
            path = path.Trim();
            char preferredSlash = useBackslashes ? '\\' : '/';
            char otherSlash = useBackslashes ? '/' : '\\';
            return path.Replace(otherSlash, preferredSlash);
        }

        public string GetFullFilePath(MagicFile magicFile)
        {
            return NormalizePath(Path.Combine(FullPath, magicFile.FullFileName), _isUNCPath);
        }

        private string EnsureDirectoryOnly(string path)
        {
            if (File.Exists(path))
                path = System.IO.Path.GetDirectoryName(path) ?? path;
            return path.TrimEnd('\\', '/');
        }

        private string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetNormalizedPath(bool useBackslashes = false)
        {
            return NormalizePath(_normalizedPath, useBackslashes);
        }

        public string GetOriginalNormalizedPath()
        {
            return NormalizePath(_normalizedPath, _isUNCPath);
        }
    }
}
