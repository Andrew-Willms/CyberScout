using System.Diagnostics;
using Comms.Dtos;
using Database.Results;
using Microsoft.Data.Sqlite;
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

		GetSuperRangeResult superRangeResult = await GetSuperRangeAround(deviceId, recordId, type);
		if (superRangeResult.IsFailure) {
			return superRangeResult.Error;
		}
		SuperRange existingRanges = superRangeResult.Value;

		SuperRange? updatedRangeSet = existingRanges.OverwriteIndexAndSimplify(recordId, metaData);
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



	public async Task<BulkSetRecordMetaDataResult> SetMatchIndexMetaData(EventDto eventDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
	}

	public async Task<BulkSetRecordMetaDataResult> SetMatchIndexMetaData(GameDto gameDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
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
			return await RollbackError<ExecuteNonQueryAndExpectError>.TryRollback(deleteRecordRangeError, Connection);
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

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<GetRangesResult> GetRanges(EventDto eventDto) {

	}

	private async Task<GetSuperRangeResult> GetSuperRangeAround(string deviceId, long recordId, RecordType type) {

		GetRangeResult containingRangeResult = await GetRange(deviceId, recordId, type);
		if (containingRangeResult.IsFailure) {
			return new GetContainingRangeError(containingRangeResult.Error, deviceId, recordId, type);
		}

		IndexRange containingRange = containingRangeResult.Value;
		List<IndexRange> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (containingRange.Start == recordId && recordId != 0) {

			GetRangeResult precedingRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (precedingRangeResult.IsFailure) {
				return new GetPrecedingRangeError(precedingRangeResult.Error, deviceId, recordId, type);
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		// The containing range is always relevant.
		relevantRanges.Add(containingRange);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (recordId == containingRange.End && recordId != long.MaxValue) {

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
				addRecordRange.Parameters.Add(new("@EventDataId", SqliteType.Text) { Value = metaData.EventDeviceId });
				break;

			default:
				throw new UnreachableException(); // TODO removed with closed class hierarchy
		}

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addRecordRangeError) {
			return await RollbackError<InsertDataResult>.TryRollback(addRecordRangeError, Connection);
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
			return await RollbackError<DeleteDataError>.TryRollback(deleteRecordRangeError, Connection);
		}

		return Success.Instance;
	}

}