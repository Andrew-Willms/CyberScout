using Comms.Dtos.Match;
using Comms.Serialization.Match;
using MatchDataDeserializationResult = Comms.Serialization.Match.MatchDataDeserializationResult;

namespace Domain.Tests.Serialization;



public class MatchToCsvTests {

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestSerialization(MatchData.MatchData matchData) {

		string serialized = MatchToCsv.Serialize(matchData);
		MatchDataDeserializationResult deserializationResult = MatchToCsv.Deserialize(serialized, SampleData.GameSpec);

		if (deserializationResult.IsFailure) {
			Assert.Fail();
		}


		Assert.True(matchData.Equals(deserializationResult.Value));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization(MatchData.MatchData matchData) {

		CreateMatchDataDtoResult result = MatchDto.Create(
			matchData: matchData,
			deviceId: "deviceId",
			matchId: 1,
			originalDeviceId: "originalDeviceId",
			originalMatchId: 1,
			parents: [],
			gameDeviceId: "gameDeviceId",
			gameId: 1
		);

		if (result.IsFailure) {
			Assert.Fail();
		}

		string serialized = MatchDtoToCsv.Serialize(result.Value);
		MatchDto? deserialized = MatchDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(result.Equals(deserialized));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization_2(MatchData.MatchData matchData) {


		CreateMatchDataDtoResult result = MatchDto.Create(
			matchData: matchData,
			deviceId: "deviceId",
			matchId: 1,
			originalDeviceId: "originalDeviceId",
			originalMatchId: 1,
			parents: [],
			gameDeviceId: "gameDeviceId",
			gameId: 1
		);

		if (result.IsFailure) {
			Assert.Fail();
		}

		string serialized = MatchDtoToCsv.Serialize(result.Value);
		MatchDto? deserialized = MatchDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(result.Equals(deserialized));
	}

}