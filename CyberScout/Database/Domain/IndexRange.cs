using System.Diagnostics;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Database.Domain;



public enum RecordStatus {
	None,
	Stored,
	Ignored
}

// TODO make closed class hierarchy
public abstract record RecordMetaData {

	public required RecordStatus Status { get; init; }

}

public record GameIndexMetaData : RecordMetaData;

public record EventIndexMetaData : RecordMetaData;

public record MatchIndexMetaData : RecordMetaData  {

	public required string? GameDeviceId { get; init; }

	public required long? GameId { get; init; }

	public required string? EventCode { get; init; }

	private MatchIndexMetaData() { }

	public static MatchIndexMetaData CreateNoneMatch() {

		return new() {
			Status = RecordStatus.None,
			GameDeviceId = null,
			GameId = null,
			EventCode = null
		};
	}

	public static MatchIndexMetaData CreateStoredMatch(string gameDeviceId, long gameId, string eventCode) {

		return new() {
			Status = RecordStatus.Stored,
			GameDeviceId = gameDeviceId,
			GameId = gameId,
			EventCode = eventCode
		};
	}

	public static MatchIndexMetaData CreateIgnoredMatch() {

		return new() {
			Status = RecordStatus.Ignored,
			GameDeviceId = null,
			GameId = null,
			EventCode = null
		};
	}

}



// TODO consider allowing zero length ranges... this might be helpful for functions like IndexRange.RangeBefore etc.
public record IndexRange {

	public string DeviceId { get; }

	public long Start { get; }

	public long End { get; }

	public RecordMetaData MetaData { get; }

	private IndexRange(string deviceId, long start, long end, RecordMetaData metaData) {
		DeviceId = deviceId;
		Start = start;
		End = end;
		MetaData = metaData;
	}

	public static Result<IndexRange> Create(string deviceId, long start, long end, RecordMetaData metaData) {

		if (start > end) {
			return new AdHocError(("start", start.ToString()), ("end", end.ToString())) {
				Message = "Start index after end index."
			};
		}

		return new IndexRange(deviceId, start, end, metaData);
	}

	public bool Contains(long index) {
		return Start <= index && index <= End;
	}

}



/// <summary>
/// A collection of contiguous <see cref="IndexRange"/>.
/// </summary>
public record SuperRange {

	public string DeviceId { get; }

	public ReadOnlyList<IndexRange> Ranges { get; }

	private SuperRange(string deviceId, ReadOnlyList<IndexRange> ranges) {
		DeviceId = deviceId;
		Ranges = ranges;
	}

	public static Result<SuperRange> Create(List<IndexRange> ranges) {

		if (ranges.IsEmpty()) {
			return new AdHocError("List or ranges is empty.");
		}

		string deviceId = ranges.First().DeviceId;
		if (ranges.Any(range => range.DeviceId == deviceId)) {
			return new AdHocError("Not all deviceIds match", ranges.Select((range, index) => (index.ToString(), range.ToString())).ToList());
		}

		long previousEnd = ranges.First().Start - 1;

		foreach (IndexRange range in ranges) {

			if (range.Start != previousEnd + 1) {
				return null;
			}

			previousEnd = range.End;
		}

		ReadOnlyList<IndexRange> checkedRanges = ranges.ToReadOnly();
		return new SuperRange(deviceId, checkedRanges);
	}



	public bool Contains(long index) {
		return Ranges.First().Start <= index && index <= Ranges.Last().End;
	}

	public Result<SuperRange> OverwriteRangeAndSimplify(long indexToOverwrite, RecordMetaData newMetaData) {

		if (!Contains(indexToOverwrite)) {
			return new AdHocError(
				"The SuperRange does not contain the index to overwrite",
				("indexToOverwrite", indexToOverwrite.ToString()), 
				("super range start", Ranges.First().Start.ToString()),
				("super range end", Ranges.Last().End.ToString()));
		}

		List<IndexRange> newRanges = new(Ranges.Count + 2);
		foreach (IndexRange range in Ranges) {

			if (!range.Contains(indexToOverwrite)) {
				newRanges.Add(range);
				continue;
			}

			Result<IndexRange> newRangeResult = IndexRange.Create(DeviceId, indexToOverwrite, indexToOverwrite, newMetaData) ?? throw new UnreachableException();
			if (newRangeResult.IsFailure) {
				return new AdHocError("Error creating range", newRangeResult.Error);
			}
			IndexRange newRange = newRangeResult.Value;

			if (range.Start == indexToOverwrite && range.End == indexToOverwrite) {
				newRanges.Add(newRange);
				continue;
			}

			// TODO consider function like IndexRange.RangeAfter(indexToSplit)
			if (range.Start == indexToOverwrite) {
				newRanges.Add(newRange);
				newRanges.Add(IndexRange.Create(DeviceId, indexToOverwrite + 1, range.End, range.MetaData).Value ?? throw new UnreachableException());
				continue;
			}

			// TODO consider function like IndexRange.RangeBefore(indexToSplit)
			if (range.End == indexToOverwrite) {
				newRanges.Add(IndexRange.Create(DeviceId, range.Start, indexToOverwrite - 1, range.MetaData).Value ?? throw new UnreachableException());
				newRanges.Add(newRange);
				continue;
			}

			// TODO see above
			newRanges.Add(IndexRange.Create(DeviceId, range.Start, indexToOverwrite - 1, range.MetaData).Value ?? throw new UnreachableException());
			newRanges.Add(newRange);
			newRanges.Add(IndexRange.Create(DeviceId, indexToOverwrite + 1, range.End, range.MetaData).Value ?? throw new UnreachableException());
		}

		Result<SuperRange> newSuperRangeUnsimplified = Create(newRanges);
		if (newSuperRangeUnsimplified.IsFailure) {
			return new AdHocError("Error creating SuperRange", newSuperRangeUnsimplified.Error);
		}

		return newSuperRangeUnsimplified.Value.Simplify();
	}

	public Result<SuperRange> OverwriteRangeAndSimplify(IndexRange rangeToOverwrite, RecordMetaData newMetaData) {

		if (rangeToOverwrite.DeviceId != DeviceId) {
			return new AdHocError(
				"The range to overwrite has a different deviceId than this SuperRange",
				("range DeviceId", rangeToOverwrite.DeviceId),
				("SuperRange DeviceId", DeviceId));
		}

		if (!Ranges.Contains(rangeToOverwrite)) {
			return new AdHocError(
				"The SuperRange does not contain the index to overwrite",
				("rangeToOverwrite", rangeToOverwrite.ToString()),
				("super range start", Ranges.First().Start.ToString()),
				("super range end", Ranges.Last().End.ToString()));
		}

		List<IndexRange> newRanges = new(Ranges.Count + 2);
		foreach (IndexRange range in Ranges) {

			if (range != rangeToOverwrite) {
				newRanges.Add(range);
				continue;
			}

			// TODO consider functions like IndexRange.WithNewMetaData(metaData)
			IndexRange newRange = IndexRange.Create(DeviceId, range.Start, range.End, newMetaData).Value ?? throw new UnreachableException();
			newRanges.Add(newRange);
		}

		Result<SuperRange> newSuperRangeUnsimplified = Create(newRanges);
		if (newSuperRangeUnsimplified.IsFailure) {
			return new AdHocError("Error creating SuperRange", newSuperRangeUnsimplified.Error);
		}

		return newSuperRangeUnsimplified.Value.Simplify();
	}

	public SuperRange Simplify() {

		if (Ranges.Count == 1) {
			return this;
		}

		List<IndexRange> simplifiedRanges = [];

		long currentStart = Ranges.First().Start;
		RecordMetaData currentStatus = Ranges.First().MetaData;

		foreach (IndexRange range in Ranges.Skip(1)) {

			if (currentStatus.Equals(range.MetaData)) {
				continue;
			}

			Result<IndexRange> createRangeResult = IndexRange.Create(DeviceId, currentStart, range.Start - 1, currentStatus);
			if (createRangeResult.IsFailure) {
				throw new UnreachableException();
			}

			simplifiedRanges.Add(createRangeResult.Value);
			currentStart = range.Start;
			currentStatus = range.MetaData;
		}

		Result<IndexRange> createLastRangeResult = IndexRange.Create(DeviceId, currentStart, Ranges.Last().End, currentStatus);
		if (createLastRangeResult.IsFailure) {
			throw new UnreachableException();
		}

		simplifiedRanges.Add(createLastRangeResult.Value);

		Result<SuperRange> createSuperRangeResult = Create(simplifiedRanges);
		if (createSuperRangeResult.IsFailure) {
			throw new UnreachableException();
		}

		return createSuperRangeResult.Value;
	}

}