using OneOf;
using OneOf.Types;

namespace Database.Results.Event;



[GenerateOneOf]
public partial class DeleteEventResult : OneOfBase<
	Success,
	Exception
>;