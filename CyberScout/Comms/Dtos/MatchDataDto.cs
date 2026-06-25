using System.Collections.Generic;
using Domain.Data;

namespace Comms.Dtos;



public class MatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required long MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public required List<(string deviceId, long matchdId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventMetaDataId { get; init; }



	private MatchDataDto() { }

	// todo: proper errors
	public static MatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		long matchId,
		string originalDeviceId,
		long originalMatchId,
		List<(string deviceId, long matchId)> parents,
		string gameDeviceId,
		long gameId,
		string eventDeviceId,
		long eventId) {

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

		foreach ((string deviceId, long matchId) parent in parents) {

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
			GameId = gameId,
			EventDeviceId = eventDeviceId,
			EventMetaDataId = eventId
		};
	}

}