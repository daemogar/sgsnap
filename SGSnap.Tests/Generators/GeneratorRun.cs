using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SGSnap.Tests.Generators;

static class GeneratorRun
{
	public static GeneratorDriver Driver(string source, IIncrementalGenerator? generator = null)
		=> CSharpGeneratorDriver.Create(generator ?? new FixtureGenerator())
			.RunGenerators(CSharpCompilation.Create("Fixture", [CSharpSyntaxTree.ParseText(source)]));
}
