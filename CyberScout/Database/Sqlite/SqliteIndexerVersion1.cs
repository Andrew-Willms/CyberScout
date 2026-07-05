using System.Diagnostics;
using Comms.Dtos;
using Database.Domain;
using Microsoft.Data.Sqlite;
using OneOf;
using SqliteUtilities;
using UtilitiesLibrary.Results;

namespace Database.Sqlite;



public enum RecordType {
	Game,
	Event,
	Match
}



public class SqliteIndexerVersion1 {

	private readonly SqliteConnection Connection;

	public SqliteIndexerVersion1(SqliteConnection connection) {
		Connection = connection;
	}



	public async Task<Result> SetGameIndexMetaData(string deviceId, long recordId, GameIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Game);
	}

	public async Task<Result> SetEventIndexMetaData(string deviceId, long recordId, EventIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Event);
	}

	public async Task<Result> SetMatchIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Match);
	}

	private async Task<Result> SetRecordMetaData(string deviceId, long recordId, RecordMetaData metaData, RecordType type) {

		Result<SuperRange> superRangeResult = await GetSuperRange(deviceId, recordId, type);
		if (superRangeResult.IsFailure) {
			return superRangeResult.Error;
		}
		SuperRange existingRanges = superRangeResult.Value;

		Result<SuperRange> updatedRangeSetResult = existingRanges.OverwriteRangeAndSimplify(recordId, metaData);
		if (updatedRangeSetResult.IsFailure) {
			return new AdHocError(
				"Error overwriting SuperRange",
				updatedRangeSetResult.Error,
				("recordId", recordId.ToString()),
				("metaData", metaData.ToString()));
		}
		SuperRange updatedRangeSet = updatedRangeSetResult.Value;

		foreach (IndexRange range in existingRanges.Ranges) {

			Result deleteResult = await DeleteRangeFromIndex(range);
			if (deleteResult.IsFailure) {
				return deleteResult.Error;
			}
		}

		foreach (IndexRange range in updatedRangeSet.Ranges) {

			Result addResult = await AddRangeToIndex(range);
			if (addResult.IsFailure) {
				return addResult.Error;
			}
		}

		return Result.Success;
	}



	public async Task<Result> SetMatchIndexMetaData(GameDto gameDto, MatchIndexMetaData newMetaData) {

		Result<List<IndexRange>> rangesResult = await GetMatchIndexRanges(gameDto);

		if (rangesResult.IsFailure) {
			return new AdHocError("Error getting ranges.", rangesResult.Error);
		}

		return await SetMatchIndexMetaData(rangesResult.Value, newMetaData);
	}

	public async Task<Result> SetMatchIndexMetaData(string eventCode, MatchIndexMetaData newMetaData) {

		Result<List<IndexRange>> rangesResult = await GetMatchIndexRanges(eventCode);

		if (rangesResult.IsFailure) {
			return new AdHocError("Error getting ranges.", rangesResult.Error);
		}

		return await SetMatchIndexMetaData(rangesResult.Value, newMetaData);
	}

	private async Task<Result> SetMatchIndexMetaData(List<IndexRange> existingRanges, MatchIndexMetaData newMetaData) {

		foreach (IndexRange existingRange in existingRanges) {

			Result<SuperRange> existingSuperRangeResult = await GetSuperRange(existingRange);
			if (existingSuperRangeResult.IsFailure) {
				return new AdHocError("Error getting SuperRange.", existingSuperRangeResult.Error, ("range", existingRange.ToString()));
			}

			SuperRange existingSuperRange = existingSuperRangeResult.Value;
			Result<SuperRange> updatedSuperRangeResult = existingSuperRange.OverwriteRangeAndSimplify(existingRange, newMetaData);
			if (updatedSuperRangeResult.IsFailure) {
				return new AdHocError("Error overwriting SuperRange.", updatedSuperRangeResult.Error, ("range", existingRange.ToString()));
			}
			SuperRange updatedSuperRange = updatedSuperRangeResult.Value;

			foreach (IndexRange oldRange in existingSuperRange.Ranges) {

				Result deleteResult = await DeleteRangeFromIndex(oldRange);
				if (deleteResult.IsFailure) {
					return new AdHocError("Error deleting IndexRange.", deleteResult.Error, ("range", existingRange.ToString()), ("existingRange", oldRange.ToString()));
				}
			}

			foreach (IndexRange newRange in updatedSuperRange.Ranges) {

				Result addResult = await AddRangeToIndex(newRange);
				if (addResult.IsFailure) {
					return new AdHocError("Error creating IndexRange.", addResult.Error, ("range", existingRange.ToString()), ("existingRange", newRange.ToString()));
				}
			}
		}

		return Result.Success;
	}



	public async Task<Result> ResetGameIndex() {
		return await ResetIndex(RecordType.Game);
	}

	public async Task<Result> ResetEventIndex() {
		return await ResetIndex(RecordType.Event);
	}

	public async Task<Result> ResetMatchIndex() {
		return await ResetIndex(RecordType.Match);
	}

	private async Task<Result> ResetIndex(RecordType type) {

		string tableName = type switch {
			RecordType.Game => nameof(Tables.GameIndex),
			RecordType.Event => nameof(Tables.EventIndex),
			RecordType.Match => nameof(Tables.MatchIndex),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

		SqliteCommand deleteRecordRange = new($"DELETE FROM \"{tableName}\";", Connection);

		ExecuteNonQueryAndExpectResult result = await deleteRecordRange.ExecuteNonQueryAndExpect(1);
		if (result.IsFailure) {
			return new AdHocError("Error deleting ranges.", result.Error, ("RecordType", type.ToString()));
		}

		return Result.Success;
	}



	private async Task<Result<IndexRange>> GetRange(string deviceId, long recordId, RecordType type) {

		string tableName = type switch {
			RecordType.Game => nameof(Tables.GameIndex),
			RecordType.Event => nameof(Tables.EventIndex),
			RecordType.Match => nameof(Tables.MatchIndex),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null) // TODO closed enum
		};

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{tableName}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" <= @Index
			   AND "{Tables.MatchIndex.EndIndex}" >= @Index
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@Index", SqliteType.Integer) { Value = recordId });

		ReaderResult readerResult = await getIndexRange.SafeExecuteReader();
		if (readerResult.IsFailure) {
			return new AdHocError("Exception executing SQLite reader.", readerResult.Error);
		}
		SqliteDataReader reader = readerResult.Value;

		if (!reader.Read()) {
			return new AdHocError("No rows in reader");
		}

		return ParseRangeFromReader(reader, type);
	}

	private async Task<Result<List<IndexRange>>> GetMatchIndexRanges(GameDto gameDto) {

		SqliteCommand getIndexRanges = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.GameDeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.GameId}" = @GameId
			 """,
			Connection);

		getIndexRanges.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		getIndexRanges.Parameters.Add(new("@GameId", SqliteType.Text) { Value = gameDto.GameId });

		return await GetMatchIndexRanges(getIndexRanges, RecordType.Game);
	}

	private async Task<Result<List<IndexRange>>> GetMatchIndexRanges(string eventCode) {

		SqliteCommand getIndexRanges = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.EventCode}" = @EventCode
			 """,
			Connection);

		getIndexRanges.Parameters.Add(new("@EventCode", SqliteType.Text) { Value = eventCode });

		return await GetMatchIndexRanges(getIndexRanges, RecordType.Event);
	}

	private static async Task<Result<List<IndexRange>>> GetMatchIndexRanges(SqliteCommand command, RecordType type) {

		ReaderResult readerResult = await command.SafeExecuteReader();
		if (readerResult.IsFailure) {
			return new AdHocError("Error executing reader.", readerResult.Error);
		}
		SqliteDataReader reader = readerResult.Value;

		List <IndexRange> ranges = [];
		while (reader.Read()) {

			Result<IndexRange> rangeResult = ParseRangeFromReader(reader, type);
			if (rangeResult.IsFailure) {
				return new AdHocError("Error parsing range.");
			}

			ranges.Add(rangeResult.Value);
		}

		return ranges;
	}

	private static Result<IndexRange> ParseRangeFromReader(SqliteDataReader reader, RecordType type) {

		GetTextResult deviceIdResult = reader.SafeGetText(Tables.MatchIndex.DeviceId);
		if (deviceIdResult.IsFailure) {
			return new AdHocError("Error getting DeviceId.", deviceIdResult.Error);
		}
		string deviceId = deviceIdResult.Value;

		GetIntegerResult startIndexResult = reader.SafeGetInteger(Tables.MatchIndex.StartIndex);
		if (startIndexResult.IsFailure) {
			return new AdHocError("Error getting StartIndex.", startIndexResult.Error);
		}
		long startIndex = startIndexResult.Value;

		GetIntegerResult endIndexResult = reader.SafeGetInteger(Tables.MatchIndex.EndIndex);
		if (endIndexResult.IsFailure) {
			return new AdHocError("Error getting EndIndex.", endIndexResult.Error);
		}
		long endIndex = startIndexResult.Value;

		GetEnumResult<RecordStatus> statusResult = reader.SafeGetTextEnum<RecordStatus>(Tables.MatchIndex.Status);
		if (statusResult.IsFailure) {
			return new AdHocError("Error getting Status.", statusResult.Error);
		}
		RecordStatus status = statusResult.Value;

		if (type == RecordType.Game) {

			GameIndexMetaData gameMetaData = new() { Status = status };
			Result<IndexRange> gameIndexRangeResult = IndexRange.Create(deviceId, startIndex, endIndex, gameMetaData);
			if (gameIndexRangeResult.IsFailure) {
				return new AdHocError("Error creating GameIndexRange", gameIndexRangeResult.Error);
			}

			return gameIndexRangeResult.Value;
		}

		if (type == RecordType.Event) {

			EventIndexMetaData eventMetaData = new() { Status = status };
			Result<IndexRange> eventIndexRangeResult = IndexRange.Create(deviceId, startIndex, endIndex, eventMetaData);
			if (eventIndexRangeResult.IsFailure) {
				return new AdHocError("Error creating GameIndexRange", eventIndexRangeResult.Error);
			}

			return eventIndexRangeResult.Value;
		}

		GetNullableTextResult gameDeviceIdResult = reader.SafeGetNullableText(Tables.MatchIndex.GameDeviceId);
		if (gameDeviceIdResult.IsFailure) {
			return new AdHocError("Error getting GameDeviceId.", gameDeviceIdResult.Error);
		}
		OneOf<string, None> gameDeviceId = gameDeviceIdResult.Value;

		GetNullableIntegerResult gameIdResult = reader.SafeGetNullableInteger(Tables.MatchIndex.GameId);
		if (gameIdResult.IsFailure) {
			return new AdHocError("Error getting GameId.", gameIdResult.Error);
		}
		OneOf<long, None> gameId = gameIdResult.Value;

		GetNullableTextResult eventCodeResult = reader.SafeGetNullableText(Tables.MatchIndex.EventCode);
		if (eventCodeResult.IsFailure) {
			return new AdHocError("Error getting GameId.", eventCodeResult.Error);
		}
		OneOf<string, None> eventDataId = eventCodeResult.Value.Value;

		MatchIndexMetaData metaData;
		if (status == RecordStatus.Stored) {

			if (gameDeviceId.IsT1) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "GameDeviceId must be not null if the record has the stored status."
				};
			}

			if (gameId.IsT1) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "GameId must be not null if the record has the stored status."
				};
			}

			if (eventDataId.IsT1) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "EventDataId must be not null if the record has the stored status."
				};
			}

			metaData = MatchIndexMetaData.CreateStoredMatch(gameDeviceId.AsT0, gameId.AsT0, eventDataId.AsT0);

		} else {

			if (gameDeviceId.IsT0) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "GameDeviceId must be null if the record does not have the stored status."
				};
			}

			if (gameId.IsT0) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "GameId must be null if the record does not have the stored status."
				};
			}

			if (eventDataId.IsT0) {
				return new AdHocError(("deviceId", deviceId), ("startIndex", startIndex.ToString())) {
					Message = "EventDataId must be null if the record does not have the stored status."
				};
			}

			metaData = status switch {
				RecordStatus.None => MatchIndexMetaData.CreateNoneMatch(),
				RecordStatus.Stored => throw new UnreachableException(),
				RecordStatus.Ignored => MatchIndexMetaData.CreateIgnoredMatch(),
				_ => throw new UnreachableException() // TODO: replace with closed enums in c# 15
			};
		}

		Result<IndexRange> rangeResult = IndexRange.Create(deviceId, startIndex, endIndex, metaData);

		if (rangeResult.IsFailure) {
			return new AdHocError(("deviceId", deviceId), ("metaData", metaData.ToString())) {
				Message = "Error creating range.",
				InternalError = rangeResult.Error
			};
		}

		return rangeResult.Value;
	}



	private async Task<Result<SuperRange>> GetSuperRange(string deviceId, long recordId, RecordType type) {

		Result<IndexRange> containingRangeResult = await GetRange(deviceId, recordId, type);
		if (containingRangeResult.IsFailure) {
			return new AdHocError(
				"Error getting containing range.",
				containingRangeResult.Error,
				("deviceId", deviceId),
				("recordId", recordId.ToString()),
				("type", type.ToString()));
		}

		IndexRange containingRange = containingRangeResult.Value;
		List<IndexRange> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (recordId == containingRange.Start && recordId != 0) {

			Result<IndexRange> precedingRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (precedingRangeResult.IsFailure) {
				return new AdHocError(
					"Error getting the preceding range.",
					precedingRangeResult.Error,
					("deviceId", deviceId),
					("recordId", recordId.ToString()),
					("type", type.ToString()));
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		// The containing range is always relevant.
		relevantRanges.Add(containingRange);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (recordId == containingRange.End && recordId != long.MaxValue) {

			Result<IndexRange> subsequentRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (subsequentRangeResult.IsFailure) {
				return new AdHocError(
					"Error getting the subsequent range.",
					subsequentRangeResult.Error,
					("deviceId", deviceId),
					("recordId", recordId.ToString()),
					("type", type.ToString()));
			}

			relevantRanges.Add(subsequentRangeResult.Value);
		}

		// Create the RangeSet.
		Result<SuperRange> superRangeResult = SuperRange.Create(relevantRanges);
		if (superRangeResult.IsFailure) {
			return new AdHocError("Error creating SuperRange", superRangeResult.Error);
		}

		return superRangeResult.Value;
	}

	private async Task<Result<SuperRange>> GetSuperRange(IndexRange range) {

		RecordType type = range.MetaData switch {
			EventIndexMetaData => RecordType.Event,
			GameIndexMetaData => RecordType.Game,
			MatchIndexMetaData => RecordType.Match,
			_ => throw new UnreachableException() // todo remove with closed hierarchies
		};

		List<IndexRange> relevantRanges = new(3);

		// If the index isn't the first possible index (0) then we need to get the preceding range.
		if (range.Start != 0) {

			Result<IndexRange> precedingRangeResult = await GetRange(range.DeviceId, range.Start - 1, type);

			if (precedingRangeResult.IsFailure) {
				return new AdHocError(("range", range.ToString())) {
					Message = "Error getting the preceding range.",
					InternalError = precedingRangeResult.Error
				};
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		relevantRanges.Add(range);

		// If the index isn't the last possible index (2^63) then we need to get the subsequent range.
		if (range.End != long.MaxValue) {

			Result<IndexRange> subsequentRangeResult = await GetRange(range.DeviceId, range.End + 1, type);

			if (subsequentRangeResult.IsFailure) {
				return new AdHocError(("range", range.ToString())) {
					Message = "Error getting the subsequent range.",
					InternalError = subsequentRangeResult.Error
				};
			}

			relevantRanges.Add(subsequentRangeResult.Value);
		}

		// Create the RangeSet.
		Result<SuperRange> superRangeResult = SuperRange.Create(relevantRanges);
		if (superRangeResult.IsFailure) {
			return new AdHocError("Error creating SuperRange", superRangeResult.Error);
		}

		return superRangeResult.Value;
	}

	

	private async Task<Result> AddRangeToIndex(IndexRange range) {

		SqliteCommand addRecordRange;

		switch (range.MetaData) {

			case GameIndexMetaData:
				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.GameIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = range.DeviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });
				break;

			case EventIndexMetaData:
				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.EventIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = range.DeviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });
				break;

			case MatchIndexMetaData metaData:

				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.MatchIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}",
					     "{Tables.MatchIndex.GameDeviceId}",
					     "{Tables.MatchIndex.GameId}",
					     "{Tables.MatchIndex.EventCode}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status,
					     @GameDeviceId,
					     @GameId,
					     @EventCode,
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = range.DeviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = metaData.Status });
				addRecordRange.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = metaData.GameDeviceId });
				addRecordRange.Parameters.Add(new("@GameId", SqliteType.Text) { Value = metaData.GameId });
				addRecordRange.Parameters.Add(new("@EventCode", SqliteType.Text) { Value = metaData.EventCode });
				break;

			default:
				throw new UnreachableException(); // TODO removed with closed class hierarchy
		}

		ExecuteNonQueryAndExpectResult result = await addRecordRange.ExecuteNonQueryAndExpect(1);
		if (result.IsFailure) {
			return new AdHocError("Error adding range.", result.Error, ("IndexRange", range.ToString()));
		}

		return Result.Success;
	}

	private async Task<Result> DeleteRangeFromIndex(IndexRange range) {

		string tableName = range.MetaData switch {
			GameIndexMetaData => nameof(Tables.GameIndex),
			EventIndexMetaData => nameof(Tables.EventIndex),
			MatchIndexMetaData => nameof(Tables.MatchIndex),
			_ => throw new UnreachableException() // TODO removed with closed class hierarchy
		};

		SqliteCommand deleteRecordRange = new(
			$"""
			 DELETE FROM "{tableName}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" = @StartIndex
			   AND "{Tables.MatchIndex.EndIndex}" = @EndIndex
			   AND "{Tables.MatchIndex.Status}" = @Status;
			 """,
			Connection);

		deleteRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = range.DeviceId });
		deleteRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
		deleteRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
		deleteRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });

		ExecuteNonQueryAndExpectResult result = await deleteRecordRange.ExecuteNonQueryAndExpect(1);
		if (result.IsFailure) {
			return new AdHocError("Error deleting range.", result.Error, ("IndexRange", range.ToString()));
		}

		return Result.Success;
	}

}