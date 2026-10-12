namespace SGSnap;

/// <summary>Thrown when snapshot files do not match; the message lists every file and how to accept it.</summary>
public sealed class SnapshotMismatchException(string message) : Exception(message);
