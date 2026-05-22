using OneOf;

namespace Database.Results.GameSpec;



[GenerateOneOf]
public partial class GetGameSpecsResult : OneOfBase<
	List<Domain.GameSpecification.GameSpec>,
	Exception
>;