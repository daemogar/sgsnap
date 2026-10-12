# SGSnap — working notes for agents

Read `README.md` for what SGSnap does and `CONTRIBUTING.md` for build, test and style. This file
holds agent-facing rules only; it points at those documents rather than repeating them.

## The naming rule

No tracked file, file name, test name or commit message may name another snapshot library, a
consuming project, or a company. Allowed: .NET, xUnit, Roslyn, VS Code. Use synthetic examples
(`Widget`, `Part`, `Fixture`) in tests and docs.

## Snapshot bytes are a compatibility promise

Written snapshots are UTF-8 with BOM, LF, content verbatim. Never "tidy" a final newline, sort
diagnostics, or change indentation in a rendered format without the corpus check described in
`CONTRIBUTING.md` "Snapshot format changes".

## `docs/` and `.superpowers/` are untracked, on purpose

Design notes there may name real systems; nothing under either path is ever committed.

## Testing

xUnit v3 3.2.2 through `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`. Read the
reported test count, not the exit code; `SGSnap.Tests/HarnessTests.cs` must stay.
