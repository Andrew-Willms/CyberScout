using System.Collections.Generic;
using UtilitiesLibrary.Collections;

namespace Domain.Dtos.Match;



public class EditedMatchDto {

	public required MatchData.MatchData Data { get; init; }

	public required string DeviceId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public required ReadOnlyList<(string deviceId, long matchId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public string EventCode => Data.EventCode;



	private EditedMatchDto() { }

	public static EditedMatchDto? Create(
		MatchData.MatchData matchData,
		string deviceId,
		string originalDeviceId,
		long originalMatchId,
		List<(string deviceId, long matchId)> parents,
		string gameDeviceId,
		long gameId) {

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
			Data = matchData,
			DeviceId = deviceId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			Parents = parents.ToReadOnly(),
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}