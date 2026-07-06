using UtilitiesLibrary.Collections;

namespace Domain.EventSchedule;



public record ScheduledAlliance {

	public required string Name { get; init; }

	public required ReadOnlyList<(uint number, bool isSurrogate)> Teams { get; init; }

}