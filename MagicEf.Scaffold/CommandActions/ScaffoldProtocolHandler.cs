using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class ScaffoldProtocolHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            // Parse required arguments
            
            string? concretePath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--concretePath"));
            string? modelPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--modelPath"));
            string? extensionPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--extensionPath"));
            string? metaDataPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--metadataPath"));
            string? interfacesPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--interfacesPath"));
            // string dbHelpersPath = ArgumentHelper.GetArgumentValue(args, "--dbHelpersPath");
            string? projectFilePath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--projectFilePath"));

            // Check that all required arguments are present
            if (
                string.IsNullOrEmpty(concretePath) ||
                string.IsNullOrEmpty(modelPath) ||
                string.IsNullOrEmpty(extensionPath) ||
                string.IsNullOrEmpty(metaDataPath) ||
                string.IsNullOrEmpty(interfacesPath) ||
               // string.IsNullOrEmpty(dbHelpersPath) ||
                string.IsNullOrEmpty(projectFilePath))
            {
                Console.WriteLine("Error: All arguments are required.");
                return;
            }

            // Verify directories exist or create them
            EnsureDirectoryExists(concretePath);
            EnsureDirectoryExists(modelPath);
            EnsureDirectoryExists(extensionPath);
            EnsureDirectoryExists(metaDataPath);
            EnsureDirectoryExists(interfacesPath);
            //  EnsureDirectoryExists(dbHelpersPath);

            // Get project namespace name from csproj file
            string? projectNamespaceName = ProjectHelper.GetProjectNamespace(projectFilePath);
            if (string.IsNullOrEmpty(projectNamespaceName))
            {
                Console.WriteLine("Error: Could not determine project namespace from project file.");
                return;
            }

            // Process each .cs file in the model path
            var modelFiles = Directory.GetFiles(modelPath, "*.cs");
            foreach (var modelFile in modelFiles)
            {
                ProcessModelFile(modelFile, projectNamespaceName, interfacesPath, metaDataPath, extensionPath, concretePath);
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }


        private void ProcessModelFile(string modelFilePath, string projectNamespaceName, string interfacesPath, string metaDataPath, string extensionPath, string concretePath)
        {
            // Read the model file
            var code = FileHelper.ReadFile(modelFilePath);
            var root = RoslynHelper.ParseCode(code);

            // Get the class name
            var classDeclaration = root.DescendantNodes()
                                       .OfType<ClassDeclarationSyntax>()
                                       .FirstOrDefault();

            if (classDeclaration == null)
            {
                Console.WriteLine($"Error: Could not find a class in file {modelFilePath}");
                return;
            }

            var modelClassName = classDeclaration.Identifier.Text;

            // Get key properties and their types
            // Get key properties directly from the class declaration
            var keyProperties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => prop.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attr => attr.Name.ToString() == "Key"))
                .Select(prop => (Name: prop.Identifier.Text, Type: prop.Type.ToString()))
                .ToList();


            // Create interface
            CreateInterface(modelClassName, projectNamespaceName, interfacesPath);

            // Create metadata class
            CreateMetaDataClass(modelClassName, projectNamespaceName, metaDataPath);

            // Create extension class
            CreateExtensionClass(modelClassName, projectNamespaceName, extensionPath);

            // Create concrete repository class
            CreateRepositoryClass(modelClassName, projectNamespaceName, concretePath, keyProperties);
        }

        private void CreateInterface(string modelClassName, string projectNamespaceName, string interfacesPath)
        {
            string interfaceName = $"I{modelClassName}Repository";
            string interfaceFilePath = Path.Combine(interfacesPath, $"{interfaceName}.cs");

            if (File.Exists(interfaceFilePath))
            {
                Console.WriteLine($"Interface {interfaceName} already exists.");
                return;
            }

            string content = $@"namespace {projectNamespaceName}.Interfaces
{{
    public interface {interfaceName} : IRepository<{modelClassName}>
    {{
    }}
}}";

            File.WriteAllText(interfaceFilePath, content);
            Console.WriteLine($"Created interface: {interfaceFilePath}");
        }

        private void CreateMetaDataClass(string modelClassName, string projectNamespaceName, string metaDataPath)
        {
            string metaDataClassName = $"{modelClassName}Metadata";
            string metaDataFilePath = Path.Combine(metaDataPath, $"{metaDataClassName}.cs");

            if (File.Exists(metaDataFilePath))
            {
                Console.WriteLine($"Metadata class {metaDataClassName} already exists.");
                return;
            }

            string content = $@"namespace {projectNamespaceName}
{{
    public partial class {metaDataClassName}
    {{
    }}
}}";

            File.WriteAllText(metaDataFilePath, content);
            Console.WriteLine($"Created Metadata class: {metaDataFilePath}");
        }

        private void CreateExtensionClass(string modelClassName, string projectNamespaceName, string extensionPath)
        {
            string extensionClassFileName = $"{modelClassName}Extension.cs";
            string extensionFilePath = Path.Combine(extensionPath, extensionClassFileName);

            if (File.Exists(extensionFilePath))
            {
                Console.WriteLine($"Extension class {extensionClassFileName} already exists.");
                return;
            }

            string metaDataClassName = $"{modelClassName}Metadata";

            string content = $@"using System.ComponentModel.DataAnnotations;

namespace {projectNamespaceName}
{{
    [MetadataType(typeof({metaDataClassName}))]
    public partial class {modelClassName}
    {{
    }}
}}";

            File.WriteAllText(extensionFilePath, content);
            Console.WriteLine($"Created Extension class: {extensionFilePath}");
        }

        private void CreateRepositoryClass(string modelClassName, string projectNamespaceName, string concretePath, List<(string Name, string Type)> keyProperties)
        {
            string repositoryClassName = $"{modelClassName}Repository";
            string repositoryFilePath = Path.Combine(concretePath, $"{repositoryClassName}.cs");

            if (File.Exists(repositoryFilePath))
            {
                Console.WriteLine($"Repository class {repositoryClassName} already exists.");
                return;
            }

            string interfaceName = $"I{modelClassName}Repository";

            // Generate the GetById methods
            string getByIdMethods = GenerateGetByIdMethods(modelClassName, keyProperties);

            string content = $@"using System;
using System.Collections.Generic;
using System.Linq;
using {projectNamespaceName}.Interfaces;

namespace {projectNamespaceName}
{{
    public class {repositoryClassName} : RepositoryBase<{modelClassName}>, {interfaceName}
    {{
{getByIdMethods}
    }}
}}";

            File.WriteAllText(repositoryFilePath, content);
            Console.WriteLine($"Created Repository class: {repositoryFilePath}");
        }

        private string GenerateGetByIdMethods(string modelClassName, List<(string Name, string Type)> keyProperties)
        {
            // Generate parameters for the first GetById method
            var parameters = keyProperties.Select(kp => $"{kp.Type} {kp.Name}").ToList();
            var parameterList = string.Join(", ", parameters);

            // Generate dictionary entries for the second GetById method
            var dictionaryEntries = keyProperties.Select(kp => $"{{ \"{kp.Name}\", {kp.Name} }}").ToList();
            var dictionaryContent = string.Join(", ", dictionaryEntries);

            // Generate validation code for each key in the second GetById method
            var validations = keyProperties.Select(kp =>
$@"            if (!idDict.TryGetValue(""{kp.Name}"", out object? temp{kp.Name}))
            {{
                throw new ArgumentException(""id must contain key '{kp.Name}'"");
            }}

            if (temp{kp.Name} == null || !(temp{kp.Name} is {kp.Type}))
            {{
                throw new ArgumentException($""id['{kp.Name}'] must be of type {kp.Type}"");
            }}

            var {kp.Name}Value = ({kp.Type})temp{kp.Name};").ToList();

            var validationCode = string.Join("\n\n", validations);

            // Generate the Where clause for the query
            var whereConditions = keyProperties.Select(kp => $"c.{kp.Name} == {kp.Name}Value").ToList();
            var whereClause = string.Join(" && ", whereConditions);

            string methodCode = $@"
        public {modelClassName} GetById({parameterList})
        {{
            // Call the default implementation with the parameters as a dictionary
            return GetById((object)new Dictionary<string, object> {{ {dictionaryContent} }});
        }}

        public {modelClassName} GetById(object id)
        {{
            var idDict = id as IDictionary<string, object>;
            if (idDict == null)
            {{
                throw new ArgumentException(""id must be a dictionary with string keys"");
            }}

{validationCode}

            // Fetch the record from the database
            return _dbSet.FirstOrDefault(c => {whereClause}) ?? throw new Exception(""Provided Id for '{modelClassName}' did not exist"");
        }}";

            return methodCode;
        }
    }
}
