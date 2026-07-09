using Domain.GameSpecification;

namespace Domain.Dtos.Game;



public record NewGameDto {

	public required string DeviceId { get; init; }

	public required GameSpec Specification { get; init; }

}