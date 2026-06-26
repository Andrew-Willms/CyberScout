using Database.Sqlite;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;
using SetRecordMetaDataError = Database.Sqlite.Indexer.SetRecordMetaDataError;

namespace Database.Results.MatchData;



public record ImportMatchDataResult : AsyncTryValueResult<Success, ImportMatchDataError> {

	public ImportMatchDataResult(Success value) : base(value) { }

	public ImportMatchDataResult(ImportMatchDataError error) : base(error) { }


	public static implicit operator ImportMatchDataResult(Success success) {
		return new(success);
	}

	public static implicit operator ImportMatchDataResult(ImportMatchDataError error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<GetIdError> error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<TableOverflowError> error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<InsertDataResult> error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<UpdateSequenceError> error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<SetRecordMetaDataError> error) {
		return new(error);
	}

	public static implicit operator ImportMatchDataResult(RollbackError<CommitTransactionError> error) {
		return new(error);
	}

}



[GenerateOneOf]
public partial class ImportMatchDataError : OneOfBase<
	BeginTransactionError,
	RollbackError<TableOverflowError>,
	RollbackError<GetIdError>,
	RollbackError<InsertDataResult>,
	RollbackError<UpdateSequenceError>,
	RollbackError<SetRecordMetaDataError>,
	RollbackError<CommitTransactionError>
>;