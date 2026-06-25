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

	public required string? EventDeviceId { get; init; }

	public required long? EventMetaDataId { get; init; }

	private MatchIndexMetaData() { }

	public static MatchIndexMetaData CreateNoneMatch() {

		return new() {
			Status = RecordStatus.None,
			GameDeviceId = null,
			GameId = null,
			EventDeviceId = null,
			EventMetaDataId = null
		};
	}

	public static MatchIndexMetaData CreateStoredMatch(string gameDeviceId, long gameId, string eventDeviceId, long eventMetaDataId) {

		return new() {
			Status = RecordStatus.Stored,
			GameDeviceId = gameDeviceId,
			GameId = gameId,
			EventDeviceId = eventDeviceId,
			EventMetaDataId = eventMetaDataId
		};
	}

	public static MatchIndexMetaData CreateIgnoredMatch() {

		return new() {
			Status = RecordStatus.Ignored,
			GameDeviceId = null,
			GameId = null,
			EventDeviceId = null,
			EventMetaDataId = null
		};
	}

}



public record Ranges {

	public long Start { get; }

	public long End { get; }

	public RecordMetaData MetaData { get; }

	private Ranges(long start, long end, RecordMetaData metaData) {
		Start = start;
		End = end;
		MetaData = metaData;
	}

	public static Ranges? Create(long start, long end, RecordMetaData metaData) {

		if (start > end) {
			return null;
		}

		return new(start, end, metaData);
	}

	public bool Contains(long index) {
		return Start <= index && index <= End;
	}

}



/// <summary>
/// A collection of contiguous <see cref="Sqlite.Ranges"/>.
/// </summary>
public record SuperRange {

	public ReadOnlyList<Ranges> Ranges { get; }

	private SuperRange(ReadOnlyList<Ranges> ranges) {
		Ranges = ranges;
	}

	public static SuperRange? Create(List<Ranges> ranges) {

		if (ranges.IsEmpty()) {
			return null;
		}

		long previousEnd = ranges.First().Start - 1;

		foreach (Ranges range in ranges) {

			if (range.Start != previousEnd + 1) {
				return null;
			}

			previousEnd = range.End;
		}

		ReadOnlyList<Ranges> checkedRanges = ranges.ToReadOnly();
		return new(checkedRanges);
	}



	public bool Contains(long index) {
		return Ranges.First().Start <= index && index <= Ranges.Last().End;
	}

	public SuperRange? OverwriteIndexAndSimplify(long indexToOverwrite, RecordMetaData newMetaData) {

		if (!Contains(indexToOverwrite)) {
			return null;
		}

		List<Ranges> newRanges = new(Ranges.Count + 2);
		foreach (Ranges range in Ranges) {

			if (!range.Contains(indexToOverwrite)) {
				newRanges.Add(range);
				continue;
			}

			Ranges newRange = Sqlite.Ranges.Create(indexToOverwrite, indexToOverwrite, newMetaData) ?? throw new UnreachableException();

			if (range.Start == indexToOverwrite && range.End == indexToOverwrite) {
				newRanges.Add(newRange);
				continue;
			}

			if (range.Start == indexToOverwrite) {
				newRanges.Add(newRange);
				newRanges.Add(Sqlite.Ranges.Create(indexToOverwrite + 1, range.End, range.MetaData) ?? throw new UnreachableException());
				continue;
			}

			if (range.End == indexToOverwrite) {
				newRanges.Add(Sqlite.Ranges.Create(range.Start, indexToOverwrite - 1, range.MetaData) ?? throw new UnreachableException());
				newRanges.Add(newRange);
				continue;
			}

			newRanges.Add(Sqlite.Ranges.Create(range.Start, indexToOverwrite - 1, range.MetaData) ?? throw new UnreachableException());
			newRanges.Add(newRange);
			newRanges.Add(Sqlite.Ranges.Create(indexToOverwrite + 1, range.End, range.MetaData) ?? throw new UnreachableException());
		}

		SuperRange newSuperRangeUnsimplified = Create(newRanges) ?? throw new UnreachableException();

		return newSuperRangeUnsimplified.Simplify();
	}

	public SuperRange Simplify() {

		if (Ranges.Count == 1) {
			return this;
		}

		List<Ranges> simplifiedRanges = [];

		long currentStart = Ranges.First().Start;
		RecordMetaData currentStatus = Ranges.First().MetaData;

		foreach (Ranges range in Ranges.Skip(1)) {

			if (currentStatus.Equals(range.MetaData)) {
				continue;
			}

			simplifiedRanges.Add(Sqlite.Ranges.Create(currentStart, range.Start - 1, currentStatus) ?? throw new UnreachableException());
			currentStart = range.Start;
			currentStatus = range.MetaData;
		}

		simplifiedRanges.Add(Sqlite.Ranges.Create(currentStart, Ranges.Last().End, currentStatus) ?? throw new UnreachableException());

		return Create(simplifiedRanges) ?? throw new UnreachableException();
	}

}