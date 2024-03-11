using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace PropertyGenerator
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        // @TODO:
        // Aliases to redirect deprecated UProperties
        // Attribute field: pointer. To support object indexes/pointer UC vars
        // Subclass support. Currently disabled for GroupFields() and not implemented for actual source generation
        
        // @TODO new:
        // Round up all bool fields and place them at the top, before everything else. This will mimic the bitfield generation UE3 does and ensure correct order for best runtime performance

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Debugger.Launch();

            context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("UPropertyAttribute.g.cs", SourceText.From(SourceGenerationHelper.AttributeData, Encoding.UTF8)));
            // context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("CollectionExtensions.g.cs", SourceText.From(SourceGenerationHelper.CollectionExtensionsData, Encoding.UTF8)));

            var fields = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    SourceGenerationHelper.AttributeName,
                    static (_, _) => true, // Guaranteed to be VariableDeclatorSytnax
                    GetFieldInfo)
                .Collect();

            // This anonymous function is really messy debug stage
            // Move into its own method(s)
            context.RegisterSourceOutput(fields, (spc, source) =>
            {
                var classes = GroupFields(source);

                foreach (var classGroup in classes)
                {
                    spc.AddSource($"{classGroup.Header.Namespace}.{classGroup.Header.Name}.g.cs", GenerateClass(classGroup).ToString());
                }
            });
        }

        // Maybe use a dictionary which is set up during first pass, then reference during hierarchy construction
        private List<ClassContainer> GroupFields(ImmutableArray<FieldInfo> fields)
        {
            List<ClassContainer> classes = [];
            var span = fields.AsSpan();

            // Construct base classes list -- no hierarchies yet

            int prev = 0;
            for (int cur = 1; cur <= fields.Length; cur++)
            {
                if (cur == fields.Length || fields[cur].Class != fields[prev].Class)
                {
                    classes.Add(new(fields[prev].Class, span.Slice(prev, cur - prev).ToArray()));
                    prev = cur;
                }
            }

            // Construct hierarchies

            for (int cur = 0; cur < classes.Count; cur++)
            {
                if (classes[cur].Header.Parent == "") continue;

                throw new InvalidOperationException("Subclasses are not currently supported");

                ClassContainer? target = null;

                // Look for the first class sharing the same namespace
                foreach (var parent in classes)
                {
                    if (parent.Header.Namespace == classes[cur].Header.Namespace)
                    {
                        if (parent.Header.Name == classes[cur].Header.Parent)
                        {
                            // Found it.
                            target = parent;
                            goto found;
                        }

                        if (parent.Children is not null)
                        {
                            foreach (var child in parent.Children)
                            {
                                if (child.Header.Name == classes[cur].Header.Parent)
                                {
                                    // Found it.
                                    target = child;
                                    goto found;
                                }
                            }
                        }
                    }
                }

                // This happens if a class' parent is not partial or doesn't implement any UProperty fields.
                // The IDE will error if it's the former. If it's the latter, we SHOULD add a new class without any fields

                // Unfortunately we can't tell if this class to be added is a child itself--throw an error for now. Maybe implement in future
                throw new InvalidOperationException($"'{classes[cur].Header.GetFullName()}' is a child of a UProperty-less or non-partial class!");

                found:
                target.Children ??= [];
                target.Children.Add(classes[cur]);
                classes.RemoveAt(cur);
                cur--;
            }

            return classes;
        }

        private static StringBuilder GenerateClass(ClassContainer classGroup)
        {
            var sb = new StringBuilder("using UnrealLib.Experimental.UnObj.DefaultProperties;\n");

            GenerateClassUsings(classGroup.Fields, classGroup.Header.Namespace, sb);

            sb.Append($"namespace {classGroup.Header.Namespace};\n\n");
            sb.Append($"public partial class {classGroup.Header.Name}\n");
            sb.Append("{");

            GenerateClassDefaults(classGroup.Fields, sb);

            sb.Append('\n');
            GenerateMethod_ParseProperty(classGroup.Fields, sb);
            sb.Append('\n');
            GenerateMethod_WriteProperties(classGroup.Fields, sb);

            sb.Append("\n}\n");

            // string final = sb.ToString();

            return sb;
        }

        private static void GenerateClassUsings(FieldInfo[] fields, string classNamespace, StringBuilder sb)
        {
            List<string> addedNamespaces = [];

            foreach (var field in fields)
            {
                if (field.Type.Namespace.Length > 0 && !addedNamespaces.Contains(field.Type.Namespace) && field.Type.Namespace != classNamespace)
                {
                    sb.Append($"using {field.Type.Namespace};\n");
                    addedNamespaces.Add(field.Type.Namespace);
                }

                if (field.Type.Type is EType.Enum && !addedNamespaces.Contains("System.Diagnostics"))
                {
                    sb.Append("using System.Diagnostics;\n");
                    addedNamespaces.Add("System.Diagnostics");
                }
            }

            if (addedNamespaces.Count > 0) sb.Append('\n');
        }

        private static void GenerateClassDefaults(FieldInfo[] fields, StringBuilder sb)
        {
            bool addedAtLeastOneDefault = false;

            foreach (var field in fields)
            {
                // If reference type or struct, create a "constant" field
                if (field.ShouldHaveBackingField())
                {
                    string accessor = field.Type.Type is EType.String ? "const" : "static readonly";
                    sb.Append($"\n\tprivate {accessor} {field.Type.Name} Default__{field.Name} = {field.DefaultValue};");

                    addedAtLeastOneDefault = true;
                }
            }

            if (addedAtLeastOneDefault) sb.Append('\n');
        }

        #region Method generators

        private static void GenerateMethod_ParseProperty(FieldInfo[] fields, StringBuilder sb)
        {
            sb.Append("\tinternal override void ParseProperty(UnrealPackage Ar, FPropertyTag tag)\n");
            sb.Append("\t{\n");

            sb.Append("\t\tswitch (tag.Name.ToString())\n");
            sb.Append("\t\t{");

            foreach (var field in fields)
            {
                string index = field.Type.IsArray ? "[tag.ArrayIndex]" : "";

                sb.Append($"\n\t\t\tcase nameof({field.Name}):\n");
                
                if (field.Type.Type is EType.Bool)
                {
                    sb.Append($"\t\t\t\t{field.Name}{index} = tag.Value.Bool;\n");
                }
                else if (field.Type.Type is EType.Enum)
                {
                    sb.Append("\t\t\t\tAr.Serialize(ref tag.Value.Name);\n");
                    sb.Append($"\t\t\t\tDebug.Assert({field.Type.Name}Extensions.TryParse(tag.Value.Name.ToString(), out {field.Name}{index}));\n");
                }
                else if (field.Type.Type is EType.Class)
                {
                    sb.Append(field.Type.IsUPropertyHolder
                        ? $"\t\t\t\t{field.Name}{index}.SerializeScriptProperties(Ar);\n"
                        : $"\t\t\t\t{field.Name}{index}.Serialize(Ar);\n");
                }
                else
                {
                    if (field.Type.IsArrayProperty)
                    {
                        sb.Append($"\t\t\t\tAr.Serialize(ref {field.Name}, tag.ArraySize);\n");
                    }
                    else
                    {
                        sb.Append($"\t\t\t\tAr.Serialize(ref {field.Name}{index});\n");
                    }
                }

                sb.Append("\t\t\t\tbreak;\n");
            }

            sb.Append("\n\t\t\tdefault:\n");
            sb.Append("\t\t\t\tbase.ParseProperty(Ar, tag);\n");
            sb.Append("\t\t\t\tbreak;\n");

            sb.Append("\t\t}\n");

            sb.Append("\t}\n");
        }

        private static void GenerateMethod_WriteProperties(FieldInfo[] fields, StringBuilder sb)
        {
            sb.Append("\n\tinternal override void WriteProperties(UnrealPackage Ar)\n");
            sb.Append("\t{");

            foreach (var field in fields)
            {
                string index = field.Type.IsArray ? "[i]" : "";

                string compareString;
                if (field.ShouldHaveBackingField())
                {
                    compareString = $"!= Default__{field.Name}";
                }
                else
                {
                    compareString = field.IsReferenceType()
                        ? "is not null"
                        : field.HasDefaultValue() ? $"!= {field.DefaultValue}" : "!= default";
                }

                string expr = $"{field.Name}{index} {compareString}{index}";

                string action = field.Type.Type switch
                {
                    EType.Class when field.Type.IsNative => $"FPropertyTag.WriteNew(Ar, nameof({field.Name}), {(field.Type.IsArray ? "i" : "0")}, {field.Name}{index}{(field.Type.IsObjectProperty ? ", true" : "")});",
                    EType.Class when field.Type.IsUPropertyHolder => $"FPropertyTag.WriteNew(Ar, nameof({field.Name}), nameof({field.Type.NameWithoutArrayHack()}), {(field.Type.IsArray ? "i" : "0")}, {field.Name}{index});",
                    EType.Class or EType.Struct => $"FPropertyTag.WriteNew(Ar, nameof({field.Name}), nameof({field.Type.NameWithoutArrayHack()}), {(field.Type.IsArray ? "i" : "0")}, {field.Name}{index});",
                    EType.Enum => $"FPropertyTag.WriteNew(Ar, nameof({field.Name}), nameof({field.Type.Name}), {(field.Type.IsArray ? "i" : "0")}, {field.Type.Name}Extensions.ToStringFast({field.Name}));",
                    _ => $"FPropertyTag.WriteNew(Ar, nameof({field.Name}), {(field.Type.IsArray ? "i" : "0")}, {field.Name}{index}{(field.Type.IsObjectProperty ? ", true" : "")});"
                };

                if (field.Type.IsArray)
                {
                    sb.Append($"\n\t\tif ({field.Name} is not null)\n");
                    sb.Append("\t\t{\n");

                    if (field.Type.IsArrayProperty)
                    {
                        sb.Append($"\t\t\tFPropertyTag.WriteNew(Ar, nameof({field.Name}), {field.Name});\n");
                    }
                    else
                    {
                        if (field.HasDefaultValue())
                        {
                            sb.Append($"\t\t\tint defaultLength = Default__{field.Name}?.Length ?? -1;\n");
                        }

                        sb.Append($"\t\t\tfor (int i = 0; i < {field.Name}.Length; i++)\n");
                        sb.Append("\t\t\t{\n");

                        if (field.HasDefaultValue())
                        {
                            sb.Append($"\t\t\t\tif (i >= defaultLength || {expr})\n");
                            sb.Append("\t\t\t\t{\n");
                            sb.Append($"\t\t\t\t\t{action}\n");
                            sb.Append("\t\t\t\t}\n");
                        }
                        else
                        {
                            // @TODO null check the values here?
                            sb.Append($"\t\t\t\t{action}\n");
                        }

                        sb.Append("\t\t\t}\n");
                    }

                    sb.Append("\t\t}\n");
                }
                else
                {
                    sb.Append($"\n\t\tif ({expr})\n");
                    sb.Append("\t\t{\n");
                    sb.Append($"\t\t\t{action}\n");
                    sb.Append("\t\t}\n");
                }
            }

            sb.Append("\n\t\tbase.WriteProperties(Ar);\n");

            sb.Append("\t}");
        }

        #endregion

        #region Helpers

        private static FieldInfo GetFieldInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var typeNode = (VariableDeclaratorSyntax)context.TargetNode;
            var classNode = (ClassDeclarationSyntax)typeNode.Parent.Parent.Parent;

            var classInfo = GetClassInfo(classNode);
            var typeInfo = GetTypeInfo(context, typeNode);

            string fieldName = typeNode.Identifier.ValueText;
            string fieldValue = typeNode.Initializer?.Value.ToString() ?? "";

            return new FieldInfo(classInfo, fieldName, typeInfo, fieldValue);
        }

        private static ClassInfo GetClassInfo(ClassDeclarationSyntax rootClass)
        {
            // Skip over parents until we reach the first non-class node
            SyntaxNode? currentNode = rootClass;
            while ((currentNode = currentNode.Parent) is ClassDeclarationSyntax) ;

            // Get class' namespace
            string namespace_ = currentNode switch
            {
                FileScopedNamespaceDeclarationSyntax fsnds => fsnds.Name.ToString(),
                NamespaceDeclarationSyntax nsds => nsds.Name.ToString(),
                _ => ""
            };

            // Get class' parent if it exists as a subclass
            string parent = rootClass.Parent is ClassDeclarationSyntax cds ? cds.Identifier.Text : "";

            return new ClassInfo(rootClass.Identifier.Text, parent, namespace_);
        }

        private static TypeInfo GetTypeInfo(GeneratorAttributeSyntaxContext context, VariableDeclaratorSyntax typeNode)
        {
            var typeSyntax = ((VariableDeclarationSyntax)typeNode.Parent).Type;
            var typeString = typeSyntax.ToString();

            int index = typeString.IndexOf('[') - 1;
            bool isArray = index != -2;

            bool isNative = false;
            bool isUPropertyHolder = false;
            bool isArrayProperty = false;
            bool isObjectProperty = false;

            // Ensure index points to the last char of the type string, excluding the null terminator.
            // Null-forgiving operator '!' is not valid for field declarations
            if (!isArray)
            {
                index = typeString.Length - 1;
                if (typeString[index] == '?')
                {
                    index--;
                }
            }

            // Check for UProperty arguments
            if (typeNode.Parent.Parent is FieldDeclarationSyntax fieldDeclaration && fieldDeclaration.AttributeLists.Count > 0)
            {
                foreach (var attributeList in fieldDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString() == "UProperty" && attribute.ArgumentList is not null)
                        {
                            foreach (var argument in attribute.ArgumentList.Arguments)
                            {
                                if (argument.NameEquals.Name.Identifier.Text == "ArrayProperty")
                                {
                                    isArrayProperty = argument.Expression.ToString() == "true";
                                }
                                else if (argument.NameEquals.Name.Identifier.Text == "ObjectProperty")
                                {
                                    isObjectProperty = argument.Expression.ToString() == "true";
                                }
                            }
                        }
                    }
                }
            }

            EType eType = typeString.AsSpan(0, index + 1) switch
            {
                "bool" => EType.Bool,
                "byte" => EType.Byte,
                "int" => EType.Int,
                "float" => EType.Float,
                "string" => EType.String,
                "FName" => EType.Name,  // @TODO see if FName can be treated as a struct
                "FGuid" or "Color" or "LinearColor" or "FVector2" or "Vector" or "FVector4" or "Rotator" => EType.Struct,
                _ => 0
            };

            // All common structs should live under the UnrealLib.Core namespace
            string namespaceString = eType is EType.Struct or EType.Name ? "UnrealLib.Core" : "";

            // If the type was not any of the above common types, access semantic model
            if (eType == 0)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax);
                var typeSymbol = isArray ? ((IArrayTypeSymbol)typeInfo.Type).ElementType : typeInfo.Type;

                eType = typeSymbol.TypeKind switch
                {
                    TypeKind.Enum => EType.Enum,
                    TypeKind.Class => EType.Class,
                    TypeKind.Struct => EType.Struct // throw new Exception("Struct should be added to common table above!")
                };

                namespaceString = typeSymbol.ContainingNamespace.ToString();

                if (namespaceString == "UnrealLib.Core")
                {
                    throw new Exception($"Should '{namespaceString}.{typeString}' live outside UnrealLib.Core?");
                }

                if (eType is EType.Class)
                {
                    isNative = ((INamedTypeSymbol)typeSymbol).AllInterfaces.Any(i => i.Name == "ISerializable");

                    var current = typeSymbol;
                    while ((current = current.BaseType) is not null)
                    {
                        if (current.ToString() == "UnrealLib.Experimental.UnObj.DefaultProperties.PropertyHolder")
                        {
                            isUPropertyHolder = true;
                            break;
                        }
                    }         
                }
            }

            return new TypeInfo(namespaceString, typeString, eType, isArray, isNative, isUPropertyHolder, isArrayProperty, isObjectProperty);
        }

        #endregion
    }

    record class ClassContainer
    {
        public ClassInfo Header;
        public FieldInfo[] Fields;
        public List<ClassContainer> Children;

        public ClassContainer(ClassInfo classInfo, FieldInfo[] fields)
        {
            Header = classInfo;
            Fields = fields;
        }

        public override string ToString() => Header.GetFullName();
    }

    #region Structs

    enum EType
    {
        // 0 is reserved for "nothing"

        Bool = 1,
        Byte,
        Int,
        Float,
        Enum,
        Struct,

        String,
        Name,       // FName is a reference type until I pull finger @TODO FName should be a struct
        Class,
    }

    readonly record struct TypeInfo
    {
        public readonly string Namespace;
        public readonly string Name;
        public readonly EType Type;
        public readonly bool IsArray;
        public readonly bool IsNative; // Whether class uses binary serialization or property tags (name suffix 'F' or 'U')
        public readonly bool IsUPropertyHolder;
        public readonly bool IsArrayProperty;
        public readonly bool IsObjectProperty;

        // Yuck
        public readonly string NameWithoutArrayHack()
        {
            int index;

            if ((index = Name.IndexOf('[')) == -1)
            {
                if ((index = Name.IndexOf('?')) == -1)
                {
                    index = Name.Length;
                }
            }

            return Name.Substring(0, index);
        }

        public readonly bool IsValueType => Type <= EType.Struct;

        public TypeInfo(string nameSpace, string name, EType flags, bool isArray, bool isNative, bool isUPropertyHolder, bool isArrayProperty, bool isObjectProperty)
        {
            Name = name;
            Namespace = nameSpace;
            Type = flags;
            IsArray = isArray;
            IsNative = isNative;
            IsUPropertyHolder = isUPropertyHolder;
            IsArrayProperty = isArrayProperty;
            IsObjectProperty = isObjectProperty;
        }

        public readonly override string ToString() => $"[{Type}] {Name}";
    }

    readonly record struct FieldInfo
    {
        public readonly ClassInfo Class;
        public readonly TypeInfo Type;

        public readonly string Name;
        public readonly string DefaultValue;

        // Excluding strings from reference types for now
        public readonly bool HasDefaultValue() => DefaultValue.Length > 0;
        public readonly bool IsReferenceType() => Type.IsArray || Type.Type is (EType.Class);
        public readonly bool ShouldHaveBackingField() => HasDefaultValue() && (Type.Type is (EType.Class or EType.Struct or EType.String) || Type.IsArray);

        public FieldInfo(ClassInfo classInfo, string fieldName, TypeInfo type, string fieldDefault)
        {
            Class = classInfo;
            Name = fieldName;
            Type = type;
            DefaultValue = fieldDefault;
        }

        public readonly override string ToString() => $"{Type.Name} {Name}";
    }

    readonly record struct ClassInfo
    {
        public readonly string Namespace;
        public readonly string Name;
        public readonly string Parent;

        public readonly string GetFullName() => $"{Namespace}.{Name}";
        public readonly override string ToString() => GetFullName();

        public ClassInfo(string name, string parent, string namespaceString)
        {
            Name = name;
            Parent = parent;
            Namespace = namespaceString;
        }
    }

    #endregion
}
