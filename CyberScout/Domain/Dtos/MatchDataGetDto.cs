using System.Collections.Generic;
using Domain.Data;

namespace Domain.Dtos;



public class MatchDataGetDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required uint MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required uint OriginalMatchId { get; init; }

	public required List<(string deviceId, uint matchId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	private MatchDataGetDto() { }

	// todo: proper errors
	public static MatchDataGetDto? Create(
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

		// If the current match is the original match there must be no parents.
		if (deviceId == originalDeviceId && matchId == originalMatchId && parents.Count != 0) {
			return null;
		}

		// If the current match isn't the original match there must be at least one parent.
		if ((deviceId != originalDeviceId || matchId != originalMatchId) && parents.Count == 0) {
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
			Parents = parents,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}