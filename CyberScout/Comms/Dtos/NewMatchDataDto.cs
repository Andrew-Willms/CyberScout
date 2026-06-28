using Domain.Data;

namespace Comms.Dtos;



public record NewMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventMetaDataId { get; init; }

}