using Domain.Data;

namespace Comms.Dtos;



public record NewMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	public required string EventDeviceId { get; init; }

	public required long EventId { get; init; }



	// Create method for if I need additional data validation
	//private NewMatchDataDto() { }
	//
	//public static NewMatchDataDto Create(
	//	MatchData matchData,
	//	string deviceId,
	//	string gameDeviceId,
	//	uint gameId) {
	//
	//	return new() {
	//		MatchData = matchData,
	//		DeviceId = deviceId,
	//		GameDeviceId = gameDeviceId,
	//		GameId = gameId
	//	};
	//}

}