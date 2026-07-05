using System.Collections.Generic;

namespace Comms.Dtos;



public readonly record struct DataToSend {

	public required List<GameDto> GameSpecifications { get; init; }

	public required List<EventDto> EventSchedules { get; init; }

	public required List<MatchDataDto> MatchData { get; init; }

}