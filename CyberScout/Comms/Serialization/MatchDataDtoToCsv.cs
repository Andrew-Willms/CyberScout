using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Comms.Dtos;
using Domain.Data;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Optional;

namespace Comms.Serialization;



public static class MatchDataDtoToCsv {

	private static readonly string FixedCsvHeader =
		nameof(MatchDataDto.DeviceId) + ',' +
		nameof(MatchDataDto.MatchId) + ',' +
		nameof(MatchDataDto.OriginalDeviceId) + ',' +
		nameof(MatchDataDto.OriginalMatchId) + ',' +
		nameof(MatchDataDto.Parents) + ',' +
		nameof(MatchDataDto.GameDeviceId) + ',' +
		nameof(MatchDataDto.GameId) + ',' +
		nameof(MatchData.EventCode) + ',' +
		nameof(MatchData.ScoutName) + ',' +
		nameof(MatchData.Match.MatchNumber) + ',' +
		nameof(MatchData.Match.Type) + ',' +
		nameof(MatchData.Match.ReplayNumber) + ',' +
		nameof(MatchData.AllianceIndex) + ',' +
		nameof(MatchData.TeamNumber) + ',' +
		nameof(MatchData.StartTime) + ',' +
		nameof(MatchData.EndTime) + ',';

	private const int FixedFieldCount = 15;

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		// The default buffer size is 16 and that seems reasonable for the length of a DataField name.
		StringBuilder stringBuilder = new(FixedCsvHeader, FixedCsvHeader.Length + gameSpecification.DataFields.Count * 16);
		stringBuilder.Append(',');
		stringBuilder.AppendJoin(",", gameSpecification.DataFields.Select(x => x.Name.ToCsvFriendly()));
		return stringBuilder.ToString();
	}

	public static string Serialize(MatchDataDto importMatchData) {

		// Estimate the device and match IDs will be about 50 characters and each field will take about 5.
		StringBuilder stringBuilder = new(50 + importMatchData.Data.GameSpecification.DataFields.Count * 5);
		stringBuilder.Append(importMatchData.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.OriginalDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.OriginalMatchId);
		stringBuilder.Append(',');

		// List of parents in the format "parent1DeviceId:parent1MatchId;parent2DeviceId:parent2MatchId"
		stringBuilder.AppendJoin(";", importMatchData.Parents.Select(parent => $"{parent.deviceId}:{parent.matchdId}"));
		stringBuilder.Append(',');

		stringBuilder.Append(importMatchData.GameDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.GameId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.ScoutName.ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.EventCode);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.Match.MatchNumber);
		stringBuilder.Append(',');
		stringBuilder.Append((int)importMatchData.Data.Match.Type);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.Match.ReplayNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.AllianceIndex);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.TeamNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.StartTime.ToString("o").ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.Data.EndTime.ToString("o").ToCsvFriendly());

		for (int i = 0; i < importMatchData.Data.DataFields.Count; i++) {

			switch (importMatchData.Data.GameSpecification.DataFields[i], importMatchData.Data.DataFields[i]) {
				case (BooleanDataFieldSpec, bool value): {
					stringBuilder.Append(value ? ",1" : ",0");
					break;
				}
				case (TextDataFieldSpec, string value): {
					stringBuilder.Append(',');
					stringBuilder.Append(value.ToCsvFriendly());
					break;
				}
				case (IntegerDataFieldSpec, int value): {
					stringBuilder.Append(',');
					stringBuilder.Append(value);
					break;
				}
				case (SelectionDataFieldSpec selectionDataFieldSpec, Optional<string> optional): {

					if (!optional.HasValue) {
						stringBuilder.Append(',');
						break;
					}
					stringBuilder.Append(',');
					stringBuilder.Append(selectionDataFieldSpec.Options.ToList().IndexOf(optional.Value)); // todo lazy af
					break;
				}
				// It's very unfortunate that Optional.NoValue is a different type like this. It breaks a lot of things
				// Todo: consider changing the Optional<> type to own the NoValue thing
				case (SelectionDataFieldSpec, Optional): {
					stringBuilder.Append(',');
					break;
				}
				default:
					throw new UnreachableException();
			}
		}

		return stringBuilder.ToString();
	}

	public static MatchDataDto? Deserialize(string matchData, GameSpec gameSpecification) {

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
		success &= uint.TryParse(columns[9], out uint matchNumber);
		success &= Enum.TryParse(columns[10], out MatchType type);
		success &= uint.TryParse(columns[11], out uint replayNumber);
		success &= uint.TryParse(columns[12], out uint allianceIndex);
		success &= uint.TryParse(columns[13], out uint teamNumber);
		success &= DateTime.TryParse(columns[14], out DateTime startTime);
		success &= DateTime.TryParse(columns[15], out DateTime endTime);

		if (!success) {
			return null;
		}

		string[] parentsTextSplit = parentsText.Split(';');
		List<(string deviceId, long matchId)> parents = [];
		foreach (string parentText in parentsTextSplit) {

			string[] parentComponents = parentText.Split(':');

			if (parentComponents.Length != 2) {
				return null;
			}

			// todo: Same DeviceId validation as above.
			string parentDeviceId = parentComponents[0];

			if (long.TryParse(parentComponents[1], out long parentMatchId)) {
				return null;
			}

			parents.Add((parentDeviceId, parentMatchId));
		}


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
			eventSchedule: null,
			scoutName: scoutName,
			match: new() {
				MatchNumber = matchNumber,
				ReplayNumber = replayNumber,
				Type = type
			},
			teamNumber,
			allianceIndex,
			startTime,
			endTime,
			dataFieldValues.ToReadOnly());

		if (matchDataObject is null) {
			return null;
		}

		return MatchDataDto.Create(
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