using Domain.EventSchedule;

namespace Comms.Dtos.Event;



public record EventDto {

	public required string DeviceId { get; init; }

	public required long EventId { get; init; }

	public required EventSchedule EventSchedule { get; init; }

}