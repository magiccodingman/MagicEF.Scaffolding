using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Models
{
    public class MagicDirectoryNode
    {
        public string Name { get; }
        public Dictionary<string, MagicDirectoryNode> Subdirectories { get; }
        public List<MagicFile> Files { get; set; } = new List<MagicFile>();
        public Permissions? Permissions { get; set; }

        // New Flags for Caching Logic
        public bool CheckedDirectly { get; private set; } = false; // Checked files & subfolders?
        public bool CheckedRecursively { get; private set; } = false; // Checked everything inside?

        public MagicDirectoryNode(string name)
        {
            Name = name;
            Subdirectories = new Dictionary<string, MagicDirectoryNode>(StringComparer.OrdinalIgnoreCase);
            Files = new List<MagicFile>();
        }

        public void MarkCheckedDirectly() => CheckedDirectly = true;
        public void MarkCheckedRecursively() => CheckedRecursively = true;
    }
}
