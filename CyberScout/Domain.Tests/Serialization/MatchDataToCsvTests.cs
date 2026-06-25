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

		if (deserializationResult.IsT1) {
			Assert.Fail();
		}


		Assert.True(matchData.Equals(deserializationResult.AsT0));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization(MatchData matchData) {

		MatchDataDto matchDataDto = MatchDataDto.Create(
			matchData: matchData,
			deviceId: "deviceId",
			matchId: 1,
			originalDeviceId: "originalDeviceId",
			originalMatchId: 1,
			parents: [],
			gameDeviceId: "gameDeviceId",
			gameId: 1,
			eventDeviceId: "eventDeviceId",
			eventId: 1
		)!;

		string serialized = MatchDataDtoToCsv.Serialize(matchDataDto);
		MatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(matchDataDto.Equals(deserialized));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization_2(MatchData matchData) {


		MatchDataDto matchDataDto = MatchDataDto.Create(
			matchData: matchData,
			deviceId: "deviceId",
			matchId: 1,
			originalDeviceId: "originalDeviceId",
			originalMatchId: 1,
			parents: [],
			gameDeviceId: "gameDeviceId",
			gameId: 1,
			eventDeviceId: "eventDeviceId",
			eventId: 1
		)!;

		string serialized = MatchDataDtoToCsv.Serialize(matchDataDto);
		MatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(matchDataDto.Equals(deserialized));
	}

}