using System.Security.Cryptography;
using System.Text;

namespace SGSnap;

/// <summary>Runs a CLI entry point in-process and renders exit code, streams and changed files as one text snapshot.</summary>
internal static class CliCapture
{
	static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static string Render(Func<TextWriter, TextWriter, int> run, string? filesRoot)
	{
		ArgumentNullException.ThrowIfNull(run);
		var before = filesRoot is null ? null : ReadTree(filesRoot);

		var stdout = new StringWriter();
		var stderr = new StringWriter();
		var exitCode = run(stdout, stderr);

		var text = new StringBuilder();
		text.Append("ExitCode: ").Append(exitCode).Append('\n');
		AppendSection(text, "--- stdout ---", stdout.ToString());
		AppendSection(text, "--- stderr ---", stderr.ToString());

		if (before is not null)
		{
			var after = ReadTree(filesRoot!);
			foreach (var (path, bytes) in after.OrderBy(entry => entry.Key, StringComparer.Ordinal))
				if (!before.TryGetValue(path, out var old) || !old.AsSpan().SequenceEqual(bytes))
					AppendSection(text, $"--- file: {path} ---", Describe(bytes));

			foreach (var path in before.Keys.Where(path => !after.ContainsKey(path)).Order(StringComparer.Ordinal))
				text.Append($"--- deleted: {path} ---\n");
		}

		return text.ToString();
	}

	static void AppendSection(StringBuilder text, string header, string body)
	{
		text.Append(header).Append('\n');
		var normalized = SnapshotText.Normalize(body);
		text.Append(normalized);
		if (normalized.Length > 0 && !normalized.EndsWith('\n'))
			text.Append('\n');
	}

	static Dictionary<string, byte[]> ReadTree(string root)
	{
		var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		if (!Directory.Exists(root))
			return files;

		foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
			files[Path.GetRelativePath(root, path).Replace('\\', '/')] = File.ReadAllBytes(path);
		return files;
	}

	static string Describe(byte[] bytes)
	{
		if (Array.IndexOf(bytes, (byte)0) < 0)
		{
			try
			{
				var start = bytes is [0xEF, 0xBB, 0xBF, ..] ? 3 : 0;
				return StrictUtf8.GetString(bytes, start, bytes.Length - start);
			}
			catch (DecoderFallbackException)
			{
			}
		}

		return $"(binary, {bytes.Length} bytes, sha256 {Convert.ToHexStringLower(SHA256.HashData(bytes))})";
	}
}
