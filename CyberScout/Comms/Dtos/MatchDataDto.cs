using System.Collections.Generic;
using Domain.Data;

namespace Comms.Dtos;



public class MatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalRecordId { get; init; }

	public required List<(string deviceId, long recordId)> Parents { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameRecordId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventRecordId { get; init; }



	private MatchDataDto() { }

	// todo: proper errors
	public static MatchDataDto? Create(
		MatchData matchData,
		string deviceId,
		long recordId,
		string originalDeviceId,
		long originalRecordId,
		List<(string deviceId, long recordId)> parents,
		string gameDeviceId,
		long gameRecordId,
		string eventDeviceId,
		long eventRecordId) {

		// The current match cannot be before the original match.
		if (deviceId == originalDeviceId && recordId < originalRecordId) {
			return null;
		}

		// If the current match is the original match there must be no parents.
		if (deviceId == originalDeviceId && recordId == originalRecordId && parents.Count != 0) {
			return null;
		}

		// If the current match isn't the original match there must be at least one parent.
		if ((deviceId != originalDeviceId || recordId != originalRecordId) && parents.Count == 0) {
			return null;
		}

		foreach ((string deviceId, long recordId) parent in parents) {

			// The current match cannot be before a parent match.
			if (deviceId == parent.deviceId && recordId < parent.recordId) {
				return null;
			}

			// A parent match cannot be before the original match.
			if (parent.deviceId == originalDeviceId && parent.recordId < originalRecordId) {
				return null;
			}
		}

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			RecordId = recordId,
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