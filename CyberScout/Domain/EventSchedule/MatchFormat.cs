using System.Collections.Generic;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



public record MatchFormat {

	public static readonly MatchFormat Standard = new() {
		Alliances = new List<AllianceDefinition> {
			AllianceDefinition.Create("Red Alliance", 3).Value!, 
			AllianceDefinition.Create("Blue Alliance", 3).Value!
		}.ToReadOnly()
	};

	// TODO sequence equality comparison
	public required ReadOnlyList<AllianceDefinition> Alliances { get; init; }

	private MatchFormat() { }


	public static Result<MatchFormat> Create(List<AllianceDefinition> alliances) {

		if (alliances.Select(alliance => alliance.Name).Duplicates().Count != 0) {
			return new AdHocError("Duplicate alliance names.", ("alliances", alliances.ToString() ?? string.Empty));
		}

		return new MatchFormat {
			Alliances = alliances.ToReadOnly()
		};
	}

}