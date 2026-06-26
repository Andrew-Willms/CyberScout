using Database.Sqlite.Indexer;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results.MatchData;



public record BulkDeleteMatchDataResult : AsyncTryValueResult<Success, BulkDeleteMatchDataError> {

	public BulkDeleteMatchDataResult(Success value) : base(value) { }

	public BulkDeleteMatchDataResult(BulkDeleteMatchDataError error) : base(error) { }


	public static implicit operator BulkDeleteMatchDataResult(Success success) {
		return new(success);
	}

	public static implicit operator BulkDeleteMatchDataResult(BulkDeleteMatchDataError error) {
		return new(error);
	}

	public static implicit operator BulkDeleteMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator BulkDeleteMatchDataResult(RollbackError<BulkDeleteDataError> error) {
		return new(error);
	}

	public static implicit operator BulkDeleteMatchDataResult(RollbackError<BulkSetRecordMetaDataError> error) {
		return new(error);
	}

	public static implicit operator BulkDeleteMatchDataResult(RollbackError<CommitTransactionError> error) {
		return new(error);
	}

}


[GenerateOneOf]
public partial class BulkDeleteMatchDataError : OneOfBase<
	BeginTransactionError,
	RollbackError<BulkDeleteDataError>,
	RollbackError<BulkSetRecordMetaDataError>,
	RollbackError<CommitTransactionError>
>;