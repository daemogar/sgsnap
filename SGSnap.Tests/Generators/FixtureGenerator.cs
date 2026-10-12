using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SGSnap.Tests.Generators;

/// <summary>Test generator: two fixed sources, and an error on every record that declares a primary constructor.</summary>
[Generator]
sealed class FixtureGenerator : IIncrementalGenerator
{
	public static readonly DiagnosticDescriptor PrimaryConstructor = new(
		"FX0001", "Record Primary Constructor", "Record `{0}` must not declare a primary constructor", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static post =>
		{
			post.AddSource("WidgetAttribute.cs", "namespace Fixture;\n\npublic sealed class WidgetAttribute : System.Attribute { }");
			post.AddSource("Helpers.g.cs", "namespace Fixture;\n\npublic static class Helpers { }\n");
		});

		var records = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is RecordDeclarationSyntax { ParameterList: not null },
			static (syntax, _) => (RecordDeclarationSyntax)syntax.Node);

		context.RegisterSourceOutput(records, static (output, record) =>
			output.ReportDiagnostic(Diagnostic.Create(PrimaryConstructor, record.ParameterList!.GetLocation(), record.Identifier.Text)));
	}
}

/// <summary>Test generator that fails, to prove generator exceptions surface.</summary>
sealed class ThrowingGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> context.RegisterSourceOutput(context.CompilationProvider, static (_, _) => throw new InvalidOperationException("boom"));
}
