using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit.Extensions
{
    public class DynamicTypeBuilder
    {
        private readonly AssemblyName _assemblyName = new("DynamicGeneratedTypes");
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        public DynamicTypeBuilder()
        {
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name);
        }

        public TypeBuilder CreateType(ClassDeclarationSyntax classDeclaration)
        {
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                classDeclaration.Identifier.Text, TypeAttributes.Public | TypeAttributes.Class);

            foreach (var member in classDeclaration.Members)
            {
                if (member is PropertyDeclarationSyntax property)
                {
                    AddProperty(typeBuilder, property);
                }
            }

            return typeBuilder;
        }

        private void AddProperty(TypeBuilder typeBuilder, PropertyDeclarationSyntax property)
        {
            string propertyName = property.Identifier.Text;
            Type propertyType = Type.GetType(property.Type.ToString()) ?? typeof(object);

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            MethodBuilder getMethod = typeBuilder.DefineMethod(
                "get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType, Type.EmptyTypes);

            ILGenerator ilGen = getMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGen.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethod);
        }
    }
}
