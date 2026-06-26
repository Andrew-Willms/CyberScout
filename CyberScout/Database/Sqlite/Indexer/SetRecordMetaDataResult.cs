using Database.Results;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record SetRecordMetaDataResult : AsyncTryValueResult<Success, SetRecordMetaDataError> {

	public SetRecordMetaDataResult(Success value) : base(value) { }

	public SetRecordMetaDataResult(SetRecordMetaDataError error) : base(error) { }

	public static implicit operator SetRecordMetaDataResult(Success success) {
		return new(success);
	}

	public static implicit operator SetRecordMetaDataResult(SetRecordMetaDataError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(GetSuperRangeError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(RangeOperationError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(DeleteRangeFromIndexError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(AddRangeToIndexError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class SetRecordMetaDataError : OneOfBase<
	GetSuperRangeError,
	RangeOperationError,
	DeleteRangeFromIndexError,
	AddRangeToIndexError
>;