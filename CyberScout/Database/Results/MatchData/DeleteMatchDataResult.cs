using Database.Sqlite;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results.MatchData;



public record DeleteMatchDataResult : AsyncTryValueResult<Success, DeleteMatchDataError> {

	public DeleteMatchDataResult(Success value) : base(value) { }

	public DeleteMatchDataResult(DeleteMatchDataError error) : base(error) { }


	public static implicit operator DeleteMatchDataResult(Success success) {
		return new(success);
	}

	public static implicit operator DeleteMatchDataResult(DeleteMatchDataError error) {
		return new(error);
	}

	public static implicit operator DeleteMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator DeleteMatchDataResult(RollbackError<DeleteDataError> error) {
		return new(error);
	}

	public static implicit operator DeleteMatchDataResult(RollbackError<SetRecordMetaDataError> error) {
		return new(error);
	}

	public static implicit operator DeleteMatchDataResult(RollbackError<CommitTransactionError> error) {
		return new(error);
	}

}


[GenerateOneOf]
public partial class DeleteMatchDataError : OneOfBase<
	BeginTransactionError,
	RollbackError<DeleteDataError>,
	RollbackError<SetRecordMetaDataError>,
	RollbackError<CommitTransactionError>
>;