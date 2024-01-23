using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace PropertyGenerator
{
    [Generator]
    public class GeneratorThing : IIncrementalGenerator
    {
        private const string PropertyAttributeName = "UnrealLib.Experimental.UnObj.DefaultProperties.PropertyAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s is FieldDeclarationSyntax f && f.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
                ).Where(static m => m is not null);

            var compilation = context.CompilationProvider.Combine(provider.Collect());

            context.RegisterSourceOutput(compilation, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<FieldDeclarationSyntax> fields, SourceProductionContext context)
        {
            if (fields.IsDefaultOrEmpty) return;

            context.AddSource("TEST.g.cs", "TESTING!");
        }

        private static FieldDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // Predicate enforces this
            var fieldDeclarationSyntax = (FieldDeclarationSyntax)context.Node;

            // Look for PropertyAttribute in this field's list of attributes
            foreach (AttributeListSyntax attributeListSyntax in fieldDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    IFieldSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IFieldSymbol;

                    if (attributeSymbol is not null && attributeSymbol.Name == PropertyAttributeName)
                    {
                        return fieldDeclarationSyntax.Parent as FieldDeclarationSyntax;
                    }
                }
            }

            // PropertyAttribute was not found on this field
            return null;
        }
    }
}
