using Database.Results;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record GetRangesResult : AsyncTryResult<List<IndexRange>, GetRangesError> {

	public GetRangesResult(List<IndexRange> value) : base(value) { }

	public GetRangesResult(GetRangesError error) : base(error) { }

	public static implicit operator GetRangesResult(List<IndexRange> value) {
		return new(value);
	}

	public static implicit operator GetRangesResult(GetRangesError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(GetEventDataIdError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(ReadDataError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(ColumnReadError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(StatusShouldBeStoredError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(NullableColumnReadError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(ColumnNullWhenShouldNotBeError error) {
		return new(error);
	}

	public static implicit operator GetRangesResult(RangeCreationError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class GetRangesError : OneOfBase<
	GetEventDataIdError,
	ReadDataError,
	ColumnReadError,
	NullableColumnReadError,
	StatusShouldBeStoredError,
	ColumnNullWhenShouldNotBeError,
	RangeCreationError
>;

public record GetEventDataIdError {

	public required IntegerScalarError Error { get; init; }

}

public record StatusShouldBeStoredError;

public record ColumnNullWhenShouldNotBeError {

	public required string ColumnName { get; init; }

}

public record RangeCreationError;