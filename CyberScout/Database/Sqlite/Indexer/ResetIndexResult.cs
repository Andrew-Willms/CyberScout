using Database.Results;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record ResetIndexResult : AsyncTryValueResult<Success, RollbackError<ExecuteNonQueryAndExpectError>> {

	public ResetIndexResult(Success value) : base(value) { }

	public ResetIndexResult(RollbackError<ExecuteNonQueryAndExpectError> error) : base(error) { }

	public static implicit operator ResetIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator ResetIndexResult(RollbackError<ExecuteNonQueryAndExpectError> error) {
		return new(error);
	}

}
