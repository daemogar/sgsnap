namespace SGSnap;

/// <summary>One snapshot file: <c>Extension</c> has no dot; a non-null <c>Name</c> becomes <c>#Name</c> in the file name.</summary>
public sealed record SnapshotTarget(string Extension, string Content, string? Name = null);
