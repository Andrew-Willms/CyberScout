using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record BulkSetRecordMetaDataResult : AsyncTryValueResult<Success, BulkSetRecordMetaDataError> {

	public BulkSetRecordMetaDataResult(Success value) : base(value) { }

	public BulkSetRecordMetaDataResult(BulkSetRecordMetaDataError error) : base(error) { }

	public static implicit operator BulkSetRecordMetaDataResult(Success value) {
		return new(value);
	}

	public static implicit operator BulkSetRecordMetaDataResult(BulkSetRecordMetaDataError error) {
		return new(error);
	}

	public static implicit operator BulkSetRecordMetaDataResult(GetSuperRangesError error) {
		return new BulkSetRecordMetaDataError(error);
	}

}

[GenerateOneOf]
public partial class BulkSetRecordMetaDataError : OneOfBase<
	GetSuperRangesError
>;