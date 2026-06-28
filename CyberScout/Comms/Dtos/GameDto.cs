using System;
using Domain.GameSpecification;

namespace Comms.Dtos;



public record GameDto {

	public required string DeviceId { get; init; }

	public required long GameId { get; init; }

	public required DateTime TimePublished { get; init; }

	public required GameSpec Specification { get; init; }

}