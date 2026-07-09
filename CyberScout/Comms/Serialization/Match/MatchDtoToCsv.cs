using System.Collections.Generic;
using System.Text;
using Domain.Dtos.Match;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.MiscExtensions;
using UtilitiesLibrary.Results;

namespace Comms.Serialization.Match;



public static class MatchDtoToCsv {

	private const int DeviceIdColumnIndex = 0;
	private const int MatchIdColumnIndex = 1;
	private const int OriginalDeviceIdColumnIndex = 2;
	private const int OriginalMatchIdColumnIndex = 3;
	private const int ParentsColumnIndex = 4;
	private const int GameDeviceIdColumnIndex = 5;
	private const int GameIdColumnIndex = 6;
	private const int MatchDataStartColumnId = 7;

	private static readonly string FixedCsvHeader =
		nameof(MatchDto.DeviceId) + ',' +
		nameof(MatchDto.MatchId) + ',' +
		nameof(MatchDto.OriginalDeviceId) + ',' +
		nameof(MatchDto.OriginalMatchId) + ',' +
		nameof(MatchDto.Parents) + ',' +
		nameof(MatchDto.GameDeviceId) + ',' +
		nameof(MatchDto.GameId) + ',';

	private const int FixedFieldCount = MatchDataStartColumnId + MatchDataToCsv.FixedFieldCount;

	public static string GetCsvHeaders(GameSpec gameSpecification) {

		string matchDataHeaders = MatchDataToCsv.GetCsvHeaders(gameSpecification);
		return FixedCsvHeader + matchDataHeaders;
	}

	public static string Serialize(MatchDto matchData) {

		// Estimate the device and match IDs will be about 100 characters and each field will take about 5.
		StringBuilder stringBuilder = new(100 + matchData.Data.GameSpecification.DataFields.Count * 5);
		stringBuilder.Append(matchData.DeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.MatchId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.OriginalDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.OriginalMatchId);
		stringBuilder.Append(',');
		stringBuilder.Append(Parents.ToText(matchData.Parents));
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.GameDeviceId);
		stringBuilder.Append(',');
		stringBuilder.Append(matchData.GameId);
		stringBuilder.Append(',');

		string matchDataSerialized = MatchDataToCsv.Serialize(matchData.Data);
		stringBuilder.Append(matchDataSerialized);
		return stringBuilder.ToString();
	}

	public static Result<MatchDto> Deserialize(string text, GameSpec gameSpecification) {

		List<string> columns = text.SplitTextToCsvColumns();

		if (columns.Count != FixedFieldCount + gameSpecification.DataFields.Count) {
			return new AdHocError("Wrong number of columns", ("data", text));
		}

		// todo: Create a DeviceId class with a TryParse method like this and create local variables for even the string types.
		// Not all strings are valid DeviceIds or text for the relevant field so this additional validation may be useful.

		string deviceId = columns[DeviceIdColumnIndex];

		if (!long.TryParse(columns[MatchIdColumnIndex], out long matchId)) {
			return new AdHocError("Could not parse matchId", ("text", columns[MatchIdColumnIndex]));
		}

		string originalDeviceId = columns[OriginalDeviceIdColumnIndex];

		if (!long.TryParse(columns[OriginalMatchIdColumnIndex], out long originalMatchId)) {
			return new AdHocError("Could not parse originalMatchId", ("text", columns[OriginalMatchIdColumnIndex]));
		}

		Result<List<(string deviceId, long matchId)>> parentsResult = Parents.FromText(columns[ParentsColumnIndex]);
		if (parentsResult.IsFailure) {
			return new AdHocError("Error parsing parents text", ("parents text", columns[ParentsColumnIndex]));
		}
		List<(string deviceId, long matchId)> parents = parentsResult.Value;

		string gameDeviceId = columns[GameDeviceIdColumnIndex];

		if (!long.TryParse(columns[GameIdColumnIndex], out long gameId)) {
			return new AdHocError("Could not parse matchId", ("text", columns[GameIdColumnIndex]));
		}

		Result<MatchData> matchDataResult = MatchDataToCsv.Deserialize(columns[MatchDataStartColumnId..], gameSpecification);
		if (matchDataResult.IsFailure) {
			return new AdHocError("Error parsing data.", ("data", text));
		}
		MatchData matchData = matchDataResult.Value;

		CreateMatchDataDtoResult matchDataDtoResult = MatchDto.Create(
			matchData: matchData,
			deviceId: deviceId,
			matchId: matchId,
			originalDeviceId: originalDeviceId,
			originalMatchId: originalMatchId,
			parents: parents,
			gameDeviceId: gameDeviceId,
			gameId: gameId
		);

		if (matchDataDtoResult.IsFailure) {
			return new AdHocError("Error creating MatchDataDto", ("data", text));
		}

		return matchDataDtoResult.Value;
	}

}