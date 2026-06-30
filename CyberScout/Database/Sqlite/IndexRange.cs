using System.Diagnostics;
using UtilitiesLibrary.Collections;

namespace Database.Sqlite;



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

	public required long? EventDataId { get; init; }

	private MatchIndexMetaData() { }

	public static MatchIndexMetaData CreateNoneMatch() {

		return new() {
			Status = RecordStatus.None,
			GameDeviceId = null,
			GameId = null,
			EventDataId = null
		};
	}

	public static MatchIndexMetaData CreateStoredMatch(string gameDeviceId, long gameId, long eventMetaDataId) {

		return new() {
			Status = RecordStatus.Stored,
			GameDeviceId = gameDeviceId,
			GameId = gameId,
			EventDataId = eventMetaDataId
		};
	}

	public static MatchIndexMetaData CreateIgnoredMatch() {

		return new() {
			Status = RecordStatus.Ignored,
			GameDeviceId = null,
			GameId = null,
			EventDataId = null
		};
	}

}



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

	public static IndexRange? Create(string deviceId, long start, long end, RecordMetaData metaData) {

		if (start > end) {
			return null;
		}

		return new(deviceId, start, end, metaData);
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

	public static SuperRange? Create(List<IndexRange> ranges) {

		if (ranges.IsEmpty()) {
			return null;
		}

		string deviceId = ranges.First().DeviceId;
		if (ranges.Any(range => range.DeviceId == deviceId)) {
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
		return new(deviceId, checkedRanges);
	}



	public bool Contains(long index) {
		return Ranges.First().Start <= index && index <= Ranges.Last().End;
	}

	public SuperRange? OverwriteRangeAndSimplify(long indexToOverwrite, RecordMetaData newMetaData) {

		if (!Contains(indexToOverwrite)) {
			return null;
		}

		List<IndexRange> newRanges = new(Ranges.Count + 2);
		foreach (IndexRange range in Ranges) {

			if (!range.Contains(indexToOverwrite)) {
				newRanges.Add(range);
				continue;
			}

			IndexRange newRange = IndexRange.Create(DeviceId, indexToOverwrite, indexToOverwrite, newMetaData) ?? throw new UnreachableException();

			if (range.Start == indexToOverwrite && range.End == indexToOverwrite) {
				newRanges.Add(newRange);
				continue;
			}

			if (range.Start == indexToOverwrite) {
				newRanges.Add(newRange);
				newRanges.Add(IndexRange.Create(DeviceId, indexToOverwrite + 1, range.End, range.MetaData) ?? throw new UnreachableException());
				continue;
			}

			if (range.End == indexToOverwrite) {
				newRanges.Add(IndexRange.Create(DeviceId, range.Start, indexToOverwrite - 1, range.MetaData) ?? throw new UnreachableException());
				newRanges.Add(newRange);
				continue;
			}

			newRanges.Add(IndexRange.Create(DeviceId, range.Start, indexToOverwrite - 1, range.MetaData) ?? throw new UnreachableException());
			newRanges.Add(newRange);
			newRanges.Add(IndexRange.Create(DeviceId, indexToOverwrite + 1, range.End, range.MetaData) ?? throw new UnreachableException());
		}

		SuperRange newSuperRangeUnsimplified = Create(newRanges) ?? throw new UnreachableException();

		return newSuperRangeUnsimplified.Simplify();
	}

	public SuperRange? OverwriteRangeAndSimplify(IndexRange rangeToOverwrite, RecordMetaData newMetaData) {

		if (!Ranges.Contains(rangeToOverwrite)) {
			return null;
		}

		List<IndexRange> newRanges = new(Ranges.Count + 2);
		foreach (IndexRange range in Ranges) {

			if (range != rangeToOverwrite) {
				newRanges.Add(range);
				continue;
			}

			IndexRange newRange = IndexRange.Create(DeviceId, range.Start, range.End, newMetaData) ?? throw new UnreachableException();
			newRanges.Add(newRange);
		}

		SuperRange newSuperRangeUnsimplified = Create(newRanges) ?? throw new UnreachableException();
		return newSuperRangeUnsimplified.Simplify();
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

			simplifiedRanges.Add(IndexRange.Create(DeviceId, currentStart, range.Start - 1, currentStatus) ?? throw new UnreachableException());
			currentStart = range.Start;
			currentStatus = range.MetaData;
		}

		simplifiedRanges.Add(IndexRange.Create(DeviceId, currentStart, Ranges.Last().End, currentStatus) ?? throw new UnreachableException());

		return Create(simplifiedRanges) ?? throw new UnreachableException();
	}

}