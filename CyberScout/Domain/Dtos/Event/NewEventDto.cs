namespace Domain.Dtos.Event;



public record NewEventDto {

	public required string DeviceId { get; init; }

	public required EventSchedule.EventSchedule EventSchedule { get; init; }

}