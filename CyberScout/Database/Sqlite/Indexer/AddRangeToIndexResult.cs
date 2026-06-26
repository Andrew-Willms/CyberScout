using Database.Results;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record AddRangeToIndexResult : AsyncTryValueResult<Success, AddRangeToIndexError> {

	public AddRangeToIndexResult(Success value) : base(value) { }

	public AddRangeToIndexResult(AddRangeToIndexError error) : base(error) { }

	public static implicit operator AddRangeToIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator AddRangeToIndexResult(AddRangeToIndexError error) {
		return new(error);
	}

	public static implicit operator AddRangeToIndexResult(InsertDataResult error) {
		return new AddRangeToIndexError(error);
	}

}

public record AddRangeToIndexError(InsertDataResult Error);