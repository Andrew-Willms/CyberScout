using System;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.Collections;

namespace Domain.Errors;



public class DomainError;



public class MatchDataCollectorInvalid : DomainError {

	public required ReadOnlyList<string> CollectorErrors { get; init; }

}

public class EventScheduleButNoEventCode : DomainError;

public class EventCodeAndScheduleMismatch : DomainError {

	public required string EventCode { get; init; }

	public required string ScheduleEventCode { get; init; }

}

public class BadMatchNumberError : DomainError {

	public required uint MatchNumber { get; init; }

	public required uint MaxMatchNumber { get; init; }

	public required MatchType MatchType { get; init; }

}

public class TeamNotInMatch : DomainError {

	public required uint Team { get; init; }

}

public class BadAllianceIndex : DomainError {

	public required uint AllianceIndex { get; init; }

	public required uint MaxAllianceIndex { get; init; }

}

public class StartAfterEnd : DomainError {

	public required DateTime StartTime { get; init; }

	public required DateTime EndTime { get; init; }
}

public class DataFieldMismatch : DomainError {

	public DataFieldSpec ExpectedDataField { get; }

	public DataFieldSpec ReceivedDataField { get; }

	public object Value { get; }

	private DataFieldMismatch(DataFieldSpec expectedDataField, DataFieldSpec receivedDataField, object value) {
		ExpectedDataField = expectedDataField;
		ReceivedDataField = receivedDataField;
		Value = value;
	}

	public static DataFieldMismatch? Create(DataFieldSpec expectedDataField, DataFieldSpec receivedDataField, object value) {

		if (expectedDataField == receivedDataField) {
			return null;
		}

		return new(expectedDataField, receivedDataField, value);
	}

}

public class DataTypeMismatch : DomainError {

	public required DataFieldSpec ExpectedDataField { get; init; }

	public required object Value { get; init; }

}