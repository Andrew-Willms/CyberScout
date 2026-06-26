using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record BulkSetRecordMetaDataResult : AsyncTryValueResult<Success, BulkSetRecordMetaDataError> {

	public BulkSetRecordMetaDataResult(Success value) : base(value) { }

	public BulkSetRecordMetaDataResult(BulkSetRecordMetaDataError error) : base(error) { }

}

public class BulkSetRecordMetaDataError;