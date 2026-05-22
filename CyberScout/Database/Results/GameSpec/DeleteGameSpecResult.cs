using OneOf;
using OneOf.Types;

namespace Database.Results.GameSpec;



[GenerateOneOf]
public partial class DeleteGameSpecResult : OneOfBase<
	Success,
	Exception
>;