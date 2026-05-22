using Domain.GameSpecification;
using OneOf;

namespace Database.Results.Event;



[GenerateOneOf]
public partial class GetEventsResult : OneOfBase<
	List<EventSchedule>,
	Exception
>;