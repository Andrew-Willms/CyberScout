using System.Collections.Generic;
using UtilitiesLibrary.Collections;

namespace Domain.EventSchedule;



public record MatchFormat {

	public static readonly MatchFormat Standard = new() {
		Alliances = new List<(string allianceName, uint teamCount)> {("Red Alliance", 3), ("Blue Alliance", 3)}.ToReadOnly()
	};

	public required ReadOnlyList<(string allianceName, uint teamCount)> Alliances { get; init; }

}