using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.DataCollectors;
using Domain.Errors;
using Domain.GameSpecification;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace Domain.MatchData;



public record MatchData : IEquatable<MatchData> {

	public GameSpec GameSpecification { get; private init; }

	public string ScoutName { get; private init; }

	// Empty indicates no EventCode
	public string EventCode { get; private init; }

	public Match Match { get; private init; }

	public uint AllianceIndex { get; private init; }

	public uint TeamNumber { get; private init; }

	public DateTime TimeStamp { get; private init; }

	public ReadOnlyList<object> DataFields { get; private init; }



	private MatchData(
		GameSpec gameSpecification,
		string scoutName,
		string eventCode,
		Match match,
		uint allianceIndex,
		uint teamNumber,
		DateTime timeStamp,
		ReadOnlyList<object> dataFieldValues) {

		GameSpecification = gameSpecification;
		EventCode = eventCode;
		ScoutName = scoutName;
		Match = match;
		TeamNumber = teamNumber;
		AllianceIndex = allianceIndex;
		TimeStamp = timeStamp;
		DataFields = dataFieldValues;
	}

	// TODO move this to where the collectors live
	public static MatchData? FromDataCollector(
		MatchDataCollector collector,
		string eventCode,
		string scoutName) {

		List<DomainError> errors = [];

		Match match = new() {
			MatchGroupName = null, // TODO fix
			MatchName = null,
			ReplayNumber = 0
		};

		if (!collector.IsValid) {
			errors.Add(new MatchDataCollectorInvalid { CollectorErrors = collector.Errors.ToReadOnly() });
		}

		ValidateAllianceIndex(errors.Add, collector.GameSpecification, collector.Alliance.Value);
		ValidateDataFields(errors.Add, collector.GameSpecification, collector.DataFields, out ReadOnlyList<object> dataFieldResults);

		if (errors.Any()) {
			return null;
		}

		return new(
			collector.GameSpecification,
			eventCode,
			scoutName,
			match,
			collector.TeamNumber.Value,
			collector.Alliance.Value,
			collector.StartTime,
			dataFieldResults
		);
	}

	public static MatchData? FromRaw(
		GameSpec gameSpecification,
		string eventCode,
		string scoutName,
		Match match,
		uint teamNumber,
		uint allianceIndex,
		DateTime startTime,
		ReadOnlyList<object> dataFieldValues) {

		List<DomainError> errors = [];
		
		ValidateAllianceIndex(errors.Add, gameSpecification, allianceIndex);
		ValidateDataFieldValues(errors.Add, gameSpecification, dataFieldValues, out ReadOnlyList<object> dataFieldResults);

		if (errors.Any()) {
			return null;
		}

		return new(
			gameSpecification,
			eventCode,
			scoutName,
			match,
			teamNumber,
			allianceIndex,
			startTime,
			dataFieldResults
		);
	}



	private static void ValidateAllianceIndex(
		Action<DomainError> errorSink,
		GameSpec gameSpecification,
		uint allianceIndex) {

		if (gameSpecification.MatchFormat.Alliances.Count <= allianceIndex) {
			errorSink(new BadAllianceIndex {
				AllianceIndex = allianceIndex,
				MaxAllianceIndex = gameSpecification.MatchFormat.Alliances.Count - 1
			});
		}
	}

	private static void ValidateDataFields(
		Action<DomainError> errorSink,
		GameSpec gameSpec,
		ReadOnlyList<DataField> dataFields,
		out ReadOnlyList<object> dataFieldResults) {

		List<object> results = [];

		for (int index = 0; index < gameSpec.DataFields.Count; index++) {

			DataFieldSpec expectedFieldSpec = gameSpec.DataFields[index];

			DataField receivedField = dataFields[index];
			DataFieldSpec receivedFieldSpec = receivedField.Specification;

			if (expectedFieldSpec != receivedFieldSpec) {
				errorSink(DataFieldMismatch.Create(expectedFieldSpec, receivedFieldSpec, receivedField.BaseValue) ?? throw new UnreachableException());
				continue;
			}

			if (receivedField.Errors.Any()) {
				errorSink(DataFieldMismatch.Create(expectedFieldSpec, receivedFieldSpec, receivedField.BaseValue) ?? throw new UnreachableException());
				continue;
			}

			results.Add(receivedField.BaseValue);
		}

		dataFieldResults = results.ToReadOnly();
	}

	private static void ValidateDataFieldValues(
		Action<DomainError> errorSink,
		GameSpec gameSpec,
		ReadOnlyList<object> dataFieldValues,
		out ReadOnlyList<object> dataFieldResults) {

		List<object> results = [];

		for (int index = 0; index < gameSpec.DataFields.Count; index++) {

			DataFieldSpec expectedFieldSpec = gameSpec.DataFields[index];

			switch (expectedFieldSpec) {
				case BooleanDataFieldSpec when dataFieldValues[index] is bool:
				case TextDataFieldSpec when dataFieldValues[index] is string:
				case IntegerDataFieldSpec when dataFieldValues[index] is int:
				case SelectionDataFieldSpec { RequiresValue: true } when dataFieldValues[index] is Optional<string>:
					results.Add(dataFieldValues[index]);
					continue;

				default:
					errorSink(new DataTypeMismatch { ExpectedDataField = expectedFieldSpec, Value = dataFieldValues[index] });
					break;
			}
		}

		dataFieldResults = results.ToReadOnly();
	}



	// TODO create a generator to generate equality comparisons that use a sequence comparison on enumerables.
	public virtual bool Equals(MatchData? other) {

		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		if (DataFields.Count != other.DataFields.Count) {
			return false;
		}

		for (int i = 0; i < DataFields.Count; i++) {

			object value = DataFields[i];
			object otherValue = other.DataFields[i];

			switch (value) {

				case Optional<string> optional:
					if (otherValue is not Optional<string> otherOptional || optional != otherOptional) {
						return false;
					}
					break;

				case bool boolean:
					if (otherValue is not bool otherBool || boolean != otherBool) {
						return false;
					}
					break;

				case string text:
					if (otherValue is not string otherText || otherText != text) {
						return false;
					}
					break;

				case int integer:
					if (otherValue is not int otherInt || integer != otherInt) {
						return false;
					}
					break;

				default:
					throw new UnreachableException();
			}
		}

		return
			GameSpecification.Equals(other.GameSpecification) &&
			ScoutName == other.ScoutName &&
			EventCode == other.EventCode &&
			Match.Equals(other.Match) &&
			AllianceIndex == other.AllianceIndex &&
			TeamNumber == other.TeamNumber &&
			TimeStamp.Equals(other.TimeStamp);
	}

	public override int GetHashCode() {
		HashCode hashCode = new();
		hashCode.Add(GameSpecification);
		hashCode.Add(EventCode);
		hashCode.Add(ScoutName);
		hashCode.Add(Match);
		hashCode.Add(TeamNumber);
		hashCode.Add(AllianceIndex);
		hashCode.Add(TimeStamp);
		hashCode.Add(DataFields);
		return hashCode.ToHashCode();
	}

}