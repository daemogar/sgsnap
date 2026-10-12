using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using SGSnap.SourceGenerators;

namespace SGSnap.Tests.Generators;

public class DiagnosticWriterTests
{
	static readonly DiagnosticDescriptor Trace = new("FX0000", "Trace", "{0}", "Logger", DiagnosticSeverity.Warning, isEnabledByDefault: true);

	static Location At(string source, string text, string path = "")
		=> Location.Create(CSharpSyntaxTree.ParseText(source, path: path), new TextSpan(source.IndexOf(text, StringComparison.Ordinal), text.Length));

	static string Write(params Diagnostic[] diagnostics) => DiagnosticWriter.Write([.. diagnostics]);

	[Fact]
	public void A_located_error_renders_context_lines_and_carets()
	{
		const string source = "namespace Fixture;\n\n[Widget]\npublic partial record Part(int Id);\n// end";
		var pad = new string(' ', 26);

		var text = Write(Diagnostic.Create(FixtureGenerator.PrimaryConstructor, At(source, "(int Id)"), "Part"));

		Assert.Equal($$"""
			{
			  Diagnostics: [
			    {
			      Location: /*
			[Widget]
			public partial record Part(int Id);
			{{pad}}^^^^^^^^
			// end
			*/
			 : (3,26)-(3,34),
			      Message: Record `Part` must not declare a primary constructor,
			      Severity: Error,
			      Descriptor: {
			        Id: FX0001,
			        Title: Record Primary Constructor,
			        MessageFormat: Record `{0}` must not declare a primary constructor,
			        Category: Design,
			        DefaultSeverity: Error,
			        IsEnabledByDefault: true
			      }
			    }
			  ]
			}
			""".ReplaceLineEndings("\n"), text);
	}

	[Fact]
	public void Diagnostics_keep_the_order_they_are_given_in()
	{
		var text = Write(
			Diagnostic.Create(Trace, Location.None, "pass complete"),
			Diagnostic.Create(FixtureGenerator.PrimaryConstructor, Location.None, "Part"));

		Assert.True(text.IndexOf("Id: FX0000", StringComparison.Ordinal) < text.IndexOf("Id: FX0001", StringComparison.Ordinal));
		Assert.Contains("""
			    {
			      Message: pass complete,
			      Severity: Warning,
			      WarningLevel: 1,
			      Descriptor: {
			""".ReplaceLineEndings("\n"), text);
	}

	[Fact]
	public void A_multi_line_span_gets_a_caret_row_under_each_line()
	{
		const string source = "a\nbcd\nef\ng";
		var location = Location.Create(CSharpSyntaxTree.ParseText(source), TextSpan.FromBounds(3, 7));

		var text = Write(Diagnostic.Create(FixtureGenerator.PrimaryConstructor, location, "Part"));

		Assert.Contains("""
			      Location: /*
			a
			bcd
			 ^^
			ef
			^
			g
			*/
			 : (1,1)-(2,1),
			""".ReplaceLineEndings("\n"), text);
	}

	[Fact]
	public void Description_and_help_link_appear_between_title_and_message_format()
	{
		var descriptor = new DiagnosticDescriptor("FX0002", "T", "M", "C", DiagnosticSeverity.Info, true, description: "Longer text", helpLinkUri: "https://example.com/FX0002");

		var text = Write(Diagnostic.Create(descriptor, Location.None));

		Assert.Contains("        Title: T,\n        Description: Longer text,\n        HelpLink: https://example.com/FX0002,\n        MessageFormat: M,", text);
	}

	[Fact]
	public void Custom_tags_are_not_supported()
	{
		var descriptor = new DiagnosticDescriptor("FX0003", "T", "M", "C", DiagnosticSeverity.Warning, true, customTags: ["Tagged"]);

		Assert.Throws<NotSupportedException>(() => Write(Diagnostic.Create(descriptor, Location.None)));
	}

	[Fact]
	public void A_tree_path_is_written_with_backslashes()
	{
		var text = Write(Diagnostic.Create(FixtureGenerator.PrimaryConstructor, At("class A { }", "class", "src/A.cs"), "A"));

		Assert.Contains("*/\n src\\A.cs: (0,0)-(0,5),", text);
	}
}
