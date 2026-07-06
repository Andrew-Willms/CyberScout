using System;
using Domain.EventSchedule;

namespace Comms.Dtos;



public enum EventDataSources {
	TheBlueAlliance,
	Manual
}



public record EventDto {

	public required string DeviceId { get; init; }

	public required long EventId { get; init; }

	public required DateTime TimePublished { get; init; }

	public required EventDataSources Source { get; init; }

	public required EventSchedule Event { get; init; }

}