using OneOf;
using Success = OneOf.Types.Success;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class AddNewMatchDataResult : OneOfBase<
	Success,
	DataStoreError
>;