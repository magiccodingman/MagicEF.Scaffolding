using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold
{
    public static class FileHelper
    {
        public static string NormalizePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                string msg = "File path cannot be null or empty";
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
               
            return Path.GetFullPath(filePath);
        }

        public static string ReadFile(string filePath)
        {
            filePath = NormalizePath(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return File.ReadAllText(filePath);
        }

        public static void WriteFile(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }

        public static IEnumerable<string> GetCsFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            return Directory.GetFiles(directoryPath, "*.cs");
        }
    }
}
