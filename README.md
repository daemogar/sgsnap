# SGSnap

Snapshot testing for xUnit v3. A test produces text; SGSnap compares it with a committed
`.verified` file next to the test. On a mismatch it writes a `.received` file, opens a diff and
fails with a line diff.

Licensed under AGPL-3.0-or-later.

## Packages

| Package | Use it for |
| --- | --- |
| `SGSnap` | Text, multi-file and in-process CLI snapshots. |
| `SGSnap.SourceGenerators` | Snapshots of a Roslyn source generator run: every generated file plus its diagnostics. |

Both target `net10.0` and need xUnit v3 (3.2.x).

## Writing a snapshot test

```csharp
using SGSnap;

public class InvoiceTests
{
	[Fact]
	public Task Renders_an_invoice()
		=> Snapshot.Match(Invoice.Render(sample), new SnapshotSettings { Directory = "Snapshots" });
}
```

The file is `Snapshots/InvoiceTests.Renders_an_invoice.verified.txt`, beside the test file. The
name is the running test's class and method; a nested class is written `Outer.Inner`.
`Directory` may be relative to the test file's folder or absolute; without it, files sit next to
the test file.

Several files at once:

```csharp
Snapshot.Match([new SnapshotTarget("txt", summary), new SnapshotTarget("cs", code, "Widget")]);
```

writes `Class.Method.verified.txt` and `Class.Method#Widget.verified.cs`.

### Scrubbers

Replace unstable text before comparing. Scrubbers run in the order added, on every file:

```csharp
new SnapshotSettings { Directory = "Snapshots" }
	.AddScrubber(new Regex(@"Offset = \d+"), "Offset = {scrubbed}");
```

## Snapshot files

Every file SGSnap writes is UTF-8 with a BOM, LF line endings, and the content exactly as
produced: no newline is added or removed at the end. Comparison is looser: the BOM is ignored and
CRLF counts as LF, so an autocrlf checkout or an editor that drops the BOM never causes a false
failure. Pin the files to LF in `.gitattributes`:

```gitattributes
*.verified.* text eol=lf
*.received.* text eol=lf
```

and ignore `*.received.*`.

Snapshots of generated C# end in `.verified.cs`. The `SGSnap` package's build targets remove
`*.verified.cs` and `*.received.cs` from the test project's compilation, so they never collide
with real types.

## When a snapshot does not match

Every file of the call is compared first; then one failure lists them all.

- **Differs**: `.received` is written, the diff tool opens, and the message shows a unified diff.
- **Missing**: there is no `.verified` yet; `.received` is written and opened.
- **Orphan**: a `.verified` file for this test that the run did not produce, such as a generated
  file that no longer exists. It fails until you delete it, so a vanished output never passes
  silently.

Stale `.received` files for the test are deleted at the start of every run.

A mismatch throws `SnapshotMismatchException` directly instead of returning a faulted task, so a
call someone forgot to `await` still fails the test.

Each test gets one snapshot file set, so make **one** `Match` call per test; pass several targets
to one call when a test has several outputs. A second call in the same test, or a `[Theory]`
whose rows share one snapshot name, throws `InvalidOperationException` instead of letting the
calls overwrite or delete each other's files.

### Accepting changes

Accept in the diff tool, or rename `.received` to `.verified`. The failure message prints the
exact `Move-Item` (PowerShell) and `mv` (POSIX) command for each file.

### Accept-all mode

`SGSNAP_ACCEPT=1` writes every mismatched or missing file straight to `.verified`, deletes
orphans, and passes, reporting each accepted file as a test warning. Use it for a deliberate
bulk change, then review the result with `git diff`. It is ignored in CI (with a warning), and
setting it together with `SGSNAP_CHECKONLY` is an error.

### Check-only mode

`SGSNAP_CHECKONLY=1` compares and fails, but writes nothing and opens nothing. Use it to prove a
set of committed snapshots passes unchanged.

## Diff tool

The default is VS Code: `code --diff "{received}" "{verified}"`, or `code "{received}"` when
there is no verified file yet. Override it with `SGSNAP_DIFFTOOL` or, in code, with
`SnapshotDefaults.DiffTool`, using `{received}` and `{verified}` placeholders; quote paths that
may contain spaces. A custom tool is not opened for a missing verified file. `none` turns
launching off.

Launching is best-effort and never waits. A tool that is not installed is skipped. At most 5
tools open per test run (`SGSNAP_MAXLAUNCH`), and none in CI.

## Environment variables

| Variable | Effect |
| --- | --- |
| `SGSNAP_ACCEPT` | `1` or `true`: accept-all mode. Ignored in CI. |
| `SGSNAP_CHECKONLY` | `1` or `true`: compare only; write and open nothing. |
| `SGSNAP_DIFFTOOL` | Diff command with `{received}` / `{verified}`, or `none`. |
| `SGSNAP_MAXLAUNCH` | Most diff tools opened per test run. Default 5. |
| `CI`, `TF_BUILD` | CI is detected when `CI` is `true` or `TF_BUILD` is set: no launches, accept-all ignored. |

## Generator snapshots

```csharp
using SGSnap;
using SGSnap.SourceGenerators;

[Fact]
public Task Generates_the_widget()
	=> GeneratorSnapshot.Match(driver, new SnapshotSettings { Directory = "Snapshots" });
```

Each generated source becomes `Class.Method#<hint name without its last extension>.verified.cs`
(`Widget.g.cs` becomes `#Widget.g`), holding `//HintName: <hint>` on the first line and the
generated text, unchanged, after it. When the run reported diagnostics, they go in
`Class.Method.verified.txt`. A generator that threw makes the test fail with that exception.

## Diagnostics format

Diagnostics keep the order Roslyn reports them in. Each one lists `Location` (when it has one),
`Message`, `Severity`, `WarningLevel` (when non-zero) and its `Descriptor`: `Id`, `Title`,
`Description` and `HelpLink` (when set), `MessageFormat`, `Category`, `DefaultSeverity`,
`IsEnabledByDefault`. Keys and values are unquoted with two-space indentation:

```
{
  Diagnostics: [
    {
      Location: /*

public partial record Part(int Id);
                          ^^^^^^^^

*/
 : (2,26)-(2,34),
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
```

The location block starts at column 0. It shows the lines the span covers, plus one line of
context before and after, with carets under the span. The line after `*/` is a space, then the
tree's path (when it has one) followed by `: `, then the 0-based span
`(line,column)-(line,column)`. A descriptor with custom tags is not supported.

## CLI snapshots

Run a command-line entry point in-process and snapshot everything it did:

```csharp
[Fact]
public Task Normalizes_a_file()
	=> Snapshot.MatchCli((stdout, stderr) => Cli.Run(["normalize", path], stdout, stderr), filesRoot: folder);
```

```
ExitCode: 0
--- stdout ---
…
--- stderr ---
…
--- file: output.json ---
…
--- deleted: stale.json ---
```

Each stream's text ends with a newline when it is not empty, and empty sections keep their
header. Files under `filesRoot` that were added or changed are listed in ordinal path order with
`/` separators; a binary file shows as `(binary, N bytes, sha256 …)`.

## Building from source

See `CONTRIBUTING.md`.
