namespace SGSnap;

/// <summary>Compares targets with their verified files under the default, accept-all and check-only rules.</summary>
internal sealed class SnapshotEngine(SnapshotEnvironment environment, DiffLauncher launcher, Action<string> warn)
{
	public void Run(string directory, string prefix, IReadOnlyList<SnapshotTarget> targets, SnapshotSettings settings)
	{
		if (environment.Accept && environment.CheckOnly)
			throw new InvalidOperationException("SGSNAP_ACCEPT and SGSNAP_CHECKONLY cannot both be set.");

		var filePrefix = SnapshotPaths.IsPerTest(settings.Directory) ? null : prefix;
		Validate(targets, filePrefix);
		CheckoutPaths.Check(directory, targets.Select(target => SnapshotPaths.FileName(filePrefix, target, "verified")), environment.RootLength);

		var accept = environment.Accept && !environment.IsCI;
		if (environment.Accept && environment.IsCI)
			warn("SGSNAP_ACCEPT is ignored in CI; snapshots were compared normally.");

		if (!environment.CheckOnly)
		{
			Directory.CreateDirectory(directory);
			foreach (var stale in SnapshotPaths.Find(directory, filePrefix, "received").ToList())
				File.Delete(stale);
		}

		var failures = new List<SnapshotFailure>();
		var produced = new HashSet<string>(SnapshotPaths.PathComparer);

		foreach (var target in targets)
		{
			var content = settings.Apply(target.Content);
			var verifiedPath = Path.Combine(directory, SnapshotPaths.FileName(filePrefix, target, "verified"));
			produced.Add(verifiedPath);

			var verified = SnapshotText.ReadVerified(verifiedPath);
			if (verified == content)
				continue;

			if (accept)
			{
				SnapshotText.Write(verifiedPath, content);
				warn($"Accepted snapshot: {verifiedPath}");
				continue;
			}

			string? receivedPath = null;
			if (!environment.CheckOnly)
			{
				receivedPath = Path.Combine(directory, SnapshotPaths.FileName(filePrefix, target, "received"));
				SnapshotText.Write(receivedPath, content);
			}

			failures.Add(verified is null
				? new SnapshotFailure(FailureKind.Missing, verifiedPath, receivedPath, null)
				: new SnapshotFailure(FailureKind.Differs, verifiedPath, receivedPath, LineDiff.Unified(verified, content)));
		}

		foreach (var orphan in SnapshotPaths.Find(directory, filePrefix, "verified").Where(path => !produced.Contains(path)).ToList())
		{
			if (accept)
			{
				File.Delete(orphan);
				warn($"Deleted orphan snapshot: {orphan}");
			}
			else
				failures.Add(new SnapshotFailure(FailureKind.Orphan, orphan, null, null));
		}

		if (failures.Count == 0)
			return;

		if (!environment.CheckOnly && !environment.IsCI)
			foreach (var failure in failures.Where(failure => failure.ReceivedPath is not null))
				launcher.Launch(environment.DiffTool, failure.ReceivedPath!, failure.VerifiedPath, failure.Kind == FailureKind.Differs, environment.MaxLaunch);

		throw new SnapshotMismatchException(SnapshotReport.Format(prefix, failures));
	}

	static void Validate(IReadOnlyList<SnapshotTarget> targets, string? filePrefix)
	{
		if (targets.Count == 0)
			throw new ArgumentException("At least one snapshot target is required.", nameof(targets));

		var invalid = Path.GetInvalidFileNameChars();
		// Case-insensitive on every OS: names committed on Linux must still check out side by side on Windows.
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var target in targets)
		{
			if (string.IsNullOrWhiteSpace(target.Extension) || target.Extension.Contains('.') || target.Extension.IndexOfAny(invalid) >= 0)
				throw new ArgumentException($"Invalid snapshot extension '{target.Extension}'.", nameof(targets));
			if (target.Name is { } name && (name.Length == 0 || name.IndexOfAny(invalid) >= 0))
				throw new ArgumentException($"Invalid snapshot name '{name}'.", nameof(targets));
			if (!seen.Add(SnapshotPaths.FileName(filePrefix, target, "verified")))
				throw new ArgumentException($"Two snapshot targets share the file name '{SnapshotPaths.FileName(filePrefix, target, "verified")}'.", nameof(targets));
		}
	}
}
