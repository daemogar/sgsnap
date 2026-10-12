using System.Runtime.CompilerServices;

using SGSnap.SourceGenerators;

namespace SGSnap.Tests.Generators;

public class GeneratorSnapshotTests
{
	[Fact]
	public Task Snapshots_a_generator_run()
		=> GeneratorSnapshot.Match(
			GeneratorRun.Driver("namespace Sample;\n\npublic partial record Part(int Id);\n"),
			new SnapshotSettings { Directory = "Snapshots" });

	[Fact]
	public void Generated_files_keep_their_own_final_newline_or_lack_of_one()
	{
		var folder = Path.Combine(Path.GetDirectoryName(ThisFile())!, "Snapshots");
		var attribute = File.ReadAllBytes(Path.Combine(folder, "GeneratorSnapshotTests.Snapshots_a_generator_run#WidgetAttribute.verified.cs"));
		var helpers = File.ReadAllBytes(Path.Combine(folder, "GeneratorSnapshotTests.Snapshots_a_generator_run#Helpers.g.verified.cs"));

		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, attribute[..3]);
		Assert.Equal((byte)'}', attribute[^1]);
		Assert.Equal((byte)'\n', helpers[^1]);
	}

	static string ThisFile([CallerFilePath] string path = "") => path;
}
