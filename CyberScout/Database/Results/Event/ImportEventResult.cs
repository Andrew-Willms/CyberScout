using OneOf;
using OneOf.Types;

namespace Database.Results.Event;



[GenerateOneOf]
public partial class ImportEventResult : OneOfBase<
	Success,
	Exception
>;