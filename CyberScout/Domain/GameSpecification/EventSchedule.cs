using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Domain.Data;
using OneOf;
using UtilitiesLibrary.Collections;

namespace Domain.GameSpecification;



public class EventScheduleCreator {

	public ReadOnlyList<OneOf<EventSchedule, StartAfterEndError, WrongMatchFormat>> Errors { get; private set; } =
		new List<OneOf<EventSchedule, StartAfterEndError, WrongMatchFormat>>().ToReadOnly();

	public MatchFormat? MatchFormat { get; set; }

	public string? Name { get; set; }

	public string? EventCode { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public List<uint> Teams { get; } = [];

	public List<ScheduledMatch> Matches { get; } = [];

}

public class EventSchedule {

	public required MatchFormat MatchFormat { get; init; }

	public required string Name { get; init; }

	public required string EventCode { get; init; }

	public required DateTime StartDate { get; init; }

	public required DateTime EndDate { get; init; }

	public required ReadOnlyList<uint> Teams { get; init; }

	public required ReadOnlyList<ScheduledMatch> Matches { get; init; }
	// TODO add support for elim matches of various formats

	public static OneOf<EventSchedule, StartAfterEndError, WrongMatchFormat> Create(
		MatchFormat matchFormat,
		string name,
		string eventCode,
		DateTime startDate,
		DateTime endDate,
		ReadOnlyList<uint> teams,
		ReadOnlyList<ScheduledMatch> matches) {

		if (startDate > endDate) {
			return new StartAfterEndError {
				StartDateTime = startDate,
				EndDateTime = endDate
			};
		}

		foreach (ScheduledMatch scheduledMatch in matches) {

			if (!scheduledMatch.MatchesFormat(matchFormat)) {

				return new WrongMatchFormat {
					RequiredMatchFormat = matchFormat,
					ViolatingMatch = scheduledMatch
				};
			}
		}

		return new EventSchedule {
			MatchFormat = matchFormat,
			Name = name,
			EventCode = eventCode,
			StartDate = startDate,
			EndDate = endDate,
			Teams = teams,
			Matches = matches
		};
	}

}

public class StartAfterEndError : DomainError {

	public required DateTime StartDateTime { get; init; }

	public required DateTime EndDateTime { get; init; }
}

public class WrongMatchFormat : DomainError {

	public required MatchFormat RequiredMatchFormat { get; init; }

	public required ScheduledMatch ViolatingMatch { get; init; }
}



public class ScheduledMatch {

	public required ReadOnlyList<ReadOnlyList<(uint team, bool isSurrogate)>> Alliances { get; init; }

	public required DateTime? Time { get; init; }

	private ScheduledMatch() { }

	// Consider having an overload without time and using params for the alliances
	public static OneOf<ScheduledMatch, DuplicateTeamInMatch> Create(ReadOnlyList<ReadOnlyList<(uint team, bool isSurrogate)>> alliances, DateTime? time = null) {

		IEnumerable<uint> teams = alliances.SelectMany(alliance => alliance.Select(x => x.team));

		ReadOnlyList<uint> duplicates = teams.Duplicates().ToReadOnly();
		if (duplicates.Count != 0) {
			return DuplicateTeamInMatch.Create(duplicates) ?? throw new UnreachableException();
		}

		return new ScheduledMatch {
			Alliances = alliances,
			Time = time
		};
	}

	public bool MatchesFormat(MatchFormat matchFormat) {

		throw new NotImplementedException();
	}

}


public class DuplicateTeamInMatch : DomainError {

	public ReadOnlyList<uint> Duplicates { get; }

	private DuplicateTeamInMatch(ReadOnlyList<uint> duplicates) {

		Duplicates = duplicates;
	}

	public static DuplicateTeamInMatch? Create(ReadOnlyList<uint> duplicates) {

		return duplicates.Count == 0 ? null : new(duplicates);
	}

}

public class AsymmetricMatchCount : DomainError {

	//Dictionary<uint, uint> teamPlayCount = teams.ToDictionary(key => key, value => 0u);

	/// <summary>
	/// The key is a number of matches. The value is the collection of teams playing that many matches.
	/// </summary>
	public required ReadOnlyDictionary<int, ReadOnlyList<uint>> TeamMatches { get; init; }

	public required EventSchedule EventSchedules { get; init; }
}