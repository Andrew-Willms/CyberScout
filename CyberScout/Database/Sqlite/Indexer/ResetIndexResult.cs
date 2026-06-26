using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record ResetIndexResult : AsyncTryValueResult<Success, ResetIndexError> {

	public ResetIndexResult(Success value) : base(value) { }

	public ResetIndexResult(ResetIndexError error) : base(error) { }

	public static implicit operator ResetIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator ResetIndexResult(ResetIndexError error) {
		return new(error);
	}

	public static implicit operator ResetIndexResult(ExecuteNonQueryAndExpectError error) {
		return new ResetIndexError(error);
	}

}

public record ResetIndexError(ExecuteNonQueryAndExpectError Error);