using Database.Sqlite;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results.MatchData;



public record AddNewEditedMatchDataResult : AsyncTryValueResult<Success, AddNewEditedMatchDataError> {

	public AddNewEditedMatchDataResult(Success value) : base(value) { }

	public AddNewEditedMatchDataResult(AddNewEditedMatchDataError error) : base(error) { }


	public static implicit operator AddNewEditedMatchDataResult(Success success) {
		return new(success);
	}

	public static implicit operator AddNewEditedMatchDataResult(AddNewEditedMatchDataError error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<GetIdError> error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<TableOverflowError> error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<InsertDataResult> error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<UpdateSequenceError> error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<SetRecordMetaDataError> error) {
		return new(error);
	}

	public static implicit operator AddNewEditedMatchDataResult(RollbackError<CommitTransactionError> error) {
		return new(error);
	}

}



[GenerateOneOf]
public partial class AddNewEditedMatchDataError : OneOfBase<
	BeginTransactionError,
	RollbackError<TableOverflowError>,
	RollbackError<GetIdError>,
	RollbackError<InsertDataResult>,
	RollbackError<UpdateSequenceError>,
	RollbackError<SetRecordMetaDataError>,
	RollbackError<CommitTransactionError>
>;