using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record GetRangesResult : AsyncTryResult<List<IndexRange>, GetRangesError> {

	public GetRangesResult(IndexRange value) : base(value) { }

	public GetRangesResult(GetRangesError error) : base(error) { }

	public static implicit operator GetRangesResult(GetRangesError error) {
		return new(error);
	}

}

//[GenerateOneOf]
public /*partial*/ class GetRangesError /*: OneOfBase<>*/; // todo