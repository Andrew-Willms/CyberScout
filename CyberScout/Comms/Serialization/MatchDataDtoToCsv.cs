using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Comms.Dtos;
using Domain.Data;
using Domain.GameSpecification;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Optional;

namespace Comms.Serialization;



public static class MatchDataDtoToCsv {

	private static readonly string FixedCsvHeader =
		nameof(MatchDataDto.DeviceId) + ',' +
		nameof(MatchDataDto.RecordId) + ',' +
		nameof(MatchDataDto.OriginalDeviceId) + ',' +
		nameof(MatchDataDto.OriginalRecordId) + ',' +
		nameof(MatchDataDto.Parents) + ',' +
		nameof(MatchDataDto.GameDeviceId) + ',' +
		nameof(MatchDataDto.GameRecordId) + ',' +
		nameof(MatchData.ScoutName) + ',' +
		nameof(MatchData.EventCode) + ',' +
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
		StringBuilder stringBuilder = new(50 + importMatchData.MatchData.GameSpecification.DataFields.Count * 5);
		stringBuilder.Append(importMatchData.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.RecordId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.OriginalDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.OriginalRecordId);
		stringBuilder.Append(',');

		// List of parents in the format "(parent1DeviceId,parent1MatchId),(parent2DeviceId,parent2MatchId)"
		stringBuilder.AppendJoin(";", importMatchData.Parents.Select(parent => $"{parent.deviceId}:{parent.recordId}"));
		stringBuilder.Append(',');

		stringBuilder.Append(importMatchData.GameDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.GameRecordId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.ScoutName.ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.EventCode);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.Match.MatchNumber);
		stringBuilder.Append(',');
		stringBuilder.Append((int)importMatchData.MatchData.Match.Type);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.Match.ReplayNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.AllianceIndex);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.TeamNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.StartTime.ToString("o").ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.MatchData.EndTime.ToString("o").ToCsvFriendly());

		for (int i = 0; i < importMatchData.MatchData.DataFields.Count; i++) {

			switch (importMatchData.MatchData.GameSpecification.DataFields[i], importMatchData.MatchData.DataFields[i]) {
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
				case (MultiIntegerDataFieldSpec, int value): {
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
		bool success = uint.TryParse(columns[1], out uint matchId);
		string originalDeviceId = columns[2];
		success &= uint.TryParse(columns[3], out uint originalMatchId);
		string parentsText = columns[4];
		string gameDeviceId = columns[5];
		success &= uint.TryParse(columns[6], out uint gameId);
		string scoutName = columns[7];
		string eventCode = columns[8];
		success &= uint.TryParse(columns[9], out uint matchNumber);
		success &= Enum.TryParse(columns[10], out MatchType type);
		success &= uint.TryParse(columns[11], out uint replayNumber);
		success &= uint.TryParse(columns[12], out uint allianceIndex);
		success &= uint.TryParse(columns[113], out uint teamNumber);
		success &= DateTime.TryParse(columns[14], out DateTime startTime);
		success &= DateTime.TryParse(columns[15], out DateTime endTime);

		if (!success) {
			return null;
		}

		string[] parentsTextSplit = parentsText.Split(';');
		List<(string deviceId, uint recordId)> parents = [];
		foreach (string parentText in parentsTextSplit) {

			string[] parentComponents = parentText.Split(':');

			if (parentComponents.Length != 2) {
				return null;
			}

			// todo: Same DeviceId validation as above.
			string parentDeviceId = parentComponents[0];

			if (uint.TryParse(parentComponents[1], out uint parentMatchNumber)) {
				return null;
			}

			parents.Add((parentDeviceId, parentMatchNumber));
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

				case IntegerDataFieldSpec:
				case MultiIntegerDataFieldSpec: {
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
			recordId: matchId,
			originalDeviceId: originalDeviceId,
			originalRecordId: originalMatchId,
			parents: parents,
			gameDeviceId: gameDeviceId,
			gameRecordId: gameId
		);
	}

}