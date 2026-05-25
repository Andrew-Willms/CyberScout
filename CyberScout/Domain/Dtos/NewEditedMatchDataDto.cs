using System.Collections.Generic;
using Domain.Data;

namespace Domain.Dtos;



public class NewEditedMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required uint OriginalMatchId { get; init; }

	public required List<(string deviceId, uint matchId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	private NewEditedMatchDataDto() { }

	public static NewEditedMatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		string originalDeviceId,
		uint originalMatchId,
		List<(string deviceId, uint matchId)> parents,
		string gameDeviceId,
		uint gameId) {

		// There must be at least one parent.
		if (parents.Count == 0) {
			return null;
		}

		foreach ((string deviceId, uint matchId) parent in parents) {

			// A parent match cannot be before the original match.
			if (parent.deviceId == originalDeviceId && parent.matchId < originalMatchId) {
				return null;
			}
		}

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			Parents = parents,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}