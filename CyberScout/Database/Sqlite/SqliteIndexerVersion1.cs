using System.Diagnostics;
using Comms.Dtos;
using Database.Results;
using Microsoft.Data.Sqlite;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite;



public class SqliteIndexerVersion1 {

	private enum RecordType {
		Game,
		Event,
		Match
	}

	private readonly SqliteConnection Connection;

	public SqliteIndexerVersion1(SqliteConnection connection) {
		Connection = connection;
	}



	public async Task<DataStoreError?> SetGameIndexMetaData(string deviceId, long index, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, index, metaData, RecordType.Game);
	}

	public async Task<DataStoreError?> SetEventIndexMetaData(string deviceId, long index, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, index, metaData, RecordType.Event);
	}

	public async Task<DataStoreError?> SetMatchIndexMetaData(string deviceId, long index, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, index, metaData, RecordType.Match);
	}

	private async Task<DataStoreError?> SetRecordMetaData(string deviceId, long index, RecordMetaData metaData, RecordType type) {

		AsyncTryResult<SuperRange, DataStoreError> superRangeResult = await GetSuperRangeAround(deviceId, index, type);
		if (superRangeResult.IsFailure) {
			return superRangeResult.Error;
		}
		SuperRange existingRanges = superRangeResult.Value;

		SuperRange? updatedRangeSet = existingRanges.OverwriteIndexAndSimplify(index, metaData);
		if (updatedRangeSet is null) {
			return new RangeOperationError();
		}

		foreach (Ranges range in existingRanges.Ranges) {

			if (await DeleteRangeFromIndex(deviceId, range) is DataStoreError error) {
				return error;
			}
		}

		foreach (Ranges range in updatedRangeSet.Ranges) {

			if (await AddRangeToIndex(deviceId, range) is DataStoreError error) {
				return error;
			}
		}

		return null;
	}



	public async Task<DataStoreError?> SetMatchIndexMetaData(EventDto eventDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
	}

	public async Task<DataStoreError?> SetMatchIndexMetaData(GameDto gameDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
	}



	public async Task<DataStoreError?> ResetGameIndex() {
		return await ResetIndex(RecordType.Game);
	}

	public async Task<DataStoreError?> ResetEventIndex() {
		return await ResetIndex(RecordType.Event);
	}

	public async Task<DataStoreError?> ResetMatchIndex() {
		return await ResetIndex(RecordType.Match);
	}

	private async Task<DataStoreError?> ResetIndex(RecordType type) {

		string tableName = type switch {
			RecordType.Game => nameof(Tables.GameIndex),
			RecordType.Event => nameof(Tables.EventIndex),
			RecordType.Match => nameof(Tables.MatchIndex),
			_ => throw new UnreachableException() // TODO removed with closed class hierarchy
		};

		SqliteCommand deleteRecordRange = new($"DELETE FROM \"{tableName}\";", Connection);

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError deleteRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(deleteRecordRangeError, Connection);
		}

		return null;
	}



	private async Task<AsyncTryResult<Ranges, DataStoreError>> GetRangeContaining(string deviceId, long index, RecordType type) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" <= @Index
			   AND "{Tables.MatchIndex.EndIndex}" >= @Index
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@Index", SqliteType.Integer) { Value = index });

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<AsyncTryResult<SuperRange, DataStoreError>> GetSuperRangeAround(string deviceId, long index, RecordType type) {

		AsyncTryResult<Ranges, DataStoreError> containingRangeResult = await GetRangeContaining(deviceId, index, type);
		if (containingRangeResult.IsFailure) {
			return containingRangeResult.Error;
		}

		Ranges containingRange = containingRangeResult.Value;
		List<Ranges> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (containingRange.Start == index && index != 0) {

			AsyncTryResult<Ranges, DataStoreError> precedingRangeResult = await GetRangeContaining(deviceId, index - 1, type);

			if (precedingRangeResult.IsFailure) {
				return precedingRangeResult.Error;
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		// The containing range is always relevant.
		relevantRanges.Add(containingRange);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (index == containingRange.End && index != long.MaxValue) {

			AsyncTryResult<Ranges, DataStoreError> subsequentRangeResult = await GetRangeContaining(deviceId, index - 1, type);

			if (subsequentRangeResult.IsFailure) {
				return subsequentRangeResult.Error;
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

	private async Task<DataStoreError?> AddRangeToIndex(string deviceId, Ranges range) {

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
				addRecordRange.Parameters.Add(new("@EventDataId", SqliteType.Text) { Value = metaData.EventDeviceId });
				break;

			default:
				throw new UnreachableException(); // TODO removed with closed class hierarchy
		}

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError addRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(addRecordRangeError, Connection);
		}

		return null;
	}

	private async Task<DataStoreError?> DeleteRangeFromIndex(string deviceId, Ranges range) {

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

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError deleteRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(deleteRecordRangeError, Connection);
		}

		return null;
	}

}