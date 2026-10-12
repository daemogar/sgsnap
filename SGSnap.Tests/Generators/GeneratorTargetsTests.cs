using SGSnap.SourceGenerators;

namespace SGSnap.Tests.Generators;

public class GeneratorTargetsTests
{
	[Fact]
	public void Diagnostics_come_first_then_sources_in_ordinal_hint_order()
	{
		var targets = GeneratorTargets.From(GeneratorRun.Driver("public partial record Part(int Id);").GetRunResult());

		Assert.Equal(new[] { "|txt", "Helpers.g|cs", "WidgetAttribute|cs" }, targets.Select(target => $"{target.Name}|{target.Extension}"));
		Assert.Equal("//HintName: WidgetAttribute.cs\nnamespace Fixture;\n\npublic sealed class WidgetAttribute : System.Attribute { }", targets[2].Content);
	}

	[Fact]
	public void No_diagnostics_means_no_txt_target()
	{
		var targets = GeneratorTargets.From(GeneratorRun.Driver("public class Plain { }").GetRunResult());

		Assert.Equal(new[] { "Helpers.g|cs", "WidgetAttribute|cs" }, targets.Select(target => $"{target.Name}|{target.Extension}"));
	}

	[Fact]
	public void A_generator_exception_is_rethrown()
	{
		var error = Assert.ThrowsAny<Exception>(() => GeneratorTargets.From(GeneratorRun.Driver("", new ThrowingGenerator()).GetRunResult()));

		Assert.Contains("boom", error.ToString());
	}
}
