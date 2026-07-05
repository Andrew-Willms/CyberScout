using Comms.Dtos;
using Comms.Serialization;
using Domain.Data;
using MatchDataDeserializationResult = Comms.Serialization.MatchDataDeserializationResult;

namespace Domain.Tests.Serialization;



public class MatchDataToCsvTests {

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestSerialization(MatchData matchData) {

		string serialized = MatchDataToCsv.Serialize(matchData);
		MatchDataDeserializationResult deserializationResult = MatchDataToCsv.Deserialize(serialized, SampleData.GameSpec);

		if (deserializationResult.IsFailure) {
			Assert.Fail();
		}


		Assert.True(matchData.Equals(deserializationResult.Value));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization(MatchData matchData) {

		CreateMatchDataDtoResult result = MatchDataDto.Create(
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

		string serialized = MatchDataDtoToCsv.Serialize(result.Value);
		MatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(result.Equals(deserialized));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization_2(MatchData matchData) {


		CreateMatchDataDtoResult result = MatchDataDto.Create(
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

		string serialized = MatchDataDtoToCsv.Serialize(result.Value);
		MatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(result.Equals(deserialized));
	}

}