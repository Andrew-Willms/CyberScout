using OneOf;
using OneOf.Types;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class DeleteMatchDataResult : OneOfBase<
	Success,
	DataStoreError
>;