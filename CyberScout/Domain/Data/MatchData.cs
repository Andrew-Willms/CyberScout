using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.DataCollectors;
using Domain.GameSpecification;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace Domain.Data;



public class MatchData : IEquatable<MatchData> {

	public GameSpec GameSpecification { get; private init; }

	public string ScoutName { get; private init; }

	public string? EventCode { get; private init; }

	public Match Match { get; private init; }

	public uint AllianceIndex { get; private init; }

	public uint TeamNumber { get; private init; }

	public DateTime StartTime { get; private init; }

	public DateTime EndTime { get; private init; }

	public ReadOnlyList<object> DataFields { get; private init; }



	private MatchData(
		GameSpec gameSpecification,
		string? eventCode,
		EventSchedule? eventSchedule,
		string scoutName,
		Match match,
		uint teamNumber,
		uint allianceIndex,
		DateTime startTime,
		DateTime endTime,
		ReadOnlyList<object> dataFieldValues) {

		GameSpecification = gameSpecification;
		EventCode = eventCode;
		ScoutName = scoutName;
		Match = match;
		TeamNumber = teamNumber;
		AllianceIndex = allianceIndex;
		StartTime = startTime;
		EndTime = endTime;
		DataFields = dataFieldValues;
	}

	public static MatchData? FromDataCollector(
		MatchDataCollector collector,
		string eventCode,
		EventSchedule? eventSchedule,
		string scoutName) {

		List<DomainError> errors = [];

		DateTime endTime = DateTime.Now;
		Match match = new() {
			MatchNumber = collector.MatchNumber.Value,
			ReplayNumber = collector.ReplayNumber.Value,
			Type = collector.MatchType.Value
		};

		if (!collector.IsValid) {
			errors.Add(new MatchDataCollectorInvalid { CollectorErrors = collector.Errors.ToReadOnly() });
		}

		ValidateMatch(errors.Add, match, collector.TeamNumber.Value, eventCode, eventSchedule);
		ValidateAllianceIndex(errors.Add, collector.GameSpecification, collector.Alliance.Value);
		ValidateTimes(errors.Add, collector.StartTime, endTime);
		ValidateDataFields(errors.Add, collector.GameSpecification, collector.DataFields, out ReadOnlyList<object> dataFieldResults);

		if (errors.Any()) {
			return null;
		}

		return new(
			collector.GameSpecification,
			eventCode,
			eventSchedule,
			scoutName,
			match,
			collector.TeamNumber.Value,
			collector.Alliance.Value,
			collector.StartTime,
			endTime,
			dataFieldResults
		);
	}

	public static MatchData? FromRaw(
		GameSpec gameSpecification,
		string? eventCode,
		EventSchedule? eventSchedule,
		string scoutName,
		Match match,
		uint teamNumber,
		uint allianceIndex,
		DateTime startTime,
		DateTime endTime,
		ReadOnlyList<object> dataFieldValues) {

		List<DomainError> errors = [];

		ValidateMatch(errors.Add, match, teamNumber, eventCode, eventSchedule);
		ValidateAllianceIndex(errors.Add, gameSpecification, allianceIndex);
		ValidateTimes(errors.Add, startTime, endTime);
		ValidateDataFieldValues(errors.Add, gameSpecification, dataFieldValues, out ReadOnlyList<object> dataFieldResults);

		if (errors.Any()) {
			return null;
		}

		return new(
			gameSpecification,
			eventCode,
			eventSchedule,
			scoutName,
			match,
			teamNumber,
			allianceIndex,
			startTime,
			endTime,
			dataFieldResults
		);
	}



	private static void ValidateMatch(
		Action<DomainError> errorSink,
		Match match,
		uint teamNumber,
		string? eventCode,
		EventSchedule? eventSchedule) {

		if (eventSchedule is null) {
			return;
		}

		if (eventCode is null) {
			errorSink(new EventScheduleButNoEventCode());

		} else if (eventCode != eventSchedule.EventCode) {
			errorSink(new EventCodeAndScheduleMismatch { EventCode = eventCode, ScheduleEventCode = eventSchedule.EventCode });
		}

		// todo this will need to be updated to support other tournament formats
		switch (match.Type) {

			case MatchType.Practice:
				break;

			case MatchType.Qualification:
				uint matchCount = (uint)eventSchedule.Matches.Count;
				if (match.MatchNumber > matchCount) {
					errorSink(new BadMatchNumberError {
						MatchNumber = match.MatchNumber,
						MaxMatchNumber = matchCount,
						MatchType = MatchType.Qualification
					});
				}
				break;

			case MatchType.Elimination:
				if (match.MatchNumber > 13) {
					errorSink(new BadMatchNumberError {
						MatchNumber = match.MatchNumber,
						MaxMatchNumber = 13,
						MatchType = MatchType.Elimination
					});
				}
				break;

			case MatchType.Final:
				if (match.MatchNumber > 3) {
					errorSink(new BadMatchNumberError {
						MatchNumber = match.MatchNumber,
						MaxMatchNumber = 3,
						MatchType = MatchType.Final
					});
				}
				break;

			default:
				throw new UnreachableException();
		}

		if (!eventSchedule.Teams.Contains(teamNumber)) {
			errorSink(new TeamNotInMatch { Team = teamNumber });
		}

		// todo validate start time and end time against event?
	}

	private static void ValidateAllianceIndex(
		Action<DomainError> errorSink,
		GameSpec gameSpecification,
		uint allianceIndex) {

		if (gameSpecification.Alliances.Count <= allianceIndex) {
			errorSink(new BadAllianceIndex {
				AllianceIndex = allianceIndex,
				MaxAllianceIndex = gameSpecification.AlliancesPerMatch - 1
			});
		}
	}

	private static void ValidateTimes(
		Action<DomainError> errorSink,
		DateTime startTime,
		DateTime endTime) {

		if (endTime < startTime) {
			errorSink(new StartAfterEnd { StartTime = startTime, EndTime = endTime });
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
				case MultiIntegerDataFieldSpec when dataFieldValues[index] is int:
				case SelectionDataFieldSpec { RequiresValue: true } when dataFieldValues[index] is Optional<string>:
				case SelectionDataFieldSpec { RequiresValue: false } when dataFieldValues[index] is Optional<string> or Optional:
					results.Add(dataFieldValues[index]);
					continue;

				default:
					errorSink(new DataTypeMismatch { ExpectedDataField = expectedFieldSpec, Value = dataFieldValues[index] });
					break;
			}
		}

		dataFieldResults = results.ToReadOnly();
	}



	public bool Equals(MatchData? other) {

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
			EventCode == other.EventCode &&
			ScoutName == other.ScoutName &&
			Match.Equals(other.Match) &&
			TeamNumber == other.TeamNumber &&
			AllianceIndex == other.AllianceIndex &&
			StartTime.Equals(other.StartTime) &&
			EndTime.Equals(other.EndTime);
	}

	public override bool Equals(object? @object) {

		if (@object is null) {
			return false;
		}

		if (ReferenceEquals(this, @object)) {
			return true;
		}

		if (@object.GetType() != GetType()) {
			return false;
		}

		return Equals((MatchData) @object);
	}

	public override int GetHashCode() {
		HashCode hashCode = new();
		hashCode.Add(GameSpecification);
		hashCode.Add(EventCode);
		hashCode.Add(ScoutName);
		hashCode.Add(Match);
		hashCode.Add(TeamNumber);
		hashCode.Add(AllianceIndex);
		hashCode.Add(StartTime);
		hashCode.Add(EndTime);
		hashCode.Add(DataFields);
		return hashCode.ToHashCode();
	}

}