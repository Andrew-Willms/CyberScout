using System;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



public abstract record CreateEventScheduleError : Error;

public record DuplicateMatchName : CreateEventScheduleError {

	public required ReadOnlyList<string> Duplicates { get; init; }

}

public record StartAfterEnd : CreateEventScheduleError {

	public required DateTime StartDateTime { get; init; }

	public required DateTime EndDateTime { get; init; }
}

public record MatchHasWrongFormat : CreateEventScheduleError {

	public required MatchFormat RequiredMatchFormat { get; init; }

	public required ScheduledMatch ViolatingMatch { get; init; }
}

public record GroupHasWrongFormat : CreateEventScheduleError {

	public required MatchFormat ScheduleFormat { get; init; }

	public required MatchFormat GroupFormat { get; init; }
}

public record DuplicateTeamInMatch : CreateEventScheduleError {

	public ReadOnlyList<uint> Duplicates { get; }

	private DuplicateTeamInMatch(ReadOnlyList<uint> duplicates) {
		Duplicates = duplicates;
	}

	public static DuplicateTeamInMatch? Create(ReadOnlyList<uint> duplicates) {
		return duplicates.Count == 0 ? null : new(duplicates);
	}

}