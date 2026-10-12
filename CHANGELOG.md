# Changelog

## Unreleased

### Added

- `SGSnap`: `Snapshot.Match` for text and multi-file snapshots named after the running xUnit v3
  test; scrubbers; byte-exact writing (BOM, LF, content verbatim) with loose comparison; a
  unified line diff in failures; accept commands; accept-all (`SGSNAP_ACCEPT`) and check-only
  (`SGSNAP_CHECKONLY`) modes; orphan detection; a diff-tool launch (VS Code by default, capped,
  skipped in CI). See the README's "When a snapshot does not match" and "Diff tool" sections.
- `SGSnap`: `Snapshot.MatchCli` captures an in-process CLI run's exit code, stdout, stderr and the
  files it wrote. See the README's "CLI snapshots" section.
- `SGSnap.SourceGenerators`: `GeneratorSnapshot.Match` snapshots a source generator run: one
  file per generated source plus its diagnostics. See the README's "Generator snapshots" and
  "Diagnostics format" sections.
