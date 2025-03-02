using Magic.GeneralSystem.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    /// <summary>
    /// A class helper similar to the File and Directory helper, but utilizes caching and 
    /// nodes to increase performance and lower memory use with the caching.
    /// </summary>
    public class MagicFileSystem
    {
        private readonly Dictionary<string, MagicDirectoryNode> _roots;

        public MagicFileSystem()
        {
            _roots = new Dictionary<string, MagicDirectoryNode>(StringComparer.OrdinalIgnoreCase);
        }

        private string NormalizeAndResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Environment.CurrentDirectory; // Default to relative path

            path = Path.GetFullPath(path.Trim());
            return path.StartsWith("\\\\") ? path : path.Replace('\\', '/');
        }

        public MagicDirectory? GetMagicDirectory(string directoryPath, bool forceRefresh = false)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);
            var node = FindOrCreateNode(directoryPath, forceRefresh);

            // ✅ If it was removed or doesn't exist, remove it from cache and return null
            if (!Directory.Exists(directoryPath))
            {
                RemoveNode(directoryPath);
                return null;
            }

            // ✅ If permissions are null or we're forcing refresh, update it
            if (forceRefresh || node.Permissions == null)
                node.Permissions = DirectoryHelper.GetPermissions(directoryPath);

            return new MagicDirectory(directoryPath, false)
            {
                Files = node.Files,
                Folders = node.Subdirectories.Keys.ToList(),
                Permissions = node.Permissions // ✅ Use cached or updated permissions
            };
        }

        public List<MagicDirectory> FindDirectoriesWithFilesByExtension(string baseDirectory, string extension, bool forceRefresh = false)
        {
            baseDirectory = NormalizeAndResolvePath(baseDirectory);

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("File extension cannot be null or empty.", nameof(extension));

            // Remove period if present
            extension = extension.TrimStart('.').ToLowerInvariant();

            // ✅ Step 1: Ensure base directory is fully cached
            AddAllDirectoriesRecursively(baseDirectory, forceRefresh);

            List<MagicDirectory> result = new List<MagicDirectory>();

            // ✅ Step 2: Iterate over the cached structure to find matching directories
            Queue<MagicDirectoryNode> queue = new Queue<MagicDirectoryNode>();
            var baseNode = FindOrCreateNode(baseDirectory, forceRefresh);
            queue.Enqueue(baseNode);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                // ✅ Step 3: Filter files by extension
                var matchingFiles = currentNode.Files
                    .Where(file => file.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingFiles.Count > 0)
                {
                    // ✅ Step 4: Convert node to MagicDirectory (only with matching files)
                    var magicDir = new MagicDirectory(baseDirectory, false)
                    {
                        Files = matchingFiles,
                        Folders = currentNode.Subdirectories.Keys.ToList(),
                        Permissions = currentNode.Permissions ?? DirectoryHelper.GetPermissions(baseDirectory)
                    };

                    result.Add(magicDir);
                }

                // ✅ Step 5: Add subdirectories to queue
                foreach (var subDir in currentNode.Subdirectories.Values)
                    queue.Enqueue(subDir);
            }

            return result;
        }

        public MagicDirectory? GetMagicFile(string filePath, bool forceRefresh = false)
        {
            filePath = NormalizeAndResolvePath(filePath);
            var directoryPath = Path.GetDirectoryName(filePath);

            if (directoryPath == null)
                return null;

            var directoryNode = FindOrCreateNode(directoryPath, forceRefresh);
            var file = directoryNode.Files.FirstOrDefault(f => f.FullFileName.Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));

            if (file == null && forceRefresh)
            {
                // ✅ Force-refresh the directory and try again
                directoryNode = FindOrCreateNode(directoryPath, true);
                file = directoryNode.Files.FirstOrDefault(f => f.FullFileName.Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));
            }

            // ✅ If file is still missing, remove directory from cache
            if (file == null)
            {
                RemoveNode(directoryPath);
                return null;
            }

            // ✅ Create a MagicDirectory containing just this file
            return new MagicDirectory(directoryPath, false)
            {
                Files = new List<MagicFile> { file },
                Folders = directoryNode.Subdirectories.Keys.ToList(),
                Permissions = directoryNode.Permissions ?? DirectoryHelper.GetPermissions(directoryPath)
            };
        }

        public void AddAllDirectoriesRecursively(string directoryPath, bool forceRefresh = false, HashSet<string>? excludedPaths = null)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);
            var rootNode = FindOrCreateNode(directoryPath, forceRefresh);

            if (rootNode.CheckedRecursively && !forceRefresh)
                return;

            var excludeSet = excludedPaths ?? new HashSet<string>();
            foreach (var subdir in rootNode.Subdirectories.Keys)
            {
                if (rootNode.Subdirectories[subdir].CheckedRecursively)
                    excludeSet.Add(Path.Combine(directoryPath, subdir));
            }

            PopulateSubdirectories(directoryPath, rootNode, excludeSet, forceRefresh);
            rootNode.MarkCheckedRecursively();
        }

        private void PopulateSubdirectories(string currentPath, MagicDirectoryNode currentNode, HashSet<string> excludePaths, bool forceRefresh)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(currentPath))
                {
                    if (excludePaths.Contains(dir) && !forceRefresh)
                        continue;

                    var subDirNode = FindOrCreateNode(dir, forceRefresh);
                    currentNode.Subdirectories[subDirNode.Name] = subDirNode;

                    PopulateSubdirectories(dir, subDirNode, excludePaths, forceRefresh);
                }

                currentNode.MarkCheckedDirectly();
            }
            catch (Exception ex)
            {
                throw new IOException($"Error accessing directories: {currentPath}", ex);
            }
        }

        private MagicDirectoryNode FindOrCreateNode(string path, bool forceRefresh = false)
        {
            path = NormalizeAndResolvePath(path);
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string rootPath = segments[0] + (path.StartsWith("\\\\") ? "" : "/");
            if (!_roots.TryGetValue(rootPath, out var current))
            {
                current = new MagicDirectoryNode(rootPath);
                _roots[rootPath] = current;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                if (!current.Subdirectories.TryGetValue(segments[i], out var next))
                {
                    next = new MagicDirectoryNode(segments[i]);
                    current.Subdirectories[segments[i]] = next;
                }
                current = next;
            }

            if (forceRefresh)
                RefreshNode(current);

            return current;
        }

        private void RefreshNode(MagicDirectoryNode node)
        {
            string fullPath = NormalizeAndResolvePath(node.Name);
            if (Directory.Exists(fullPath))
            {
                node.Files = Directory.GetFiles(fullPath).Select(f => new MagicFile(f)).ToList();
                node.Subdirectories.Clear();

                foreach (var dir in Directory.GetDirectories(fullPath))
                {
                    var subNode = new MagicDirectoryNode(Path.GetFileName(dir));
                    node.Subdirectories[subNode.Name] = subNode;
                }

                // ✅ Update permissions upon refresh
                node.Permissions = DirectoryHelper.GetPermissions(fullPath);
            }
            else
            {
                RemoveNode(fullPath);
            }
        }

        private void RemoveNode(string path)
        {
            path = NormalizeAndResolvePath(path);
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (!_roots.TryGetValue(segments[0] + (path.StartsWith("\\\\") ? "" : "/"), out var current))
                return;

            for (int i = 1; i < segments.Length - 1; i++)
            {
                if (!current.Subdirectories.TryGetValue(segments[i], out var next))
                    return;
                current = next;
            }

            // ✅ Remove the last node from the cache
            current.Subdirectories.Remove(segments.Last());
        }

        public bool FileExists(string filePath, bool forceRefresh = false)
        {
            filePath = NormalizeAndResolvePath(filePath);

            var magicFile = GetMagicFile(filePath, forceRefresh);
            return magicFile != null;
        }

        public bool DirectoryExists(string directoryPath, bool forceRefresh = false)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);

            var magicDir = GetMagicDirectory(directoryPath, forceRefresh);
            return magicDir != null;
        }

        public void DeleteFile(string filePath)
        {
            filePath = NormalizeAndResolvePath(filePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            RemoveNode(Path.GetDirectoryName(filePath)); // ✅ Remove from cache
        }

        public void TrueDeleteFile(string filePath)
        {
            filePath = NormalizeAndResolvePath(filePath);

            if (File.Exists(filePath))
            {
                FileHelper.TrueDelete(filePath);
            }
            RemoveNode(Path.GetDirectoryName(filePath)); // ✅ Remove from cache
        }

        public void DeleteDirectory(string directoryPath, bool recursive = true)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);

            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive);
            }
            RemoveNode(directoryPath); // ✅ Remove from cache
        }

        public void TrueDeleteDirectory(string directoryPath)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);

            if (Directory.Exists(directoryPath))
            {
                DirectoryHelper.TrueDelete(directoryPath);
            }
            RemoveNode(directoryPath); // ✅ Remove from cache
        }

        public void CreateDirectory(string directoryPath, bool forceRefresh = false)
        {
            directoryPath = NormalizeAndResolvePath(directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (forceRefresh)
                GetMagicDirectory(directoryPath, true); // ✅ Ensure it's added to cache
        }

    }
}
