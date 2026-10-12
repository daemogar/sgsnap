using System.Text;

namespace SGSnap;

/// <summary>Reads and writes snapshot text: compare loosely (no BOM, LF), write exactly (BOM, LF, content verbatim).</summary>
internal static class SnapshotText
{
	static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];
	static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

	/// <summary>The file's text without BOM and with LF line endings, or null when the file does not exist.</summary>
	public static string? ReadVerified(string path)
	{
		if (!File.Exists(path))
			return null;

		var bytes = File.ReadAllBytes(path);
		var start = bytes.AsSpan().StartsWith(Bom) ? Bom.Length : 0;
		return Normalize(Utf8.GetString(bytes, start, bytes.Length - start));
	}

	/// <summary>Writes the BOM, then <paramref name="normalized"/> exactly; callers pass text already normalized.</summary>
	public static void Write(string path, string normalized)
	{
		var body = Utf8.GetBytes(normalized);
		var bytes = new byte[Bom.Length + body.Length];
		Bom.CopyTo(bytes, 0);
		body.CopyTo(bytes, Bom.Length);
		File.WriteAllBytes(path, bytes);
	}
}
