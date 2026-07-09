using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



public record ScheduledMatch {

	public required string Name { get; init; }

	public required ReadOnlyList<ScheduledAlliance> Alliances { get; init; }

	private ScheduledMatch() { }

	public static Result<ScheduledMatch, DuplicateTeamInMatch> Create(string name, List<ScheduledAlliance> alliances) {

		IEnumerable<uint> teams = alliances.SelectMany(alliance => alliance.Teams.Select(team => team.number));

		ReadOnlyList<uint> duplicates = teams.Duplicates().ToReadOnly();
		if (duplicates.Count != 0) {
			return DuplicateTeamInMatch.Create(duplicates) ?? throw new UnreachableException();
		}

		return new ScheduledMatch {
			Name = name,
			Alliances = alliances.ToReadOnly()
		};
	}

	public bool MatchesFormat(MatchFormat matchFormat) {

		if (Alliances.Count != matchFormat.Alliances.Count) {
			return false;
		}

		for (int i = 0; i < Alliances.Count; i++) {

			if (Alliances[i].Name != matchFormat.Alliances[i].Name) {
				return false;
			}

			if (Alliances[i].Teams.Count != matchFormat.Alliances[i].TeamCount) {
				return false;
			}
		}

		return true;
	}

}