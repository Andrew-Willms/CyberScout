using System.Collections.Generic;
using Domain.Data;

namespace Domain.Dtos;



public record ImportEditedMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required uint MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required uint OriginalMatchId { get; init; }

	public required string FirstParentDeviceId { get; init; }

	public required uint FirstParentMatchId { get; init; }

	public required List<(string deviceId, uint matchId)> OtherParents { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	private ImportEditedMatchDataDto() { }

	public static ImportEditedMatchDataDto Create(
		MatchData matchData,
		string deviceId,
		uint matchId,
		string originalDeviceId,
		uint originalMatchId,
		string firstParentDeviceId,
		uint firstParentMatchId,
		List<(string deviceId, uint matchId)> otherParents,
		string gameDeviceId,
		uint gameId) {

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			MatchId = matchId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			FirstParentDeviceId = firstParentDeviceId,
			FirstParentMatchId = firstParentMatchId,
			OtherParents = otherParents,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}