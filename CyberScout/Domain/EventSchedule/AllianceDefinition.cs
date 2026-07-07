using System.Diagnostics.CodeAnalysis;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



public record AllianceDefinition {

	public required string Name { get; init; }

	public required uint TeamCount { get; init; }

	[SetsRequiredMembers]
	private AllianceDefinition(string name, uint teamCount) {
		Name = name;
		TeamCount = teamCount;
	}

	public static Result<AllianceDefinition> Create(string name, uint teamCount) {

		if (teamCount == 0) {
			return new AdHocError("Team count cannot be 0.");
		}

		return new AllianceDefinition(name, teamCount);
	}

}