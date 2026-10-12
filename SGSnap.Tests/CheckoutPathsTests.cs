namespace SGSnap.Tests;

public class CheckoutPathsTests
{
	static readonly SnapshotEnvironment Local = new(Accept: false, CheckOnly: false, IsCI: false, DiffTool: "none", MaxLaunch: 0);

	/// <summary>A temp repository root holding a <c>.git</c> folder, with a snapshot folder inside it.</summary>
	sealed class Repository : IDisposable
	{
		readonly TempDirectory temp = new();

		public Repository() => Directory.CreateDirectory(temp.File(".git"));

		public string Root => temp.Path;

		public string Snapshots => temp.File("Snapshots");

		public void Run(SnapshotEnvironment environment, string prefix, params SnapshotTarget[] targets)
			=> new SnapshotEngine(environment, new DiffLauncher(new RecordingStarter(), _ => null), _ => { })
				.Run(Snapshots, prefix, targets, new SnapshotSettings());

		public void Dispose() => temp.Dispose();
	}

	/// <summary>A prefix that makes <c>Snapshots/&lt;prefix&gt;.verified.txt</c> exactly <paramref name="length"/> characters.</summary>
	static string PrefixFor(int length) => new('P', length - "Snapshots/".Length - ".verified.txt".Length);

	[Fact]
	public void A_path_at_the_limit_is_allowed()
	{
		using var repo = new Repository();
		var prefix = PrefixFor(CheckoutPaths.MaxPath - 20);

		Assert.Throws<SnapshotMismatchException>(() => repo.Run(Local with { RootLength = 20, CheckOnly = true }, prefix, new SnapshotTarget("txt", "x")));
	}

	[Fact]
	public void A_path_one_over_the_limit_is_rejected_before_anything_is_written()
	{
		using var repo = new Repository();
		var prefix = PrefixFor(CheckoutPaths.MaxPath - 20 + 1);

		var error = Assert.Throws<ArgumentException>(() => repo.Run(Local with { RootLength = 20 }, prefix, new SnapshotTarget("txt", "x")));

		Assert.Contains($"{CheckoutPaths.MaxPath + 1}: Snapshots/{prefix}.verified.txt", error.Message);
		Assert.Contains("20-character checkout root", error.Message);
		Assert.Contains("SGSNAP_ROOTLENGTH", error.Message);
		Assert.False(Directory.Exists(repo.Snapshots));
	}

	[Fact]
	public void Every_target_over_the_limit_is_listed()
	{
		using var repo = new Repository();
		var prefix = PrefixFor(CheckoutPaths.MaxPath - 20 - 5);

		var error = Assert.Throws<ArgumentException>(() => repo.Run(Local with { RootLength = 20 }, prefix,
			new SnapshotTarget("txt", "short"), new SnapshotTarget("cs", "a", "Widget"), new SnapshotTarget("cs", "b", "Gadget")));

		Assert.DoesNotContain($"{prefix}.verified.txt", error.Message);
		Assert.Contains($"{prefix}#Widget.verified.cs", error.Message);
		Assert.Contains($"{prefix}#Gadget.verified.cs", error.Message);
	}

	[Fact]
	public void The_default_allowance_is_a_hosted_runner_root_for_the_repository_name()
	{
		using var repo = new Repository();
		var name = new DirectoryInfo(repo.Root).Name;
		var allowance = $@"D:\a\{name}\{name}\".Length;
		var prefix = PrefixFor(CheckoutPaths.MaxPath - allowance + 1);

		var error = Assert.Throws<ArgumentException>(() => repo.Run(Local, prefix, new SnapshotTarget("txt", "x")));

		Assert.Contains($"{allowance}-character checkout root", error.Message);
	}

	[Fact]
	public void Outside_a_repository_nothing_is_checked()
	{
		using var temp = new TempDirectory();
		var engine = new SnapshotEngine(Local with { RootLength = 200, CheckOnly = true }, new DiffLauncher(new RecordingStarter(), _ => null), _ => { });

		Assert.Throws<SnapshotMismatchException>(() => engine.Run(temp.Path, new string('P', 100), [new SnapshotTarget("txt", "x")], new SnapshotSettings()));
	}

	[Fact]
	public void The_root_is_the_nearest_folder_with_a_git_folder_or_file()
	{
		using var temp = new TempDirectory();
		Directory.CreateDirectory(temp.File(".git"));
		var nested = Directory.CreateDirectory(temp.File("nested")).FullName;
		File.WriteAllText(Path.Combine(nested, ".git"), "gitdir: ../.git/modules/nested");

		Assert.Equal(nested, CheckoutPaths.FindRoot(Path.Combine(nested, "a", "b")));
		Assert.Equal(temp.Path, CheckoutPaths.FindRoot(temp.File("other")));
	}

	[Fact]
	public void A_linked_worktree_takes_the_main_checkouts_name()
	{
		using var temp = new TempDirectory();
		var worktree = Directory.CreateDirectory(temp.File("Widget-feature")).FullName;
		File.WriteAllText(Path.Combine(worktree, ".git"), $"gitdir: {temp.File("Widget").Replace('\\', '/')}/.git/worktrees/Widget-feature\n");

		Assert.Equal("Widget", CheckoutPaths.RepositoryName(worktree));
	}

	[Fact]
	public void A_main_checkout_or_submodule_takes_its_own_folder_name()
	{
		using var temp = new TempDirectory();
		var main = Directory.CreateDirectory(temp.File("Widget")).FullName;
		Directory.CreateDirectory(Path.Combine(main, ".git"));
		var module = Directory.CreateDirectory(Path.Combine(main, "Part")).FullName;
		File.WriteAllText(Path.Combine(module, ".git"), "gitdir: ../.git/modules/Part");

		Assert.Equal("Widget", CheckoutPaths.RepositoryName(main));
		Assert.Equal("Part", CheckoutPaths.RepositoryName(module));
	}
}
