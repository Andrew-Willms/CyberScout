using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Optional;
using UtilitiesLibrary.Results;

namespace Comms.Serialization.Match;



public static class MatchDataToCsv {

	private const int ScoutNameColumnIndex = 0;
	private const int EventCodeColumnIndex = 1;
	private const int MatchGroupNameColumnIndex = 2;
	private const int MatchNameColumnIndex = 3;
	private const int ReplayNumberColumnIndex = 4;
	private const int AllianceColumnIndex = 5;
	private const int TeamNumberColumnIndex = 6;
	private const int TimeStampColumnIndex = 7;
	private const int CountOfBuiltInFields = 8;

	private static readonly string FixedCsvHeader =
		nameof(MatchData.ScoutName) + ',' +
		nameof(MatchData.EventCode) + ',' +
		nameof(MatchData.Match.MatchGroupName) + ',' +
		nameof(MatchData.Match.MatchName) + ',' +
		nameof(MatchData.Match.ReplayNumber) + ',' +
		nameof(MatchData.AllianceIndex) + ',' +
		nameof(MatchData.TeamNumber) + ',' +
		nameof(MatchData.TimeStamp) + ',';

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		// The default buffer size is 16 and that seems reasonable for the length of a DataField name.
		int stringBuilderCapacity = FixedCsvHeader.Length + gameSpecification.DataFields.Count * 16;
		StringBuilder stringBuilder = new(FixedCsvHeader, stringBuilderCapacity);
		stringBuilder.AppendJoin(",", gameSpecification.DataFields.Select(x => x.Name.ToCsvFriendly()));
		return stringBuilder.ToString();
	}

	public static string Serialize(MatchData matchData) {

		// Estimate the device and match IDs will be about 50 characters and each field will take about 5.
		StringBuilder stringBuilder = new(50 + matchData.GameSpecification.DataFields.Count * 5);
		stringBuilder.Append(matchData.ScoutName.ToCsvFriendly());
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.EventCode);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.Match.MatchGroupName);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.Match.MatchName);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.Match.ReplayNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.AllianceIndex);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.TeamNumber);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.TimeStamp.ToString("o").ToCsvFriendly());

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

	public static Result<MatchData> Deserialize(string text, GameSpec gameSpecification) {

		List<string> columns = text.SplitTextToCsvColumns();
		return Deserialize(columns, gameSpecification);
	}

	public static Result<MatchData> Deserialize(List<string> columns, GameSpec gameSpecification) {

		uint expectedColumnCount = CountOfBuiltInFields + (uint)gameSpecification.DataFields.Count;

		if (columns.Count != expectedColumnCount) {
			return new AdHocError("Wrong number of columns",
				("columns", string.Join(',', columns)),
				("game specification", gameSpecification.ToString()),
				("expected column count", expectedColumnCount.ToString()),
				("actual column count", columns.Count.ToString()));
		}

		string scoutName = columns[ScoutNameColumnIndex];
		string eventCode = columns[EventCodeColumnIndex];
		string matchGroupName = columns[MatchGroupNameColumnIndex];
		string matchName = columns[MatchNameColumnIndex];

		if (!uint.TryParse(columns[ReplayNumberColumnIndex], out uint replayNumber)) {
			return new AdHocError("Error parsing ReplayNumber column.",
				("columns", string.Join(',', columns)),
				("game specification", gameSpecification.ToString()),
				("column", columns[ReplayNumberColumnIndex]));
		}

		if (!uint.TryParse(columns[AllianceColumnIndex], out uint allianceIndex)) {
			return new AdHocError("Error parsing AllianceIndex column.",
				("columns", string.Join(',', columns)),
				("game specification", gameSpecification.ToString()),
				("column", columns[AllianceColumnIndex]));
		}

		if (!uint.TryParse(columns[TeamNumberColumnIndex], out uint teamNumber)) {
			return new AdHocError("Error parsing TeamNumber column.",
				("columns", string.Join(',', columns)),
				("game specification", gameSpecification.ToString()),
				("column", columns[TeamNumberColumnIndex]));
		}

		if (!DateTime.TryParse(columns[TimeStampColumnIndex], out DateTime startTime)) {
			return new AdHocError("Error parsing TimeStamp column.",
				("columns", string.Join(',', columns)),
				("game specification", gameSpecification.ToString()),
				("column", columns[TimeStampColumnIndex]));
		}

		List<object> dataFieldValues = new(gameSpecification.DataFields.Count);
		for (int i = 0; i < gameSpecification.DataFields.Count; i++) {

			int csvColumnIndex = i + CountOfBuiltInFields;
			string value = columns[csvColumnIndex];

			Result<object> result = ParseDataFieldValue(value, gameSpecification.DataFields[i]);

			if (result.IsFailure) {
				return new AdHocError("");
			}

			dataFieldValues.Add(result.Value);
		}

		MatchData? matchData = MatchData.FromRaw(
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
			dataFieldValues.ToReadOnly()
		);

		if (matchData is null) {
			return new AdHocError("Unknown MatchData deserialization error.");
		}

		return matchData;
	}

	// Not sure how the implicit casts will work with Result<object> TODO: test
	private static Result<object> ParseDataFieldValue(string text, DataFieldSpec dataField) {

		switch (dataField) {

			case BooleanDataFieldSpec dataFieldSpec: {
				return text switch {
					"1" => true,
					"0" => false,
					_ => new AdHocError("Error parsing DataField", ("DataField", dataFieldSpec.ToString()), ("text", text))
				};
			}

			case TextDataFieldSpec:
				return text;

			case IntegerDataFieldSpec dataFieldSpec: {

				if (!int.TryParse(text, out int result)) {
					return new AdHocError("Error parsing DataField", ("DataField", dataFieldSpec.ToString()),
						("text", text));
				}

				if (result > dataFieldSpec.MinValue || result < dataFieldSpec.MaxValue) {
					return new AdHocError("IntegerDataField value outside of range",
						("DataField", dataFieldSpec.ToString()), 
						("text", text));
				}

				return result;
			}

			case SelectionDataFieldSpec dataFieldSpec: {

				if (text == string.Empty) {

					if (!dataFieldSpec.RequiresValue) {
						return Optional.NoValue;
					}

					return new AdHocError("SelectionDataField has no value when one is required",
						("DataField", dataFieldSpec.ToString()),
						("text", text));
				}

				if (!int.TryParse(text, out int result)) {
					return new AdHocError("Error parsing DataField", 
						("DataField", dataFieldSpec.ToString()),
						("text", text));
				}

				if (result >= dataFieldSpec.Options.Count) {
					return new AdHocError("Option index is too high.",
						("DataField", dataFieldSpec.ToString()),
						("text", text));
				}

				return dataFieldSpec.Options[result].Optionalize();
			}

			default:
				throw new NotImplementedException(dataField.GetType().Name);
		}
	}

}



public record MatchDataDeserializationResult : Result<MatchData, MatchDataDeserializationError> {

	public MatchDataDeserializationResult(MatchData value) : base(value) { }

	public MatchDataDeserializationResult(MatchDataDeserializationError error) : base(error) { }

	public static implicit operator MatchDataDeserializationResult(MatchData value) {
		return new(value);
	}

	public static implicit operator MatchDataDeserializationResult(MatchDataDeserializationError error) {
		return new(error);
	}

}

// TODO: Consider making this a OneOf<> to enforce exhaustive matching on switch
// The SerializedMatchData and GameSpecification properties could be moved to an interface?
public abstract record MatchDataDeserializationError : Error {

	public required string SerializedMatchData { get; init; }

	public required GameSpec GameSpecification { get; init; }

}

public record WrongNumberOfCsvColumnsError : MatchDataDeserializationError {

	public required uint ExpectedColumnCount { get; init; }

	public required ReadOnlyList<string> Columns { get; init; }

}

public record CouldNotParseValuesError : MatchDataDeserializationError {

	public required ReadOnlyList<CoreValueError> CoreValueErrors { get; init; }

	public required ReadOnlyList<DataFieldError> DataFieldErrors { get; init; }
}

public record CoreValueError {

	public required int ColumnIndex { get; init; }

	public required Type ExpectedType { get; init; }

	public required string Text { get; init; }
}

public record DataFieldError {

	public required DataFieldSpec DataField { get; init; }

	public required string Text { get; init; }

}