using Domain.GameSpecification;
using UtilitiesLibrary.Collections;

namespace Domain.Errors;



public class DomainError;



public class MatchDataCollectorInvalid : DomainError {

	public required ReadOnlyList<string> CollectorErrors { get; init; }

}

public class BadAllianceIndex : DomainError {

	public required uint AllianceIndex { get; init; }

	public required int MaxAllianceIndex { get; init; }

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