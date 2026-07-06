using System.Collections.Generic;
using Comms.Dtos.Event;
using Comms.Dtos.Game;
using Comms.Dtos.Match;

namespace Comms.Dtos;



public readonly record struct DataToSend {

	public required List<GameDto> GameSpecifications { get; init; }

	public required List<EventDto> EventSchedules { get; init; }

	public required List<MatchDto> MatchData { get; init; }

}