using Domain.Data;

namespace Domain.Dtos;



public record ImportMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required uint MatchId { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	// Create method for if I need additional data validation
	//private ImportMatchDataDto() { }
	//
	//public static ImportMatchDataDto Create(
	//	MatchData matchData,
	//	string deviceId,
	//	uint matchId,
	//	string gameDeviceId,
	//	uint gameId) {
	//
	//	return new() {
	//		MatchData = matchData,
	//		DeviceId = deviceId,
	//		MatchId = matchId,
	//		GameDeviceId = gameDeviceId,
	//		GameId = gameId
	//	};
	//}

}