using Domain.EventSchedule;

namespace Comms.Dtos.Event;



public record NewEventDto {

	public required string DeviceId { get; init; }

	public required EventSchedule EventSchedule { get; init; }

}