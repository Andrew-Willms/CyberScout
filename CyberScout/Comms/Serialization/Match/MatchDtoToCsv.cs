using System;
using System.Collections.Generic;
using System.Text;
using Comms.Dtos.Match;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Optional;
using UtilitiesLibrary.Results;

namespace Comms.Serialization.Match;



public static class MatchDtoToCsv {

	private static readonly string FixedCsvHeader =
		nameof(MatchDto.DeviceId) + ',' +
		nameof(MatchDto.MatchId) + ',' +
		nameof(MatchDto.OriginalDeviceId) + ',' +
		nameof(MatchDto.OriginalMatchId) + ',' +
		nameof(MatchDto.Parents) + ',' +
		nameof(MatchDto.GameDeviceId) + ',' +
		nameof(MatchDto.GameId) + ',';

	private const int FixedFieldCount = 15;

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		string matchDataHeaders = MatchDataToCsv.GetCsvHeaders(gameSpecification);
		return FixedCsvHeader + matchDataHeaders;
	}

	public static string Serialize(MatchDto matchData) {

		// Estimate the device and match IDs will be about 100 characters and each field will take about 5.
		StringBuilder stringBuilder = new(100 + matchData.Data.GameSpecification.DataFields.Count * 5);
		stringBuilder.Append(matchData.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.MatchId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.OriginalDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.OriginalMatchId);
		stringBuilder.Append(',');
		stringBuilder.Append(Parents.ToText(matchData.Parents));
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.GameDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.GameId);
		stringBuilder.Append(',');

		string matchDataSerialized = MatchDataToCsv.Serialize(matchData.Data);
		stringBuilder.Append(matchDataSerialized);
		return stringBuilder.ToString();
	}

	public static Result<MatchDto> Deserialize(string matchData, GameSpec gameSpecification) {

		List<string> columns = matchData.SplitTextToCsvColumns();

		if (columns.Count != FixedFieldCount + gameSpecification.DataFields.Count) {
			return null;
		}

		// todo: Create a DeviceId class with a TryParse method like this and create local variables for even the string types.
		// Not all strings are valid DeviceIds or text for the relevant field so this additional validation may be useful.

		string deviceId = columns[0];
		bool success = long.TryParse(columns[1], out long matchId);
		string originalDeviceId = columns[2];
		success &= long.TryParse(columns[3], out long originalMatchId);
		string parentsText = columns[4];
		string gameDeviceId = columns[5];
		success &= long.TryParse(columns[6], out long gameId);


		string eventCode = columns[7];
		string scoutName = columns[8];
		string matchGroupName = columns[9];
		string matchName = columns[10];
		success &= uint.TryParse(columns[11], out uint replayNumber);
		success &= uint.TryParse(columns[12], out uint allianceIndex);
		success &= uint.TryParse(columns[13], out uint teamNumber);
		success &= DateTime.TryParse(columns[14], out DateTime startTime);

		if (!success) {
			return null;
		}

		Result<List<(string deviceId, long matchId)>> parentsResult = Parents.FromText(parentsText);
		if (parentsResult.IsFailure) {
			return null;
		}
		List<(string deviceId, long matchId)> parents = parentsResult.Value;

		List<object> dataFieldValues = [];
		for (int i = 0; i < gameSpecification.DataFields.Count; i++) {

			string value = columns[i + 13];

			switch (gameSpecification.DataFields[i]) {

				case BooleanDataFieldSpec:
					switch (value) {
						case "1":
							dataFieldValues.Add(true);
							continue;
						case "0":
							dataFieldValues.Add(false);
							continue;
						default:
							return null;
					}

				case TextDataFieldSpec:
					dataFieldValues.Add(value);
					break;

				// TODO: I should probably parse the value and make sure it's within the bounds defined by the DataField
				case IntegerDataFieldSpec: {
					if (!int.TryParse(value, out int result)) {
						return null;
					}
					dataFieldValues.Add(result);
					break;
				}
				case SelectionDataFieldSpec selectionSpec: {

					if (value == string.Empty) {
						dataFieldValues.Add(Optional.NoValue);
						break;
					}

					if (!int.TryParse(value, out int result)) {
						return null;
					}

					if (result < 0 || result >= selectionSpec.Options.Count) {
						return null;
					}

					dataFieldValues.Add(selectionSpec.Options[result].Optionalize());
					break;
				}
			}
		}

		MatchData? matchDataObject = MatchData.FromRaw(
			gameSpecification: gameSpecification,
			eventCode: eventCode,
			scoutName: scoutName,
			match: new() {
				MatchGroupName = matchGroupName,
				MatchName = matchName,
				ReplayNumber = replayNumber
			},
			teamNumber,
			allianceIndex,
			startTime,
			dataFieldValues.ToReadOnly());

		if (matchDataObject is null) {
			return null;
		}

		return MatchDto.Create(
			matchData: matchDataObject,
			deviceId: deviceId,
			matchId: matchId,
			originalDeviceId: originalDeviceId,
			originalMatchId: originalMatchId,
			parents: parents,
			gameDeviceId: gameDeviceId,
			gameId: gameId
		).Value;
	}

}