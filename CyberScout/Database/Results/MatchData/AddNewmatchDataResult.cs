using Database.Sqlite;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;
using SetRecordMetaDataError = Database.Sqlite.Indexer.SetRecordMetaDataError;

namespace Database.Results.MatchData;



public record AddNewMatchDataResult : AsyncTryValueResult<Success, AddNewMatchDataError> {

	public AddNewMatchDataResult(Success value) : base(value) { }

	public AddNewMatchDataResult(AddNewMatchDataError error) : base(error) { }


	public static implicit operator AddNewMatchDataResult(Success success) {
		return new(success);
	}

	public static implicit operator AddNewMatchDataResult(AddNewMatchDataError error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<GetIdError> error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<TableOverflowError> error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<InsertDataResult> error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<UpdateSequenceError> error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<SetRecordMetaDataError> error) {
		return new(error);
	}

	public static implicit operator AddNewMatchDataResult(RollbackError<CommitTransactionError> error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class AddNewMatchDataError : OneOfBase<
	BeginTransactionError,
	RollbackError<TableOverflowError>,
	RollbackError<GetIdError>,
	RollbackError<InsertDataResult>,
	RollbackError<UpdateSequenceError>,
	RollbackError<SetRecordMetaDataError>,
	RollbackError<CommitTransactionError>
>;