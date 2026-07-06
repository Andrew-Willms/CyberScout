using Domain.GameSpecification;

namespace Comms.Dtos.Game;



public record GameDto {

	public required string DeviceId { get; init; }

	public required long GameId { get; init; }

	public required GameSpec Specification { get; init; }

}