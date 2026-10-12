# Contributing

## Prerequisites

The .NET 10 SDK. The projects target `net10.0` and use central package management: every
dependency's version lives in `Directory.Packages.props`, and a `PackageReference` never carries
its own `Version`.

## Build and test

```bash
dotnet restore SGSnap.slnx
dotnet build   SGSnap.slnx
dotnet test    SGSnap.slnx
```

**Read the test count that is reported, not just the exit code.** A test project whose runner
adapter is missing discovers zero tests and still exits zero. `SGSnap.Tests/HarnessTests.cs` is a
permanent smoke test for exactly that failure.

## Style

- **Tabs** for indentation in C# files; see `.editorconfig` for every other file type. C# files
  are UTF-8 with BOM.
- File-scoped namespaces, usings outside the namespace, system directives first, import groups
  separated by a blank line.
- **Comments stay short.** A `<summary>` says what a member is and why it exists. Longer
  explanation belongs in `README.md` (user-facing) or here (contributor-facing), never in
  `<remarks>`.

## Snapshot format changes

The bytes SGSnap writes are a compatibility promise: committed snapshots in consuming repos must
keep passing unchanged. A change to how any target is rendered or written needs a run over a real
corpus of committed snapshots, in check-only mode, with zero differences, before it merges.

## No vendor or product references in tracked files

Nothing in this repository (code, comments, documentation, commit messages, file names, or test
names) names another snapshot library, a consuming project, or a company. The platform and
dependencies SGSnap is built on (.NET, xUnit, Roslyn) and the default diff tool (VS Code) are the
only names used. `docs/` and `.superpowers/` are gitignored and never committed; anything that
needs a specific name to explain itself belongs there.

## Pull requests

Keep a change and its explanation together: a behavior change that needs a longer rationale gets
it in `README.md` or here, not only in the commit message. Update `CHANGELOG.md`'s
`## Unreleased` section for anything a consumer would notice.
