using System.Collections.Generic;
using Domain.Data;

namespace Comms.Dtos;



public class NewEditedMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public required List<(string deviceId, long matchId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventId { get; init; }



	private NewEditedMatchDataDto() { }

	public static NewEditedMatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		string originalDeviceId,
		long originalMatchId,
		List<(string deviceId, long matchId)> parents,
		string gameDeviceId,
		long gameId,
		string eventDeviceId,
		long eventId) {

		// There must be at least one parent.
		if (parents.Count == 0) {
			return null;
		}

		foreach ((string deviceId, long matchId) parent in parents) {

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
			GameId = gameId,
			EventDeviceId = eventDeviceId,
			EventId = eventId
		};
	}

}