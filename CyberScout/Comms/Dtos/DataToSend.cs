using System.Collections.Generic;
using Domain.Data;
using Domain.GameSpecification;

namespace Comms.Dtos;



public readonly record struct DataToSend {

	public required List<GameSpec> GameSpecifications { get; init; }

	public required List<EventSchedule> EventSchedules { get; init; }

	public required List<MatchData> MatchData { get; init; }

}