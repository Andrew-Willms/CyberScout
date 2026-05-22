using OneOf;
using OneOf.Types;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class DeleteAllMatchDataResult : OneOfBase<
	Success,
	Exception
>;