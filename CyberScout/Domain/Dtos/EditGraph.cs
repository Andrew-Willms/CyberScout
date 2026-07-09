using System.Collections.Generic;
using System.Linq;
using Domain.Dtos.Match;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.Dtos;



// TODO move this out of comms... but it relies on DTOs...
// It's possible that the way I have structured it the DTOs are just an integral part of the Domain and should live there...
public record EditGraph {

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public required ReadOnlyList<MatchDto> Nodes { get; init; }



	private EditGraph() { }

	public static Result<EditGraph> Create(List<MatchDto> matchDataDtos) {

		if (matchDataDtos.Count == 0) {
			return new AdHocError("Match dtos list is empty.");
		}

		List<MatchDto> duplicateMatches = matchDataDtos.Duplicates();
		if (duplicateMatches.Count != 0) {
			return new AdHocError("Duplicate matches.");
		}

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