using OneOf;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record GetSuperRangesResult : AsyncTryResult<SuperRange, GetSuperRangesError> {

	public GetSuperRangesResult(SuperRange value) : base(value) { }

	public GetSuperRangesResult(GetSuperRangesError error) : base(error) { }

	public static implicit operator GetSuperRangesResult(SuperRange value) {
		return new(value);
	}

	public static implicit operator GetSuperRangesResult(GetSuperRangesError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class GetSuperRangesError : OneOfBase<
	int
>;