using Comms.Dtos;
using Comms.Serialization;
using Domain.Data;
using MatchDataDeserializationResult = Domain.Serialization.MatchDataDeserializationResult;

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

		ImportMatchDataDto importMatchDataDto = new() {
			MatchData = matchData,
			DeviceId = "deviceId",
			RecordId = 1,
			EditBasedOn = null
		};

		string serialized = MatchDataDtoToCsv.Serialize(importMatchDataDto);
		ImportMatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(importMatchDataDto.Equals(deserialized));
	}

	[Theory]
	[ClassData(typeof(SampleData))]
	public void TestDtoSerialization_2(MatchData matchData) {

		ImportMatchDataDto importMatchDataDto = new() {
			MatchData = matchData,
			DeviceId = "deviceId",
			RecordId = 2,
			EditBasedOn = ("deviceId", 1)
		};

		string serialized = MatchDataDtoToCsv.Serialize(importMatchDataDto);
		ImportMatchDataDto? deserialized = MatchDataDtoToCsv.Deserialize(serialized, SampleData.GameSpec);

		Assert.True(importMatchDataDto.Equals(deserialized));
	}

}