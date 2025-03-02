using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Models
{
    public class DirectoryShard
    {
        public string FolderName { get; }
        public List<DirectoryShard> Subdirectories { get; }

        public DirectoryShard(string folderName)
        {
            FolderName = folderName;
            Subdirectories = new List<DirectoryShard>();
        }

        public void AddSubdirectory(DirectoryShard subdirectory)
        {
            Subdirectories.Add(subdirectory);
        }

        public IEnumerable<string> GetFullPaths(string basePath)
        {
            string currentFullPath = Path.Combine(basePath, FolderName);
            yield return currentFullPath;

            foreach (var subDir in Subdirectories)
            {
                foreach (var subPath in subDir.GetFullPaths(currentFullPath))
                {
                    yield return subPath;
                }
            }
        }
    }
}
