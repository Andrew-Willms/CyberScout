using Database.Results;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record DeleteRangeFromIndexResult : AsyncTryValueResult<Success, DeleteRangeFromIndexError> {

	public DeleteRangeFromIndexResult(Success value) : base(value) { }

	public DeleteRangeFromIndexResult(DeleteRangeFromIndexError error) : base(error) { }

	public static implicit operator DeleteRangeFromIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator DeleteRangeFromIndexResult(DeleteRangeFromIndexError error) {
		return new(error);
	}

	public static implicit operator DeleteRangeFromIndexResult(RollbackError<DeleteDataError> error) {
		return new DeleteRangeFromIndexError(error);
	}

}

public record DeleteRangeFromIndexError(RollbackError<DeleteDataError> Error);