using System.Collections.Generic;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Comms.Dtos;



// TODO move this out of comms... but it relies on DTOs...
public record EditGraph {

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public required ReadOnlyList<MatchDataDto> Nodes { get; init; }



	private EditGraph() { }

	public static Result<EditGraph> Create(List<MatchDataDto> matchDataDtos) {

		if (matchDataDtos.Count == 0) {
			return new AdHocError("Match dtos list is empty.");
		}

		List<MatchDataDto> duplicateMatches = matchDataDtos.Duplicates();
		if (duplicateMatches.Count != 0) {
			return new AdHocError("Duplicate matches.");
		}

		MatchDataDto? originalMatch = null;
		string originalDeviceId = matchDataDtos[0].OriginalDeviceId;
		long originalMatchId = matchDataDtos[0].OriginalMatchId;

		if (matchDataDtos.Any(x => x.OriginalDeviceId != originalDeviceId || x.OriginalMatchId != originalMatchId)) {
			return new AdHocError("Not all matches have the same OriginalDeviceId and OriginalMatchId.");
		}

		return new EditGraph {
			OriginalDeviceId = matchDataDtos[0].OriginalDeviceId,
			OriginalMatchId = matchDataDtos[0].OriginalMatchId,
			Nodes = matchDataDtos.ToReadOnly()
		};
	}

}