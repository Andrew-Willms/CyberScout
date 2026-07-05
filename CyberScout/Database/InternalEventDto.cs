using Comms.Dtos;
using Domain.GameSpecification;

namespace Database;



public record InternalEventDto {

	public required string DeviceId { get; init; }

	public required long MetaDataId { get; init; }

	public required long DataId { get; init; }

	public required DateTime TimePublished { get; init; }

	public required EventDataSources Source { get; init; }

	public required EventSchedule Event { get; init; }

}