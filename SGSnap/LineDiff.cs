namespace SGSnap;

/// <summary>Line diff (Myers, after trimming common prefix and suffix) rendered as a unified diff for failure messages.</summary>
internal static class LineDiff
{
	/// <summary>Above this many differing lines the search is skipped and the middle shows as delete-all, insert-all.</summary>
	const int MaxSearchLines = 2000;

	internal enum Kind { Same, Delete, Insert }

	internal readonly record struct Edit(Kind Kind, string Line, int OldIndex, int NewIndex);

	public static IReadOnlyList<Edit> Compute(string[] a, string[] b)
	{
		var prefix = 0;
		while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix])
			prefix++;

		var suffix = 0;
		while (suffix < a.Length - prefix && suffix < b.Length - prefix && a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix])
			suffix++;

		var edits = new List<Edit>();
		for (var i = 0; i < prefix; i++)
			edits.Add(new(Kind.Same, a[i], i, i));

		var middleA = a[prefix..(a.Length - suffix)];
		var middleB = b[prefix..(b.Length - suffix)];
		edits.AddRange(Middle(middleA, middleB).Select(edit => edit with { OldIndex = edit.OldIndex + prefix, NewIndex = edit.NewIndex + prefix }));

		for (var i = 0; i < suffix; i++)
			edits.Add(new(Kind.Same, a[a.Length - suffix + i], a.Length - suffix + i, b.Length - suffix + i));

		return edits;
	}

	static List<Edit> Middle(string[] a, string[] b)
	{
		if (a.Length + b.Length <= MaxSearchLines)
			return Myers(a, b);

		return [
			.. a.Select((line, i) => new Edit(Kind.Delete, line, i, 0)),
			.. b.Select((line, j) => new Edit(Kind.Insert, line, a.Length, j)),
		];
	}

	static List<Edit> Myers(string[] a, string[] b)
	{
		var n = a.Length;
		var m = b.Length;
		var max = n + m;
		var offset = max;
		var v = new int[2 * max + 2];
		var trace = new List<int[]>();

		for (var d = 0; d <= max; d++)
		{
			trace.Add((int[])v.Clone());
			for (var k = -d; k <= d; k += 2)
			{
				var x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1])
					? v[offset + k + 1]
					: v[offset + k - 1] + 1;
				var y = x - k;
				while (x < n && y < m && a[x] == b[y])
				{
					x++;
					y++;
				}

				v[offset + k] = x;
				if (x >= n && y >= m)
					return Backtrack(trace, a, b, offset);
			}
		}

		throw new InvalidOperationException("The diff search ended without reaching the end of both inputs.");
	}

	static List<Edit> Backtrack(List<int[]> trace, string[] a, string[] b, int offset)
	{
		var edits = new List<Edit>();
		var x = a.Length;
		var y = b.Length;

		for (var d = trace.Count - 1; d >= 0; d--)
		{
			var v = trace[d];
			var k = x - y;
			var prevK = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? k + 1 : k - 1;
			var prevX = v[offset + prevK];
			var prevY = prevX - prevK;

			while (x > prevX && y > prevY)
			{
				x--;
				y--;
				edits.Add(new(Kind.Same, a[x], x, y));
			}

			if (d > 0)
			{
				if (x == prevX)
					edits.Add(new(Kind.Insert, b[prevY], prevX, prevY));
				else
					edits.Add(new(Kind.Delete, a[prevX], prevX, prevY));
			}

			x = prevX;
			y = prevY;
		}

		edits.Reverse();
		return edits;
	}

	/// <summary>A unified diff of <paramref name="verified"/> against <paramref name="received"/>, cut off after <paramref name="maxLines"/> lines.</summary>
	public static string Unified(string verified, string received, int context = 3, int maxLines = 200)
	{
		var edits = Compute(verified.Split('\n'), received.Split('\n'));
		var lines = new List<string> { "--- verified", "+++ received" };

		var i = 0;
		while (i < edits.Count)
		{
			if (edits[i].Kind == Kind.Same)
			{
				i++;
				continue;
			}

			var start = Math.Max(0, i - context);
			var end = i;
			var j = i;
			while (j < edits.Count)
			{
				if (edits[j].Kind != Kind.Same)
				{
					end = j;
					j++;
					continue;
				}

				var run = j;
				while (run < edits.Count && edits[run].Kind == Kind.Same)
					run++;
				if (run < edits.Count && run - j <= 2 * context)
				{
					j = run;
					continue;
				}

				break;
			}

			var stop = Math.Min(edits.Count - 1, end + context);
			var hunk = edits.Skip(start).Take(stop - start + 1).ToList();
			var oldLength = hunk.Count(edit => edit.Kind != Kind.Insert);
			var newLength = hunk.Count(edit => edit.Kind != Kind.Delete);
			lines.Add($"@@ -{hunk[0].OldIndex + 1},{oldLength} +{hunk[0].NewIndex + 1},{newLength} @@");
			lines.AddRange(hunk.Select(edit => edit.Kind switch
			{
				Kind.Delete => "-" + edit.Line,
				Kind.Insert => "+" + edit.Line,
				_ => " " + edit.Line,
			}));
			i = stop + 1;
		}

		if (lines.Count > maxLines)
		{
			var hidden = lines.Count - maxLines;
			lines = [.. lines.Take(maxLines), $"... (+{hidden} more diff lines)"];
		}

		return string.Join("\n", lines);
	}
}
