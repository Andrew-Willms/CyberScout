using System.Diagnostics;
using UtilitiesLibrary.Collections;

namespace Database.Range;



public enum RecordStatus {
	None,
	Stored,
	Ignored
}


public record IndexRange {

	public long Start { get; }

	public long End { get; }

	public RecordStatus Status { get; }

	private IndexRange(long start, long end, RecordStatus status) {
		Start = start;
		End = end;
		Status = status;
	}

	public static IndexRange? Create(long start, long end, RecordStatus status) {

		if (start > end) {
			return null;
		}

		return new(start, end, status);
	}

	public bool Contains(long index) {
		return Start <= index && index <= End;
	}

}



public record RangeSet {

	public ReadOnlyList<IndexRange> Ranges { get; }

	private RangeSet(ReadOnlyList<IndexRange> ranges) {
		Ranges = ranges;
	}

	public static RangeSet? Create(List<IndexRange> ranges) {

		if (ranges.IsEmpty()) {
			return null;
		}

		long previousEnd = ranges.First().Start - 1;

		foreach (IndexRange range in ranges) {

			if (range.Start != previousEnd + 1) {
				return null;
			}

			previousEnd = range.End;
		}

		ReadOnlyList<IndexRange> checkedRanges = ranges.ToReadOnly();
		return new(checkedRanges);
	}



	public bool Contains(long index) {
		return Ranges.First().Start <= index && index <= Ranges.Last().End;
	}

	public RangeSet? OverwriteIndexAndSimplify(long indexToOverwrite, RecordStatus newStatus) {

		if (!Contains(indexToOverwrite)) {
			return null;
		}

		List<IndexRange> newRanges = new(Ranges.Count + 2);
		foreach (IndexRange range in Ranges) {

			if (!range.Contains(indexToOverwrite)) {
				newRanges.Add(range);
				continue;
			}

			IndexRange newRange = IndexRange.Create(indexToOverwrite, indexToOverwrite, newStatus) ?? throw new UnreachableException();

			if (range.Start == indexToOverwrite && range.End == indexToOverwrite) {
				newRanges.Add(newRange);
				continue;
			}

			if (range.Start == indexToOverwrite) {
				newRanges.Add(newRange);
				newRanges.Add(IndexRange.Create(indexToOverwrite + 1, range.End, range.Status) ?? throw new UnreachableException());
				continue;
			}

			if (range.End == indexToOverwrite) {
				newRanges.Add(IndexRange.Create(range.Start, indexToOverwrite - 1, range.Status) ?? throw new UnreachableException());
				newRanges.Add(newRange);
				continue;
			}

			newRanges.Add(IndexRange.Create(range.Start, indexToOverwrite - 1, range.Status) ?? throw new UnreachableException());
			newRanges.Add(newRange);
			newRanges.Add(IndexRange.Create(indexToOverwrite + 1, range.End, range.Status) ?? throw new UnreachableException());
		}

		RangeSet newRangeSetUnsimplified = Create(newRanges) ?? throw new UnreachableException();

		return newRangeSetUnsimplified.Simplify();
	}

	public RangeSet Simplify() {

		if (Ranges.Count == 1) {
			return this;
		}

		List<IndexRange> simplifiedRanges = [];

		long currentStart = Ranges.First().Start;
		RecordStatus currentStatus = Ranges.First().Status;

		foreach (IndexRange range in Ranges.Skip(1)) {

			if (currentStatus == range.Status) {
				continue;
			}

			simplifiedRanges.Add(IndexRange.Create(currentStart, range.Start - 1, currentStatus) ?? throw new UnreachableException());
			currentStart = range.Start;
			currentStatus = range.Status;
		}

		simplifiedRanges.Add(IndexRange.Create(currentStart, Ranges.Last().End, currentStatus) ?? throw new UnreachableException());

		return Create(simplifiedRanges) ?? throw new UnreachableException();
	}

}