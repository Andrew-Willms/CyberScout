using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Comms.Dtos;
using Domain.Data;
using Domain.GameSpecification;
using OneOf;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Optional;

namespace Comms.Serialization;



[GenerateOneOf]
public partial class MatchDataDeserializationResult : OneOfBase<MatchData, MatchDataDeserializationError>;

// TODO: Consider making this a OneOf<> to enforce exhaustive matching on switch
// The SerializedMatchData and GameSpecification properties could be moved to an interface?
public abstract class MatchDataDeserializationError {

	public required string SerializedMatchData { get; init; }

	public required GameSpec GameSpecification { get; init; }

}

public class WrongNumberOfCsvColumnsError : MatchDataDeserializationError {

	public required uint ExpectedColumnCount { get; init; }

	public required ReadOnlyList<string> Columns { get; init; }

}

public class CouldNotParseValuesError : MatchDataDeserializationError {

	public required ReadOnlyList<CoreValueError> CoreValueErrors { get; init; }

	public required ReadOnlyList<DataFieldError> DataFieldErrors { get; init; }
}

public class CoreValueError {

	public required int ColumnIndex { get; init; }

	public required Type ExpectedType { get; init; }

	public required string Text { get; init; }
}

public class DataFieldError {

	public required DataFieldSpec DataField { get; init; }

	public required string Text { get; init; }

}




public static class MatchDataToCsv {

	private const int ScoutNameColumnIndex = 0;
	private const int EventCodeColumnIndex = 1;
	private const int MatchNumberColumnIndex = 2;
	private const int MatchTypeColumnIndex = 3;
	private const int ReplayNumberColumnIndex = 4;
	private const int AllianceColumnIndex = 5;
	private const int TeamNumberColumnIndex = 6;
	private const int StartTimeColumnIndex = 7;
	private const int EndTimeColumnIndex = 8;
	private const int CountOfBuiltInFields = 9;

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		StringBuilder stringBuilder = new("ScoutName,EventCode,MatchNumber,MatchType,ReplayNumber,AllianceIndex,TeamNumber,StartTime,EndTime,");

		stringBuilder.AppendJoin(",", gameSpecification.DataFields.Select(x => x.Name.ToCsvFriendly()));

		return stringBuilder.ToString();
	}

	public static string Serialize(MatchData matchData) {

		StringBuilder stringBuilder = new(matchData.ScoutName.ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.EventCode);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.Match.MatchNumber);
		stringBuilder.Append(',');
		stringBuilder.Append((int)matchData.Match.Type);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.Match.ReplayNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.AllianceIndex);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.TeamNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.StartTime.ToString("o").ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.EndTime.ToString("o").ToCsvFriendly());

		for (int i = 0; i < matchData.DataFields.Count; i++) {

			switch (matchData.GameSpecification.DataFields[i], matchData.DataFields[i]) {
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
				// TODO: Optional doesn't match the Optional<T> type
				case (SelectionDataFieldSpec, Optional): {
					stringBuilder.Append(',');
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
				default:
					throw new UnreachableException();
			}
		}

		return stringBuilder.ToString();
	}

	public static MatchDataDeserializationResult Deserialize(string matchData, GameSpec gameSpecification) {

		List<string> columns = matchData.SplitTextToCsvColumns();
		uint expectedColumnCount = CountOfBuiltInFields + (uint)gameSpecification.DataFields.Count;

		if (columns.Count != expectedColumnCount) {

			return new WrongNumberOfCsvColumnsError {
				SerializedMatchData = matchData,
				GameSpecification = gameSpecification,
				ExpectedColumnCount = expectedColumnCount,
				Columns = columns.ToReadOnly(),
			};
		}

		List<CoreValueError> coreValueErrors = [];
		
		if (!uint.TryParse(columns[MatchNumberColumnIndex], out uint matchNumber)) {
			coreValueErrors.Add(new() {
				ColumnIndex = MatchNumberColumnIndex,
				ExpectedType = typeof(uint),
				Text = columns[MatchNumberColumnIndex]
			});
		}

		if (!Enum.TryParse(columns[MatchTypeColumnIndex], out MatchType type)) {
			coreValueErrors.Add(new() {
				ColumnIndex = MatchTypeColumnIndex,
				ExpectedType = typeof(MatchType),
				Text = columns[MatchTypeColumnIndex]
			});
		}

		if (!uint.TryParse(columns[ReplayNumberColumnIndex], out uint replayNumber)) {
			coreValueErrors.Add(new() {
				ColumnIndex = ReplayNumberColumnIndex,
				ExpectedType = typeof(uint),
				Text = columns[ReplayNumberColumnIndex]
			});
		}

		if (!uint.TryParse(columns[AllianceColumnIndex], out uint allianceIndex)) {
			coreValueErrors.Add(new() {
				ColumnIndex = AllianceColumnIndex,
				ExpectedType = typeof(uint),
				Text = columns[AllianceColumnIndex]
			});
		}

		if (!uint.TryParse(columns[TeamNumberColumnIndex], out uint teamNumber)) {
			coreValueErrors.Add(new() {
				ColumnIndex = TeamNumberColumnIndex,
				ExpectedType = typeof(uint),
				Text = columns[TeamNumberColumnIndex]
			});
		}

		if (!DateTime.TryParse(columns[StartTimeColumnIndex], out DateTime startTime)) {
			coreValueErrors.Add(new() {
				ColumnIndex = StartTimeColumnIndex,
				ExpectedType = typeof(DateTime),
				Text = columns[StartTimeColumnIndex]
			});
		}

		if (!DateTime.TryParse(columns[EndTimeColumnIndex], out DateTime endTime)) {
			coreValueErrors.Add(new() {
				ColumnIndex = EndTimeColumnIndex,
				ExpectedType = typeof(DateTime),
				Text = columns[EndTimeColumnIndex]
			});
		}

		List<DataFieldError> dataFieldErrors = [];

		List<object> dataFieldValues = [];
		for (int i = 0; i < gameSpecification.DataFields.Count; i++) {

			int csvColumnIndex = i + CountOfBuiltInFields;
			string value = columns[csvColumnIndex];

			switch (gameSpecification.DataFields[i]) {

				case BooleanDataFieldSpec dataFieldSpec: {
					switch (value) {
						case "1":
							dataFieldValues.Add(true);
							break;
						case "0":
							dataFieldValues.Add(false);
							break;
						default:
							dataFieldErrors.Add(new() {
								DataField = dataFieldSpec,
								Text = value
							});
							break;
					}
					break;
				}
				case TextDataFieldSpec:
					dataFieldValues.Add(value);
					break;

				case IntegerDataFieldSpec dataFieldSpec: {

					if (int.TryParse(value, out int result)) {
						dataFieldValues.Add(result);
						break;
					}

					dataFieldErrors.Add(new() {
						DataField = dataFieldSpec,
						Text = value
					});
					break;
				}

				case MultiIntegerDataFieldSpec dataFieldSpec: {

					if (int.TryParse(value, out int result)) {
						dataFieldValues.Add(result);
						break;
					}

					dataFieldErrors.Add(new() {
						DataField = dataFieldSpec,
						Text = value
					});
					break;
				}

				case SelectionDataFieldSpec dataFieldSpec: {

					if (value == string.Empty) {

						if (!dataFieldSpec.RequiresValue) {
							dataFieldValues.Add(Optional.NoValue);
							break;
						}

						dataFieldErrors.Add(new() {
							DataField = dataFieldSpec,
							Text = string.Empty
						});

						break;
					}

					if (!uint.TryParse(value, out uint result) || result >= dataFieldSpec.Options.Count) {

						dataFieldErrors.Add(new() {
							DataField = dataFieldSpec,
							Text = value
						});
						break;
					}

					dataFieldValues.Add(dataFieldSpec.Options[(int)result].Optionalize());
					break;
				}
			}
		}

		if (coreValueErrors.Count > 0) {
			return new CouldNotParseValuesError {
				SerializedMatchData = matchData,
				GameSpecification = gameSpecification,
				CoreValueErrors = coreValueErrors.ToReadOnly(),
				DataFieldErrors = dataFieldErrors.ToReadOnly()
			};
		}

		return MatchData.FromRaw(
			gameSpecification: gameSpecification,
			eventCode: columns[1] == string.Empty ? null : columns[1],
			eventSchedule: null,
			scoutName: columns[0],
			match: new() {
				MatchNumber = matchNumber,
				ReplayNumber = replayNumber,
				Type = type
			},
			teamNumber,
			allianceIndex,
			startTime,
			endTime,
			dataFieldValues.ToReadOnly()
		);
	}

}

public static class MatchDataDtoToCsv {

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		StringBuilder stringBuilder = new("DeviceId,RecordId,EditDeviceId,EditRecordId,ScoutName,EventCode,MatchNumber,MatchType,ReplayNumber,AllianceIndex,TeamNumber,StartTime,EndTime,");

		stringBuilder.AppendJoin(",", gameSpecification.DataFields.Select(x => x.Name.ToCsvFriendly()));

		return stringBuilder.ToString();
	}

	public static string Serialize(ImportMatchDataDto importMatchData) {

		StringBuilder stringBuilder = new(importMatchData.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.RecordId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.EditBasedOn?.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(importMatchData.EditBasedOn?.RecordId);
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

	public static ImportMatchDataDto? Deserialize(string matchData, GameSpec gameSpecification) {

		List<string> columns = matchData.SplitTextToCsvColumns();

		if (columns.Count != 13 + gameSpecification.DataFields.Count) {
			return null;
		}

		bool success = int.TryParse(columns[1], out int recordId);
		success &= uint.TryParse(columns[6], out uint matchNumber);
		success &= Enum.TryParse(columns[7], out MatchType type);
		success &= uint.TryParse(columns[8], out uint replayNumber);
		success &= uint.TryParse(columns[9], out uint allianceIndex);
		success &= uint.TryParse(columns[10], out uint teamNumber);
		success &= DateTime.TryParse(columns[11], out DateTime startTime);
		success &= DateTime.TryParse(columns[12], out DateTime endTime);

		if (!success) {
			return null;
		}

		(string, int)? editBasedOn;
		switch (columns[2] == string.Empty, columns[3] == string.Empty) {
			case (true, true):
				editBasedOn = null;
				break;
			case (false, false):
				if (!int.TryParse(columns[3], out int editRecordId)) {
					return null;
				}
				editBasedOn = (columns[2], editRecordId);
				break;
			default:
				return null;
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
			eventCode: columns[5] == string.Empty ? null : columns[5],
			eventSchedule: null,
			scoutName: columns[4],
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

		return new() {
			MatchData = matchDataObject,
			DeviceId = columns[0],
			RecordId = recordId,
			EditBasedOn = editBasedOn
		};
	}

}