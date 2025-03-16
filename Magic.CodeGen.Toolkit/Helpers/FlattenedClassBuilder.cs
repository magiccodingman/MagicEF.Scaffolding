using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public static class FlattenedClassBuilder
    {
        /// <summary>
        /// Constructs a fully flattened version of the provided class declaration by merging all connected partial 
        /// classes, base classes, and metadata type attributes while maintaining proper inheritance rules.
        /// 
        /// This method ensures that the final class contains all relevant properties, attributes, methods (only if explicitly included),
        /// generic constraints, and interfaces while excluding explicitly marked exclusions.
        /// 
        /// **Steps & Logic:**
        /// 1. **Collect All Connected Classes**:
        ///    - Uses `AnalyzerHelper.GetPartialClassDeclarations()` to find all **partial class definitions** tied to the original class.
        ///    - Uses `CollectConnectedClasses()` to recursively collect:
        ///      - **Base class extensions** (excluding `System.Object`).
        ///      - **Nested extensions** ensuring all connected parts are captured.
        ///    - Skips any class that has the `[MagicScaffoldExclude]` attribute.
        /// 
        /// 2. **Extract and Organize Class Components**:
        ///    - **Properties**: 
        ///      - Merges all properties while ensuring correct handling of:
        ///        - `new` keyword (overrides inherited properties).
        ///        - `[MagicScaffoldExclude]` (removes properties).
        ///        - `[MagicScaffoldInclude]` (brings virtual properties back when needed).
        ///    - **Methods**: 
        ///      - By default, methods are **not included** unless they have `[MagicScaffoldInclude]`.
        ///    - **Class Attributes**:
        ///      - Collects all attributes except:
        ///        - `[MetadataType]` (handled separately).
        ///        - Attributes explicitly listed in `excludedAttributes`.
        ///    - **Generic Constraints**:
        ///      - Merges `where T : ...` constraints from all connected classes, ensuring no duplicates.
        /// 
        /// 3. **Process MetadataType Attributes**:
        ///    - Identifies `[MetadataType]` attributes on **all collected class declarations**.
        ///    - Finds the referenced **metadata type class**.
        ///    - Extracts matching properties and **transfers attributes** from metadata properties to the flattened class properties.
        /// 
        /// 4. **Handle Using Statements**:
        ///    - Collects all **required usings** from:
        ///      - The original class and all its connected classes.
        ///      - The manually provided `manualUsings` list.
        ///      - Any **inferred dependencies** from properties and attributes.
        ///    - Ensures no duplicate using statements.
        /// 
        /// 5. **Build the Final Flattened Class**:
        ///    - Constructs the class definition with:
        ///      - **All required using statements**.
        ///      - **Namespace declaration** (`newNamespace`).
        ///      - **Merged class attributes**.
        ///      - **Interfaces provided in `interfaces`**.
        ///      - **Flattened properties and included methods**.
        ///      - **Generic constraints merged properly**.
        ///      - **Manually provided attributes (`additionalAttributes`)**.
        /// 
        /// **Additional Notes:**
        /// - Properties marked `virtual` are **skipped unless explicitly included**.
        /// - `[MagicScaffoldExclude]` on a class stops processing at that level.
        /// - Methods are **never included** unless explicitly marked `[MagicScaffoldInclude]`.
        /// - Attributes are **deduplicated** to avoid redundant entries.
        /// - If a new property overrides an excluded one (`new` keyword), it is **kept**.
        /// 
        /// **Parameters:**
        /// <param name="compilation">The Roslyn Compilation representing the entire project.</param>
        /// <param name="classDeclaration">The main class declaration to flatten.</param>
        /// <param name="newNamespace">The namespace to assign to the flattened class.</param>
        /// <param name="newClassName">The name of the final flattened class.</param>
        /// <param name="interfaces">A list of interfaces to be implemented by the flattened class.</param>
        /// <param name="manualUsings">A list of additional using statements to be included in the flattened class.</param>
        /// <param name="excludedAttributes">A list of attribute types that should be explicitly removed from the final flattened class.</param>
        /// <param name="additionalAttributes">A list of additional attributes to manually add to the flattened class.</param>
        /// 
        /// <returns>A string representation of the fully flattened class definition.</returns>

        public static string BuildFlattenedClass(
            Compilation compilation,
            ClassDeclarationSyntax classDeclaration,
            string newNamespace,
            string newClassName,
            List<string> interfaces,
            List<string> manualUsings,
            List<INamedTypeSymbol> excludedAttributes,
            List<string> additionalAttributes)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var partialClasses = AnalyzerHelper.GetPartialClassDeclarations(compilation, classDeclaration);
            var allConnectedClasses = new HashSet<ClassDeclarationSyntax>();

            // Recursively collect all connected classes (including base class extensions)
            CollectConnectedClasses(partialClasses, allConnectedClasses, compilation, semanticModel);

            // Process class properties, attributes, and methods
            var properties = new Dictionary<string, PropertyDeclarationSyntax>();
            var methods = new List<MethodDeclarationSyntax>();
            var classAttributes = new HashSet<AttributeSyntax>();
            var genericConstraints = new HashSet<string>();
            var requiredUsings = new HashSet<string>(manualUsings??new List<string>());

            foreach (var partialClass in allConnectedClasses)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(partialClass);
                if (classSymbol == null || classSymbol.GetAttributes().Any(attr => attr.AttributeClass?.ToString() == "Magic.Truth.Toolkit.Attributes.MagicScaffoldExcludeAttribute"))
                    continue; // Skip excluded classes

                // Collect usings
                var root = partialClass.SyntaxTree.GetRoot();
                var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name.ToString());
                foreach (var u in usingDirectives) requiredUsings.Add(u);

                // Collect class attributes (excluding MetadataType)
                foreach (var attr in partialClass.AttributeLists.SelectMany(a => a.Attributes))
                {
                    var attrSymbol = semanticModel.GetSymbolInfo(attr).Symbol?.ContainingType;
                    if (attrSymbol != null && attrSymbol.ToString() != "System.ComponentModel.DataAnnotations.MetadataTypeAttribute")
                    {
                        if (!excludedAttributes.Any(e => e.ToString() == attrSymbol.ToString()))
                        {
                            classAttributes.Add(attr);
                        }
                    }
                }

                // Collect generic constraints
                if (partialClass.ConstraintClauses.Any())
                {
                    foreach (var constraint in partialClass.ConstraintClauses)
                    {
                        var constraintText = constraint.ToFullString();
                        genericConstraints.Add(constraintText);
                    }
                }

                // Collect properties
                foreach (var prop in partialClass.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var propSymbol = semanticModel.GetDeclaredSymbol(prop);
                    if (propSymbol == null) continue;

                    if (prop.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)) &&
                        !propSymbol.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Magic.Truth.Toolkit.Attributes.MagicScaffoldIncludeAttribute"))
                        continue; // Skip virtual unless explicitly included

                    if (propSymbol.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Magic.Truth.Toolkit.Attributes.MagicScaffoldExcludeAttribute"))
                        continue; // Skip if excluded

                    var propName = prop.Identifier.Text;
                    if (!properties.ContainsKey(propName) || prop.Modifiers.Any(m => m.IsKind(SyntaxKind.NewKeyword)))
                    {
                        properties[propName] = prop;
                    }
                }

                // Collect methods if they have [MagicScaffoldInclude]
                foreach (var method in partialClass.Members.OfType<MethodDeclarationSyntax>())
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                    if (methodSymbol != null &&
                        methodSymbol.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Magic.Truth.Toolkit.Attributes.MagicScaffoldIncludeAttribute"))
                    {
                        methods.Add(method);
                    }
                }
            }

            // Process metadata type attributes and bring over attributes
            var metadataClasses = allConnectedClasses
                .SelectMany(c => c.AttributeLists.SelectMany(a => a.Attributes))
                .Where(attr => semanticModel.GetSymbolInfo(attr).Symbol?.ContainingType?.ToString() == "System.ComponentModel.DataAnnotations.MetadataTypeAttribute")
                .Select(attr => attr.ArgumentList.Arguments.FirstOrDefault()?.Expression as TypeOfExpressionSyntax)
                .Where(typeOfExpr => typeOfExpr != null)
                .Select(typeOfExpr => semanticModel.GetTypeInfo(typeOfExpr.Type).Type as INamedTypeSymbol)
                .Where(metadataType => metadataType != null)
                .Select(metadataType => metadataType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax)
                .Where(metadataClass => metadataClass != null)
                .ToList();

            foreach (var metadataClass in metadataClasses)
            {
                foreach (var metaProp in metadataClass.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var propName = metaProp.Identifier.Text;
                    if (properties.TryGetValue(propName, out var existingProp))
                    {
                        var newAttributes = metaProp.AttributeLists.SelectMany(a => a.Attributes);
                        foreach (var attr in newAttributes)
                        {
                            if (!existingProp.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToString() == attr.ToString()))
                            {
                                existingProp = existingProp.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)));
                                properties[propName] = existingProp;
                            }
                        }
                    }
                }
            }

            // Construct final class string
            var sb = new StringBuilder();
            foreach (var u in requiredUsings) sb.AppendLine($"using {u};");
            sb.AppendLine($"\nnamespace {newNamespace}\n{{");


            if (classAttributes.Any())
            {
                sb.AppendLine($"    [{string.Join(", ", classAttributes)}]");
            }


            string interfacePart = (interfaces != null && interfaces.Any()) ? $" : {string.Join(", ", interfaces)}" : "";
            string constraintPart = (genericConstraints != null && genericConstraints.Any()) ? $" {string.Join(" ", genericConstraints)}" : "";

            sb.AppendLine($"    public class {newClassName}{interfacePart}{constraintPart}");
            sb.AppendLine("    {");

            foreach (var prop in properties.Values) sb.AppendLine($"        {prop.ToFullString()}");

            foreach (var method in methods) sb.AppendLine($"        {method.ToFullString()}");

            sb.AppendLine("    }\n}");

            return sb.ToString();
        }

        private static void CollectConnectedClasses(
    IEnumerable<ClassDeclarationSyntax> classDeclarations,
    HashSet<ClassDeclarationSyntax> collectedClasses,
    Compilation compilation,
    SemanticModel semanticModel)
        {
            foreach (var classDeclaration in classDeclarations)
            {
                if (collectedClasses.Contains(classDeclaration))
                    continue; // Prevent duplicate processing

                collectedClasses.Add(classDeclaration);

                INamedTypeSymbol? classSymbol = null;

                try
                {
                    classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get symbol for class: {classDeclaration.Identifier.ValueText}");
                    Console.WriteLine($"Error: {ex.Message}");
                    continue; // ✅ Skip the failing class instead of crashing
                }

                if (classSymbol == null) continue;

                // Collect base class if it's in the same compilation
                if (classSymbol.BaseType != null &&
                    classSymbol.BaseType.SpecialType != SpecialType.System_Object &&
                    classSymbol.BaseType.ContainingAssembly == compilation.Assembly)
                {
                    var baseClassSyntax = classSymbol.BaseType.DeclaringSyntaxReferences
                        .Select(r => r.GetSyntax() as ClassDeclarationSyntax)
                        .Where(c => c != null)
                        .ToList();

                    if (baseClassSyntax.Count > 0)
                    {
                        CollectConnectedClasses(baseClassSyntax, collectedClasses, compilation, semanticModel);
                    }
                }

                // Collect all partial class declarations (using our helper method)
                var partialClassDeclarations = AnalyzerHelper.GetPartialClassDeclarations(compilation, classDeclaration);
                foreach (var partialClass in partialClassDeclarations)
                {
                    if (!collectedClasses.Any(c => c.Identifier.ValueText == partialClass.Identifier.ValueText))
                    {
                        collectedClasses.Add(partialClass);
                    }
                }
            }
        }

        /*private static void CollectConnectedClasses(
    IEnumerable<ClassDeclarationSyntax> classDeclarations,
    HashSet<ClassDeclarationSyntax> collectedClasses,
    Compilation compilation,
    SemanticModel semanticModel)
        {
            foreach (var classDeclaration in classDeclarations)
            {
                if (collectedClasses.Contains(classDeclaration))
                    continue; // Prevent duplicate processing

                collectedClasses.Add(classDeclaration);

                // Get the class symbol
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (classSymbol == null) continue;

                // Collect any base class extensions (excluding object)
                if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
                {
                    var baseClassSyntax = classSymbol.BaseType.DeclaringSyntaxReferences
                        .Select(r => r.GetSyntax() as ClassDeclarationSyntax)
                        .Where(c => c != null)
                        .ToList();

                    try
                    {
                        CollectConnectedClasses(baseClassSyntax, collectedClasses, compilation, semanticModel);
                    }
                    catch(Exception ex)
                    {

                    }
                }

                // Collect all partial class declarations (using our helper method)
                var partialClassDeclarations = AnalyzerHelper.GetPartialClassDeclarations(compilation, classDeclaration);
                foreach (var partialClass in partialClassDeclarations)
                {
                    if (!collectedClasses.Contains(partialClass))
                        collectedClasses.Add(partialClass);
                }
            }
        }*/

    }
}
