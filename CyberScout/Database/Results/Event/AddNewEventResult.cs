using OneOf;
using OneOf.Types;

namespace Database.Results.Event;



[GenerateOneOf]
public partial class AddNewEventResult : OneOfBase<
	Success,
	Exception
>;