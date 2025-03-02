using Magic.GeneralSystem.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public static class DirectoryHelper
    {
        /// <summary>
        /// Extremely efficient method of returning all the directories that can be found recursively. 
        /// Method utilizes sharded model for returned paths to prevent memory overflow in case of 
        /// extreme scenarios of grabbing all paths within deeply nested large starting points.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static DirectoryShard GetAllDirectoriesSharded(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

            directoryPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The specified directory does not exist: {directoryPath}");

            // Now the root DirectoryShard **includes** the provided directory itself
            DirectoryShard root = new DirectoryShard(directoryPath);
            PopulateSubdirectories(directoryPath, root);

            return root;
        }

        private static void PopulateSubdirectories(string currentPath, DirectoryShard currentNode)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(currentPath))
                {
                    var subDir = new DirectoryShard(dir);
                    currentNode.AddSubdirectory(subDir);
                    PopulateSubdirectories(dir, subDir);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Error while accessing directories: {currentPath}", ex);
            }
        }

        public static List<MagicFile> GetFilesAsMagicFiles(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

            directoryPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The specified directory does not exist: {directoryPath}");

            List<MagicFile> magicFiles = new List<MagicFile>();

            try
            {
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    magicFiles.Add(new MagicFile(file));
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to retrieve files from directory: {directoryPath}", ex);
            }

            return magicFiles;
        }

        public static Permissions GetPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            path = Path.GetFullPath(path);

            var permissions = new Permissions
            {
                Read = CanRead(path),
                Write = CanWrite(path),
                Execute = CanExecute(path),
                Modify = CanModify(path)
            };

            return permissions;
        }

        private static bool CanRead(string path)
        {
            try
            {
                using (var fs = new FileStream(Path.Combine(path, Path.GetRandomFileName()), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                {
                    FileHelper.TrueDelete(fs.Name);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool CanWrite(string path)
        {
            try
            {
                string testFile = Path.Combine(path, Path.GetRandomFileName());
                File.WriteAllText(testFile, "test");
                FileHelper.TrueDelete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CanExecute(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    var acl = dirInfo.GetAccessControl();
                    var rules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

                    foreach (FileSystemAccessRule rule in rules)
                    {
                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) == FileSystemRights.ExecuteFile)
                        {
                            return true;
                        }
                    }
                }
                catch { }
                return false;
            }
            else
            {
                try
                {
                    var filePath = Path.Combine(path, Path.GetRandomFileName());
                    File.WriteAllText(filePath, "#!/bin/sh\necho test");
                    File.SetUnixFileMode(filePath, UnixFileMode.UserExecute);
                    FileHelper.TrueDelete(filePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool CanModify(string path)
        {
            try
            {
                string testDir = Path.Combine(path, Path.GetRandomFileName());
                Directory.CreateDirectory(testDir);
                TrueDelete(testDir);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void TrueDelete(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

            directoryPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The specified directory does not exist: {directoryPath}");

            try
            {
                // Recursively delete files securely
                foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    FileHelper.TrueDelete(file);
                }

                // Recursively delete directories
                foreach (var subDir in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
                {
                    TrueDelete(subDir); // Recursive call
                }

                // Delete the now-empty directory
                Directory.Delete(directoryPath, false);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to permanently delete directory: {directoryPath}", ex);
            }
        }
    }

}
