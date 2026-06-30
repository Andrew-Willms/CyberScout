using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record GetRangeResult : AsyncTryResult<IndexRange, GetRangeError> {

	public GetRangeResult(IndexRange value) : base(value) { }

	public GetRangeResult(GetRangeError error) : base(error) { }

	public static implicit operator GetRangeResult(IndexRange value) {
		return new(value);
	}

	public static implicit operator GetRangeResult(GetRangeError error) {
		return new(error);
	}

}

//[GenerateOneOf]
public /*partial*/ class GetRangeError /*: OneOfBase<>*/; // todo