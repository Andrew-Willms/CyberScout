using System;
using Domain.GameSpecification;

namespace Comms.Dtos;



public enum EventDataSources {
	TheBlueAlliance,
	Manual
}



public record EventDto {

	public required string DeviceId { get; init; }

	public required long MetaDataId { get; init; }

	public required DateTime TimePublished { get; init; }

	public required EventDataSources Source { get; init; }

	public required EventSchedule Event { get; init; }

}