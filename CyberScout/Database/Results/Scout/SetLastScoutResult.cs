using OneOf;
using OneOf.Types;

namespace Database.Results.Scout;



[GenerateOneOf]
public partial class SetLastScoutResult : OneOfBase<
	Success,
	Exception
>;