using System.Diagnostics;
using Comms.Dtos;
using Database.Results;
using Microsoft.Data.Sqlite;
using OneOf;
using SqliteUtilities;

namespace Database.Sqlite.Indexer;



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



	public async Task<SetRecordMetaDataResult> SetGameIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Game);
	}

	public async Task<SetRecordMetaDataResult> SetEventIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Event);
	}

	public async Task<SetRecordMetaDataResult> SetMatchIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Match);
	}

	private async Task<SetRecordMetaDataResult> SetRecordMetaData(string deviceId, long recordId, RecordMetaData metaData, RecordType type) {

		GetSuperRangeResult superRangeResult = await GetSuperRange(deviceId, recordId, type);
		if (superRangeResult.IsFailure) {
			return superRangeResult.Error;
		}
		SuperRange existingRanges = superRangeResult.Value;

		SuperRange? updatedRangeSet = existingRanges.OverwriteRangeAndSimplify(recordId, metaData);
		if (updatedRangeSet is null) {
			return new RangeOperationError();
		}

		foreach (IndexRange range in existingRanges.Ranges) {

			DeleteRangeFromIndexResult deleteResult = await DeleteRangeFromIndex(deviceId, range);
			if (deleteResult.IsFailure) {
				return deleteResult.Error;
			}
		}

		foreach (IndexRange range in updatedRangeSet.Ranges) {

			AddRangeToIndexResult addResult = await AddRangeToIndex(deviceId, range);
			if (addResult.IsFailure) {
				return addResult.Error;
			}
		}

		return Success.Instance;
	}



	public async Task<BulkSetRecordMetaDataResult> SetMatchIndexMetaData(EventDto eventDto, MatchIndexMetaData newMetaData) {
		return await GenericSetMatchIndexMetaData(eventDto, newMetaData);
	}

	public async Task<BulkSetRecordMetaDataResult> SetMatchIndexMetaData(GameDto gameDto, MatchIndexMetaData newMetaData) {
		return await GenericSetMatchIndexMetaData(gameDto, newMetaData);
	}

	private async Task<BulkSetRecordMetaDataResult> GenericSetMatchIndexMetaData(object dto, MatchIndexMetaData newMetaData) {

		GetRangesResult rangesResult = dto switch {
			EventDto eventDto => await GetRanges(eventDto),
			GameDto gameDto => await GetRanges(gameDto),
			_ => throw new UnreachableException()
		};

		if (rangesResult.IsFailure) {
			return null;
		}
		List<IndexRange>? ranges = rangesResult.Value;

		foreach (IndexRange range in ranges) {

			GetSuperRangeResult existingSuperRangeResult = await GetSuperRange(range);
			if (existingSuperRangeResult.IsFailure) {
				return null;
			}

			SuperRange existingSuperRange = existingSuperRangeResult.Value;
			SuperRange? updatedSuperRange = existingSuperRange.OverwriteRangeAndSimplify(range, newMetaData);

			if (updatedSuperRange is null) {
				return null;
			}

			foreach (IndexRange existingRange in existingSuperRange.Ranges) {

				DeleteRangeFromIndexResult deleteResult = await DeleteRangeFromIndex(range.DeviceId, existingRange);
				if (deleteResult.IsFailure) {
					return null;
				}
			}

			foreach (IndexRange newRange in updatedSuperRange.Ranges) {

				AddRangeToIndexResult addResult = await AddRangeToIndex(range.DeviceId, newRange);
				if (addResult.IsFailure) {
					return null;
				}
			}
		}

		return Success.Instance;
	}



	public async Task<ResetIndexResult> ResetGameIndex() {
		return await ResetIndex(RecordType.Game);
	}

	public async Task<ResetIndexResult> ResetEventIndex() {
		return await ResetIndex(RecordType.Event);
	}

	public async Task<ResetIndexResult> ResetMatchIndex() {
		return await ResetIndex(RecordType.Match);
	}

	private async Task<ResetIndexResult> ResetIndex(RecordType type) {

		string tableName = type switch {
			RecordType.Game => nameof(Tables.GameIndex),
			RecordType.Event => nameof(Tables.EventIndex),
			RecordType.Match => nameof(Tables.MatchIndex),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

		SqliteCommand deleteRecordRange = new($"DELETE FROM \"{tableName}\";", Connection);

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError deleteRecordRangeError) {
			return deleteRecordRangeError;
		}

		return Success.Instance;
	}



	private async Task<GetRangeResult> GetRange(string deviceId, long recordId, RecordType type) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" <= @Index
			   AND "{Tables.MatchIndex.EndIndex}" >= @Index
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@Index", SqliteType.Integer) { Value = recordId });

		SqliteDataReader reader;
		try {
			reader = await getIndexRange.ExecuteReaderAsync();
		} catch (Exception exception) {
			return null;
		}

		SafeGetIntegerResult startIndexResult = reader.SafeGetInteger(Tables.MatchIndex.StartIndex);
		if (startIndexResult.IsFailure) {
			return null;
		}

		long startIndex = startIndexResult.Value;

		SafeGetIntegerResult endIndexResult = reader.SafeGetInteger(Tables.MatchIndex.EndIndex);
		if (endIndexResult.IsFailure) {
			return null;
		}

		long endIndex = startIndexResult.Value;

		SafeGetTextResult statusResult = reader.SafeGetText(Tables.MatchIndex.Status);
		if (statusResult.IsFailure) {
			return null;
		}

		if (statusResult.Value != nameof(RecordStatus.Stored)) {
			return null;
		}

		SafeGetNullableTextResult gameDeviceIdResult = reader.SafeGetNullableText(Tables.MatchIndex.GameDeviceId);
		if (gameDeviceIdResult.IsFailure) {
			return null;
		}
		OneOf<string, None> gameDeviceId = gameDeviceIdResult.Value;

		SafeGetNullableIntegerResult gameIdResult = reader.SafeGetNullableInteger(Tables.MatchIndex.GameId);
		if (gameIdResult.IsFailure) {
			return null;
		}
		OneOf<long, None> gameId = gameIdResult.Value;

		SafeGetNullableIntegerResult eventDataIdResult = reader.SafeGetNullableInteger(Tables.MatchIndex.EventDataId);
		if (eventDataIdResult.IsFailure) {
			return null;
		}
		OneOf<long, None> eventDataId = eventDataIdResult.Value.Value;

		if (eventDataId.IsT1) {
			return null;
		}

		if (gameDeviceId.IsT1) {
			return null;
		}

		if (gameId.IsT1) {
			return null;
		}

		// In the query we only get rows with a set EventDataId so we should only get stored rows.
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(gameDeviceId.AsT0, gameId.AsT0, eventDataId.AsT0);
		IndexRange? range = IndexRange.Create(deviceId, startIndex, endIndex, metaData);

		if (range is null) {
			return null;
		}

		return range;
	}

	private async Task<GetRangesResult> GetRanges(EventDto eventDto) {

		SqliteCommand getEventDataId = new(
			$"""
			 SELECT "{Tables.EventMetaData.DataId}" FROM "{nameof(Tables.EventMetaData)}"
			 WHERE "{Tables.EventMetaData.DeviceId}" = @DeviceId
			   AND "{Tables.EventMetaData.MetaDataId}" <= @MetaDataId
			 """,
			Connection);

		getEventDataId.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = eventDto.DeviceId });
		getEventDataId.Parameters.Add(new("@MetaDataId", SqliteType.Integer) { Value = eventDto.MetaDataId });

		IntegerScalarResult getEventDataIdResult = await getEventDataId.ExecuteIntegerScalar();
		if (getEventDataIdResult.IsFailure) {
			return new GetEventDataIdError { Error = getEventDataIdResult.Error };
		}

		long eventDataId = getEventDataIdResult.Value;

		SqliteCommand getIndexRanges = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.EventDataId}" = @EventDataId
			 """,
			Connection);

		getIndexRanges.Parameters.Add(new("@EventDataId", SqliteType.Text) { Value = eventDataId });

		return await GetRanges(getIndexRanges);
	}

	private async Task<GetRangesResult> GetRanges(GameDto gameDto) {

		SqliteCommand getIndexRanges = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.GameDeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.GameId}" = @GameId
			 """,
			Connection);

		getIndexRanges.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		getIndexRanges.Parameters.Add(new("@GameId", SqliteType.Text) { Value = gameDto.GameId });

		return await GetRanges(getIndexRanges);
	}

	private static async Task<GetRangesResult> GetRanges(SqliteCommand command) {

		SqliteDataReader reader;
		try {
			reader = await command.ExecuteReaderAsync();
		} catch (Exception exception) {
			return new ReadDataError(ExceptionError.FromException(exception, command));
		}

		List<IndexRange> ranges = [];
		while (reader.Read()) {

			SafeGetTextResult deviceIdResult = reader.SafeGetText(Tables.MatchIndex.DeviceId);
			if (deviceIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.MatchId, deviceIdResult.Error);
			}

			string deviceId = deviceIdResult.Value;

			SafeGetIntegerResult startIndexResult = reader.SafeGetInteger(Tables.MatchIndex.StartIndex);
			if (startIndexResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.MatchId, startIndexResult.Error);
			}

			long startIndex = startIndexResult.Value;

			SafeGetIntegerResult endIndexResult = reader.SafeGetInteger(Tables.MatchIndex.EndIndex);
			if (endIndexResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.MatchId, endIndexResult.Error);
			}

			long endIndex = startIndexResult.Value;

			SafeGetTextResult statusResult = reader.SafeGetText(Tables.MatchIndex.Status);
			if (statusResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.DeviceId, statusResult.Error);
			}

			if (statusResult.Value != nameof(RecordStatus.Stored)) {
				return new StatusShouldBeStoredError();
			}

			SafeGetNullableTextResult gameDeviceIdResult = reader.SafeGetNullableText(Tables.MatchIndex.GameDeviceId);
			if (gameDeviceIdResult.IsFailure) {
				return new NullableColumnReadError(Tables.MatchData.DeviceId, gameDeviceIdResult.Error);
			}
			OneOf<string, None> gameDeviceId = gameDeviceIdResult.Value;

			SafeGetNullableIntegerResult gameIdResult = reader.SafeGetNullableInteger(Tables.MatchIndex.GameId);
			if (gameIdResult.IsFailure) {
				return new NullableColumnReadError(Tables.MatchData.MatchId, gameIdResult.Error);
			}
			OneOf<long, None> gameId = gameIdResult.Value;

			SafeGetNullableIntegerResult eventDataIdResult = reader.SafeGetNullableInteger(Tables.MatchIndex.EventDataId);
			if (eventDataIdResult.IsFailure) {
				return new NullableColumnReadError(Tables.MatchData.MatchId, eventDataIdResult.Error);
			}
			OneOf<long, None> eventDataId = eventDataIdResult.Value.Value;

			if (eventDataId.IsT1) {
				return new ColumnNullWhenShouldNotBeError { ColumnName = Tables.MatchIndex.EventDataId };
			}

			if (gameDeviceId.IsT1) {
				return new ColumnNullWhenShouldNotBeError { ColumnName = Tables.MatchIndex.GameDeviceId };
			}

			if (gameId.IsT1) {
				return new ColumnNullWhenShouldNotBeError { ColumnName = Tables.MatchIndex.GameId };
			}

			// In the query we only get rows with a set EventDataId so we should only get stored rows.
			MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(gameDeviceId.AsT0, gameId.AsT0, eventDataId.AsT0);
			IndexRange? range = IndexRange.Create(deviceId, startIndex, endIndex, metaData);

			if (range is null) {
				return new RangeCreationError();
			}

			ranges.Add(range);
		}

		return ranges;
	}



	private async Task<GetSuperRangeResult> GetSuperRange(string deviceId, long recordId, RecordType type) {

		GetRangeResult containingRangeResult = await GetRange(deviceId, recordId, type);
		if (containingRangeResult.IsFailure) {
			return new GetContainingRangeError(containingRangeResult.Error, deviceId, recordId, type);
		}

		IndexRange containingRange = containingRangeResult.Value;
		List<IndexRange> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (recordId != 0) {

			GetRangeResult precedingRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (precedingRangeResult.IsFailure) {
				return new GetPrecedingRangeError(precedingRangeResult.Error, deviceId, recordId, type);
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		// The containing range is always relevant.
		relevantRanges.Add(containingRange);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (recordId != long.MaxValue) {

			GetRangeResult subsequentRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (subsequentRangeResult.IsFailure) {
				return new GetSubsequentRangeError(subsequentRangeResult.Error, deviceId, recordId, type);
			}

			relevantRanges.Add(subsequentRangeResult.Value);
		}

		// Create the RangeSet.
		SuperRange? superRange = SuperRange.Create(relevantRanges);
		if (superRange is null) {
			return new RangeOperationError();
		}

		return superRange;
	}

	private async Task<GetSuperRangeResult> GetSuperRange(IndexRange range) {

		RecordType type = range.MetaData switch {
			GameIndexMetaData => RecordType.Game,
			EventIndexMetaData => RecordType.Event,
			MatchIndexMetaData => RecordType.Match,
			_ => throw new UnreachableException()
		};

		List<IndexRange> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (range.Start != 0) {

			GetRangeResult precedingRangeResult = await GetRange(range.DeviceId, range.Start - 1, type);

			if (precedingRangeResult.IsFailure) {
				return null;
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		relevantRanges.Add(range);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (range.End != long.MaxValue) {

			GetRangeResult subsequentRangeResult = await GetRange(range.DeviceId, range.End + 1, type);

			if (subsequentRangeResult.IsFailure) {
				return null;
			}

			relevantRanges.Add(subsequentRangeResult.Value);
		}

		// Create the RangeSet.
		SuperRange? superRange = SuperRange.Create(relevantRanges);
		if (superRange is null) {
			return new RangeOperationError();
		}

		return superRange;
	}

	

	private async Task<AddRangeToIndexResult> AddRangeToIndex(string deviceId, IndexRange range) {

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

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
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

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
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
					     "{Tables.MatchIndex.EventDataId}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status,
					     @GameDeviceId,
					     @GameId,
					     @EventDataId,
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = metaData.Status });
				addRecordRange.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = metaData.GameDeviceId });
				addRecordRange.Parameters.Add(new("@GameId", SqliteType.Text) { Value = metaData.GameId });
				addRecordRange.Parameters.Add(new("@EventDataId", SqliteType.Text) { Value = metaData.EventDataId });
				break;

			default:
				throw new UnreachableException(); // TODO removed with closed class hierarchy
		}

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addRecordRangeError) {
			return new InsertDataResult(addRecordRangeError);
		}

		return Success.Instance;
	}

	private async Task<DeleteRangeFromIndexResult> DeleteRangeFromIndex(string deviceId, IndexRange range) {

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

		deleteRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		deleteRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
		deleteRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
		deleteRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError deleteRecordRangeError) {
			return new DeleteDataError(deleteRecordRangeError);
		}

		return Success.Instance;
	}

}