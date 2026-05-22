using Domain.Data;

namespace Domain.Dtos;



public record NewMatchDataDto {

	public required MatchData MatchData { get; init; }

	public required string DeviceId { get; init; }

	public required string GameDeviceId { get; init; }

	public required uint GameId { get; init; }



	private NewMatchDataDto() { }

	public static NewMatchDataDto Create(
		MatchData matchData,
		string deviceId,
		string gameDeviceId,
		uint gameId) {

		return new() {
			MatchData = matchData,
			DeviceId = deviceId,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

}