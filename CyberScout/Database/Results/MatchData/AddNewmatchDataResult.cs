using OneOf;
using OneOf.Types;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class AddNewMatchDataResult : OneOfBase<
	Success,
	Exception
>;