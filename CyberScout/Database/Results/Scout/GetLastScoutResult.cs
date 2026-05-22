using OneOf;

namespace Database.Results.Scout;



[GenerateOneOf]
public partial class GetLastScoutResult : OneOfBase<
	string,
	Exception
>;