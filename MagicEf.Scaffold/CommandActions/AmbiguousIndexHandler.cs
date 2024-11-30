using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class AmbiguousIndexHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            string filePath = ArgumentHelper.GetArgumentValue(args, "--filePath");
            string directoryPath = ArgumentHelper.GetArgumentValue(args, "--directoryPath");

            if (!string.IsNullOrEmpty(filePath))
            {
                ProcessFile(filePath);
            }
            else if (!string.IsNullOrEmpty(directoryPath))
            {
                foreach (var csFile in FileHelper.GetCsFiles(directoryPath))
                {
                    ProcessFile(csFile);
                }
            }
            else
            {
                Console.WriteLine("Error: Provide either --filePath or --directoryPath.");
            }
        }

        private void ProcessFile(string filePath)
        {
            var code = FileHelper.ReadFile(filePath);
            var root = RoslynHelper.ParseCode(code);

            var rewriter = new IndexRewriter();
            var newRoot = rewriter.Visit(root);

            if (rewriter.ChangesMade)
            {
                FileHelper.WriteFile(filePath, newRoot.ToFullString());
                Console.WriteLine($"Updated file: {filePath}");
            }
            else
            {
                Console.WriteLine("No changes needed.");
            }
        }
    }
}
