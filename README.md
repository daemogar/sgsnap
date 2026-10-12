# SGSnap

Snapshot testing for xUnit v3. A test produces text; SGSnap compares it with a committed
`.verified` file next to the test, and on a mismatch writes a `.received` file, opens a diff and
fails with a line diff.

Licensed under AGPL-3.0-or-later.
