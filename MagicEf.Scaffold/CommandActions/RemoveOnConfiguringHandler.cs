using MagicEf.Scaffold.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class RemoveOnConfiguringHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            string filePath = ArgumentHelper.GetArgumentValue(args, "--filePath");

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Error: --filePath is required.");
                return;
            }

            ProcessFile(filePath);
        }

        private void ProcessFile(string filePath)
        {
            var code = FileHelper.ReadFile(filePath);
            var root = RoslynHelper.ParseCode(code);

            var rewriter = new OnConfiguringRemover();
            var newRoot = rewriter.Visit(root);

            if (rewriter.ChangesMade)
            {
                FileHelper.WriteFile(filePath, newRoot.ToFullString());
                Console.WriteLine($"Removed OnConfiguring method from: {filePath}");
            }
            else
            {
                Console.WriteLine("No OnConfiguring method found to remove.");
            }
        }
    }
}
