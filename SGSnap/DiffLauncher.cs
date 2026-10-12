using System.ComponentModel;
using System.Text;

namespace SGSnap;

/// <summary>Opens failing snapshots in a diff tool: best-effort, non-blocking, capped per test run.</summary>
internal sealed class DiffLauncher(IProcessStarter starter, Func<string, string?> getEnvironment)
{
	public const string DefaultTool = "code --diff \"{received}\" \"{verified}\"";
	const string DefaultNewFileTool = "code \"{received}\"";

	public static readonly DiffLauncher Shared = new(new ProcessStarter(), Environment.GetEnvironmentVariable);

	int launched;

	public void Launch(string? tool, string receivedPath, string verifiedPath, bool verifiedExists, int maxLaunch)
	{
		var template = tool ?? DefaultTool;
		if (template.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
			return;

		if (!verifiedExists)
		{
			// A custom tool gets no stand-in for the missing file; the default opens the new snapshot alone.
			if (tool is not null)
				return;
			template = DefaultNewFileTool;
		}

		if (Interlocked.Increment(ref launched) > maxLaunch)
			return;

		var tokens = Tokenize(template)
			.Select(token => token.Replace("{received}", receivedPath).Replace("{verified}", verifiedPath))
			.ToList();
		if (tokens.Count == 0 || ResolveExecutable(tokens[0]) is not { } executable)
			return;

		try
		{
			starter.Start(executable, tokens.Skip(1).ToList());
		}
		catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
		{
			// Launching is a convenience; the failure message already names every file.
		}
	}

	internal static List<string> Tokenize(string template)
	{
		var tokens = new List<string>();
		var current = new StringBuilder();
		var quoted = false;
		var pending = false;
		foreach (var c in template)
		{
			if (c == '"')
			{
				quoted = !quoted;
				pending = true;
				continue;
			}

			if (char.IsWhiteSpace(c) && !quoted)
			{
				if (pending)
				{
					tokens.Add(current.ToString());
					current.Clear();
					pending = false;
				}
				continue;
			}

			current.Append(c);
			pending = true;
		}

		if (pending)
			tokens.Add(current.ToString());
		return tokens;
	}

	/// <summary>A full path for <paramref name="name"/> found on PATH (trying PATHEXT on Windows), or null.</summary>
	internal string? ResolveExecutable(string name)
	{
		if (Path.IsPathRooted(name) || name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
			return File.Exists(name) ? name : null;

		string[] extensions = OperatingSystem.IsWindows() && !Path.HasExtension(name)
			? (getEnvironment("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries)
			: [""];

		foreach (var folder in (getEnvironment("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
			foreach (var extension in extensions)
			{
				var candidate = Path.Combine(folder.Trim('"'), name + extension);
				if (File.Exists(candidate))
					return candidate;
			}

		return null;
	}
}
