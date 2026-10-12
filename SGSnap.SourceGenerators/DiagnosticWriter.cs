using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SGSnap.SourceGenerators;

/// <summary>Renders generator diagnostics in the snapshot text format, in the order given.</summary>
internal static class DiagnosticWriter
{
	public static string Write(ImmutableArray<Diagnostic> diagnostics)
		=> "{\n  Diagnostics: [\n" + string.Join(",\n", diagnostics.Select(WriteDiagnostic)) + "\n  ]\n}";

	static string WriteDiagnostic(Diagnostic diagnostic)
	{
		var members = new List<string>();
		if (diagnostic.Location != Location.None)
			members.Add("      Location: " + WriteLocation(diagnostic.Location));
		members.Add("      Message: " + diagnostic.GetMessage(CultureInfo.InvariantCulture));
		members.Add("      Severity: " + diagnostic.Severity);
		if (diagnostic.WarningLevel != 0)
			members.Add("      WarningLevel: " + diagnostic.WarningLevel.ToString(CultureInfo.InvariantCulture));
		members.Add("      Descriptor: " + WriteDescriptor(diagnostic.Descriptor));
		return "    {\n" + string.Join(",\n", members) + "\n    }";
	}

	static string WriteDescriptor(DiagnosticDescriptor descriptor)
	{
		if (descriptor.CustomTags.Any())
			throw new NotSupportedException($"Diagnostic {descriptor.Id} has custom tags; their snapshot format is not defined.");

		var members = new List<string>
		{
			"        Id: " + descriptor.Id,
			"        Title: " + descriptor.Title.ToString(CultureInfo.InvariantCulture),
		};

		var description = descriptor.Description.ToString(CultureInfo.InvariantCulture);
		if (!string.IsNullOrWhiteSpace(description))
			members.Add("        Description: " + description);
		if (!string.IsNullOrWhiteSpace(descriptor.HelpLinkUri))
			members.Add("        HelpLink: " + descriptor.HelpLinkUri);

		members.Add("        MessageFormat: " + descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));
		members.Add("        Category: " + descriptor.Category);
		members.Add("        DefaultSeverity: " + descriptor.DefaultSeverity);
		members.Add("        IsEnabledByDefault: " + (descriptor.IsEnabledByDefault ? "true" : "false"));
		return "{\n" + string.Join(",\n", members) + "\n      }";
	}

	static string WriteLocation(Location location)
	{
		var span = location.GetMappedLineSpan();
		var value = span.IsValid ? $"{span.Path.Replace('/', '\\')}: {span.Span}" : "";
		if (location.SourceTree is not { } tree)
			return value;

		return "/*\n" + Snippet(tree, location.SourceSpan, span) + "*/\n " + value;
	}

	/// <summary>The span's lines plus one line of context either side, each followed by a caret row where the span touches it.</summary>
	static string Snippet(SyntaxTree tree, TextSpan sourceSpan, FileLinePositionSpan span)
	{
		var lines = tree.GetText().Lines;
		var text = new StringBuilder();
		var first = Math.Max(span.StartLinePosition.Line - 1, 0);
		var last = Math.Min(span.EndLinePosition.Line + 1, lines.Count - 1);

		for (var index = first; index <= last; index++)
		{
			var line = lines[index];
			text.Append(line.ToString()).Append('\n');
			if (line.Span.Intersection(sourceSpan) is not { } overlap)
				continue;

			text.Append(' ', overlap.Start - line.Start).Append('^', overlap.Length).Append('\n');
		}

		return text.ToString();
	}
}
