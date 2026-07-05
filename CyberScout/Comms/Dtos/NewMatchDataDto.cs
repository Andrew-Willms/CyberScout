using Domain.Data;

namespace Comms.Dtos;



public record NewMatchDataDto {

	public required MatchData Data { get; init; }

	public required string DeviceId { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public string EventCode => Data.EventCode;

}