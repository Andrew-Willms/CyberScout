using System.Collections.Generic;
using Domain.Data;

namespace Comms.Dtos;



public class NewEditedMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalRecordId { get; init; }

	public required List<(string deviceId, long recordId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameRecordId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventRecordId { get; init; }



	private NewEditedMatchDataDto() { }

	public static NewEditedMatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		string originalDeviceId,
		long originalRecordId,
		List<(string deviceId, long recordId)> parents,
		string gameDeviceId,
		long gameRecordId,
		string eventDeviceId,
		long eventRecordId) {

		// There must be at least one parent.
		if (parents.Count == 0) {
			return null;
		}

		foreach ((string deviceId, long recordId) parent in parents) {

			// A parent match cannot be before the original match.
			if (parent.deviceId == originalDeviceId && parent.recordId < originalRecordId) {
				return null;
			}
		}

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			OriginalDeviceId = originalDeviceId,
			OriginalRecordId = originalRecordId,
			Parents = parents,
			GameDeviceId = gameDeviceId,
			GameRecordId = gameRecordId,
			EventDeviceId = eventDeviceId,
			EventRecordId = eventRecordId
		};
	}

}