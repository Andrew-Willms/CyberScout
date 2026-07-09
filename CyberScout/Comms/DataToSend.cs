using System.Collections.Generic;
using Domain.Dtos.Event;
using Domain.Dtos.Game;
using Domain.Dtos.Match;

namespace Comms;



public readonly record struct DataToSend {

	public required List<GameDto> GameSpecifications { get; init; }

	public required List<EventDto> EventSchedules { get; init; }

	public required List<MatchDto> MatchData { get; init; }

}