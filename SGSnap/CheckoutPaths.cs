namespace SGSnap;

/// <summary>Rejects snapshot paths too long for a Windows checkout without long-path support.</summary>
/// <remarks>See README.md "Path length".</remarks>
internal static class CheckoutPaths
{
	/// <summary>The longest path Windows creates without long-path support: MAX_PATH less the terminating null.</summary>
	public const int MaxPath = 259;

	public static void Check(string directory, IEnumerable<string> fileNames, int? rootLength)
	{
		var root = FindRoot(directory);
		if (root is null)
			return;

		var allowance = rootLength ?? DefaultRootLength(RepositoryName(root));
		var folder = Path.GetRelativePath(root, directory);
		var tooLong = fileNames
			.Select(name => (folder == "." ? name : Path.Combine(folder, name)).Replace('\\', '/'))
			.Where(relative => allowance + relative.Length > MaxPath)
			.ToList();
		if (tooLong.Count == 0)
			return;

		var lines = tooLong.Select(relative => $"  {allowance + relative.Length}: {relative}");
		throw new ArgumentException(
			$"Snapshot paths are too long to check out on Windows ({MaxPath} characters at most, counting a {allowance}-character checkout root):{Environment.NewLine}"
			+ string.Join(Environment.NewLine, lines) + Environment.NewLine
			+ "Shorten the test class or method name, the snapshot folder, or the target name. SGSNAP_ROOTLENGTH sets the checkout-root allowance.");
	}

	/// <summary>The nearest folder at or above <paramref name="directory"/> holding a <c>.git</c> folder or file.</summary>
	public static string? FindRoot(string directory)
	{
		for (var folder = new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
		{
			var git = Path.Combine(folder.FullName, ".git");
			if (Directory.Exists(git) || File.Exists(git))
				return folder.FullName;
		}

		return null;
	}

	/// <summary>The main checkout's folder name, followed from a linked worktree's <c>.git</c> file when there is one.</summary>
	public static string RepositoryName(string root)
	{
		var git = Path.Combine(root, ".git");
		const string key = "gitdir:";
		if (File.Exists(git) && File.ReadLines(git).FirstOrDefault() is { } line && line.StartsWith(key, StringComparison.Ordinal))
		{
			// A linked worktree points at <main>/.git/worktrees/<name>.
			var worktrees = Path.GetDirectoryName(Path.GetFullPath(line[key.Length..].Trim(), root));
			var dotGit = Path.GetDirectoryName(worktrees);
			if (Path.GetFileName(worktrees) == "worktrees" && Path.GetFileName(dotGit) == ".git")
				return Path.GetFileName(Path.GetDirectoryName(dotGit))!;
		}

		return new DirectoryInfo(root).Name;
	}

	/// <summary>The length of a hosted Windows runner's checkout root, <c>D:\a\&lt;repo&gt;\&lt;repo&gt;\</c>.</summary>
	public static int DefaultRootLength(string repository) => $@"D:\a\{repository}\{repository}\".Length;
}
