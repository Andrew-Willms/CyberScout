using System.Collections.Generic;
using Domain.Data;

namespace Domain.Dtos;



public record ImportEditedMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required uint MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required uint OriginalMatchId { get; init; }

	public required List<(string deviceId, uint matchId)> OtherParents { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	private ImportEditedMatchDataDto() { }

	// todo: propper errors
	public static ImportEditedMatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		uint matchId,
		string originalDeviceId,
		uint originalMatchId,
		List<(string deviceId, uint matchId)> parents,
		string gameDeviceId,
		uint gameId) {

		// The current match cannot be before the original match.
		if (deviceId == originalDeviceId && matchId < originalMatchId) {
			return null;
		}

		// There must be at least one parent.
		if (parents.Count == 0) {
			return null;
		}

		foreach ((string deviceId, uint matchId) parent in parents) {

			// The current match cannot be before a parent match.
			if (deviceId == parent.deviceId && matchId < parent.matchId) {
				return null;
			}

			// A parent match cannot be before the original match.
			if (parent.deviceId == originalDeviceId && parent.matchId < originalMatchId) {
				return null;
			}
		}

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			MatchId = matchId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			OtherParents = parents,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}