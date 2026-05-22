using OneOf;
using OneOf.Types;

namespace Database.Results.GameSpec;



[GenerateOneOf]
public partial class AddNewGameSpecResult : OneOfBase<
	Success,
	Exception
>;