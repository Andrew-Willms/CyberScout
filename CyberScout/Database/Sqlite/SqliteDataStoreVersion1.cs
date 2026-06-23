using System.Diagnostics;
using System.Drawing;
using Comms.Dtos;
using Comms.Serialization;
using Database.Range;
using Database.Results;
using Database.Results.Event;
using Database.Results.GameSpec;
using Database.Results.MatchData;
using Database.Results.Scout;
using Domain.Data;
using Domain.GameSpecification;
using Microsoft.Data.Sqlite;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;
using Willmsy.AsyncTryResult;
using Success = OneOf.Types.Success;

namespace Database.Sqlite;



public class SqliteDataStoreVersion1Creator : IDataStoreCreator {

	public async Task<IDataStore?> Create(string settings) {

		return await SqliteDataStoreVersion1.Initialize(settings);
	}
}



public class SqliteDataStoreVersion1 : IDataStore {

	private const uint TargetDatabaseVersion = 1;

	private static class Tables {

		public static class DatabaseVersion {
			public const string Version = "Version";
		}

		public static class Scout {
			public const string Name = "Name";
		}

		public static class KnownDevices {
			public const string DeviceId = "DeviceId";
			public const string DeviceName = "DeviceName";
			public const string PublicKey = "PublicKey";
		}

		// I am targeting low spec Android phones with slow storage.
		// I think I should do this even if it adds overhead.
		public static class RecordIndex {
			public const string DeviceId = "DeviceId";
			public const string StartIndex = "StartIndex";
			public const string EndIndex = "EndIndex";
			public const string Status = "Status";
		}

		public static class GlobalIdSequence {
			public const string NextId = "NextId";
		}

		public static class Games {
			public const string DeviceId = "DeviceId";
			public const string GameId = "GameId";
			public const string TimePublished = "TimePublished";
			public const string Data = "Data";
			public const string MajorVersion = "MajorVersion";
			public const string MinorVersion = "MinorVersion";
			public const string PatchVersion = "PatchVersion";
		}

		public static class EventData {
			public const string EventId = "EventId";
			public const string Data = "Data";
			public const string Hash = "Hash";
		}

		public static class EventMetaData {
			public const string EventId = "EventId";
			public const string PublishedByDeviceId = "PublishedByDeviceId";
			public const string TimePublished = "TimePublished";
			public const string ManuallyCreated = "ManuallyCreated";
		}

		public static class MatchData {
			public const string DeviceId = "DeviceId";
			public const string MatchId = "MatchId";
			public const string OriginalDeviceId = "OriginalDeviceId";
			public const string OriginalMatchId = "OriginalMatchId";
			public const string GameDeviceId = "GameDeviceId";
			public const string GameId = "GameId";
			public const string Data = "Data";
		}

		public static class EditGraphVertices {
			public const string ChildDeviceId = "ChildDeviceId";
			public const string ChildMatchId = "ChildMatchId";
			public const string ParentDeviceId = "ParentDeviceId";
			public const string ParentMatchId = "ParentMatchId";
			public const string OriginalDeviceId = "OriginalDeviceId";
			public const string OriginalMatchId = "OriginalMatchId";
			public const string Comment = "Comment";
		}

	}

	private readonly SqliteConnection Connection;



	private SqliteDataStoreVersion1(SqliteConnection connection) {
		Connection = connection;
	}

	public static async Task<SqliteDataStoreVersion1?> Initialize(string dbPath) {

		// TODO: add real errors

		SqliteConnection connection;
		try {
			connection = new($"Data Source={dbPath}");
			connection.Open();
		} catch {
			return null;
		}

		bool? isEmpty = await DatabaseChecks.IsEmpty(connection);
		if (isEmpty is null) {
			return null;
		}

		if (isEmpty is true && !await Create(connection)) {
			return null;
		}

		uint? existingDatabaseVersion = await DatabaseChecks.GetDatabaseVersion(connection);
		if (existingDatabaseVersion != TargetDatabaseVersion) {
			return null;
		}

		// For database version X (where X > 1) you would have something like:
		// if (existingDatabaseVersion is null) {
		//     return null;
		// } else if (existingDatabaseVersion < TargetDatabaseVersion) {
		//     await SqliteDataStoreVersion[X-1].Initialize(connection);
		// } else if (existingDatabaseVersion != TargetDatabaseVersion) {
		//     return null;
		// }

		bool? valid = await CheckIntegrity(connection);
		if (valid != true) {
			return null;
		}

		return new(connection);
	}

	private static async Task<bool> Create(SqliteConnection connection) {

		// -------- DatabaseVersion Table --------
		SqliteCommand createDatabaseVersionTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.DatabaseVersion)}" (
			 	"{Tables.DatabaseVersion.Version}" INTEGER NOT NULL
			 );
			 
			 INSERT INTO "{nameof(Tables.DatabaseVersion)}" ("{Tables.DatabaseVersion.Version}")
			 VALUES ({TargetDatabaseVersion});
			 
			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.DatabaseVersion)}"
			 BEFORE INSERT ON "{nameof(Tables.DatabaseVersion)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.DatabaseVersion)}"
			 BEFORE DELETE ON "{nameof(Tables.DatabaseVersion)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await createDatabaseVersionTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- Scout Table --------
		SqliteCommand createScoutTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.Scout)}" (
			 	"{Tables.Scout.Name}" TEXT NOT NULL
			 );
			 
			 INSERT INTO "{nameof(Tables.Scout)}" ("{Tables.Scout.Name}")
			 VALUES ('');
			 
			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.Scout)}"
			 BEFORE INSERT ON "{nameof(Tables.Scout)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.Scout)}"
			 BEFORE DELETE ON "{nameof(Tables.Scout)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await createScoutTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- KnownDevices Table --------
		SqliteCommand createKnownDeviceTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.KnownDevices)}" (
			 	"{Tables.KnownDevices.DeviceId}" TEXT NOT NULL PRIMARY KEY,
			 	"{Tables.KnownDevices.DeviceName}" INTEGER NOT NULL,
			 	"{Tables.KnownDevices.PublicKey}" TEXT NOT NULL
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.KnownDevices)}"
			 BEFORE UPDATE ON "{nameof(Tables.KnownDevices)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await createKnownDeviceTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- RecordIndex Table --------
		SqliteCommand createRecordIndexTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.RecordIndex)}" (
			 	"{Tables.RecordIndex.DeviceId}" TEXT NOT NULL,
			 	"{Tables.RecordIndex.StartIndex}" INTEGER NOT NULL,
			 	"{Tables.RecordIndex.EndIndex}" INTEGER NOT NULL,
			 	"{Tables.RecordIndex.Status}" TEXT CHECK("{Tables.RecordIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			 	
			 	CHECK ("{Tables.RecordIndex.StartIndex}" <= "{Tables.RecordIndex.EndIndex}"),
			 	
			 	PRIMARY KEY ("{Tables.RecordIndex.DeviceId}", "{Tables.RecordIndex.StartIndex}", "{Tables.RecordIndex.EndIndex}")
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.RecordIndex)}"
			 BEFORE UPDATE ON "{nameof(Tables.RecordIndex)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "prevent_overlapping_ranges_in_{nameof(Tables.RecordIndex)}"
			 BEFORE INSERT ON "{nameof(Tables.RecordIndex)}"
			 FOR EACH ROW
			 WHEN EXISTS (
			     SELECT 1
			     FROM "{nameof(Tables.RecordIndex)}" current
			     WHERE NEW."{Tables.RecordIndex.DeviceId}" = current."{Tables.RecordIndex.DeviceId}"
			       AND NEW."{Tables.RecordIndex.StartIndex}" <= current."{Tables.RecordIndex.EndIndex}"
			       AND NEW."{Tables.RecordIndex.EndIndex}"   >= current."{Tables.RecordIndex.StartIndex}"
			 )
			 BEGIN
			     SELECT RAISE(ABORT, '{nameof(Tables.RecordIndex)} ranges may not overlap.');
			 END;
			 """,
			connection);

		try {
			await createRecordIndexTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- GameIdSequence Table --------
		SqliteCommand createGlobalIdSequenceTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.GlobalIdSequence)}" (
			 	"{Tables.GlobalIdSequence.NextId}" INTEGER NOT NULL
			 );

			 INSERT INTO "{nameof(Tables.GlobalIdSequence)}" ("{Tables.GlobalIdSequence.NextId}")
			 VALUES (0);

			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.GlobalIdSequence)}"
			 BEFORE INSERT ON "{nameof(Tables.GlobalIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;

			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.GlobalIdSequence)}"
			 BEFORE DELETE ON "{nameof(Tables.GlobalIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await createGlobalIdSequenceTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- Games Table --------
		SqliteCommand createGamesTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.Games)}" (
			     "{Tables.Games.DeviceId}" TEXT NOT NULL,
			     "{Tables.Games.GameId}" INTEGER NOT NULL,
			     "{Tables.Games.TimePublished}" INTEGER NOT NULL,
			     "{Tables.Games.MajorVersion}" INTEGER NOT NULL,
			     "{Tables.Games.MinorVersion}" INTEGER NOT NULL,
			     "{Tables.Games.PatchVersion}" INTEGER NOT NULL,
			     "{Tables.Games.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.Games.GameId}", "{Tables.Games.DeviceId}")
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.Games)}"
			 BEFORE UPDATE ON "{nameof(Tables.Games)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await createGamesTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- EventData Table --------
		SqliteCommand createEventDataTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventData)}" (
			 	"{Tables.EventData.EventId}" INTEGER PRIMARY KEY,
			 	"{Tables.EventData.Data}" TEXT NOT NULL,
			 	"{Tables.EventData.Hash}" INTEGER NOT NULL,
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventData)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await createEventDataTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- EventMetaData Table --------
		SqliteCommand createEventMetaDataTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventMetaData)}" (
			 	"{Tables.EventMetaData.EventId}" INTEGER PRIMARY KEY,
			 	"{Tables.EventMetaData.PublishedByDeviceId}" TEXT NOT NULL,
			 	"{Tables.EventMetaData.TimePublished}" INTEGER NOT NULL,
			 	"{Tables.EventMetaData.ManuallyCreated}" BOOLEAN NOT NULL CHECK ("{Tables.EventMetaData.ManuallyCreated}" IN (0, 1))
			 	
			 	FOREIGN KEY "{Tables.EventMetaData.EventId}"
			 		REFERENCES "{nameof(Tables.EventData)}" ("{Tables.EventData.EventId}")
			 			ON UPDATE RESTRICT
			 			ON DELETE CASCADE
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventMetaData)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventMetaData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await createEventMetaDataTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- MatchData Table --------
		SqliteCommand createMatchDataTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchData)}" (
			 	"{Tables.MatchData.DeviceId}" TEXT NOT NULL,
			 	"{Tables.MatchData.MatchId}" INTEGER NOT NULL,
			 	"{Tables.MatchData.OriginalDeviceId}" TEXT NOT NULL,
			 	"{Tables.MatchData.OriginalMatchId}" INTEGER NOT NULL,
			 	"{Tables.MatchData.GameDeviceId}" TEXT NOT NULL,
			 	"{Tables.MatchData.GameId}" INTEGER NOT NULL,
			 	"{Tables.MatchData.Data}" TEXT NOT NULL,
			 	PRIMARY KEY ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}")
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.MatchData)}"
			 BEFORE UPDATE ON "{nameof(Tables.MatchData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table. Only Insert and Delete.');
			 END;
			 """,
			connection);

		try {
			await createMatchDataTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		// -------- EditGraphVertex Table --------
		SqliteCommand createEditGraphVerticesTable = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EditGraphVertices)}" (
			 	"{Tables.EditGraphVertices.ChildDeviceId}" TEXT NOT NULL,
			 	"{Tables.EditGraphVertices.ChildMatchId}" INTEGER NOT NULL,
			 	"{Tables.EditGraphVertices.ParentDeviceId}" TEXT NOT NULL,
			 	"{Tables.EditGraphVertices.ParentMatchId}" INTEGER NOT NULL,
			 	"{Tables.EditGraphVertices.OriginalDeviceId}" TEXT NOT NULL,
			 	"{Tables.EditGraphVertices.OriginalMatchId}" INTEGER NOT NULL,
			 	"{Tables.EditGraphVertices.Comment}" TEXT,
			 	
			 	PRIMARY KEY ("{Tables.EditGraphVertices.ChildDeviceId}", "{Tables.EditGraphVertices.ChildMatchId}, {Tables.EditGraphVertices.ParentDeviceId}", "{Tables.EditGraphVertices.ParentMatchId}"),
			 	
			 	FOREIGN KEY ("{Tables.EditGraphVertices.ChildDeviceId}", "{Tables.EditGraphVertices.ChildMatchId}")
			 		REFERENCES "{nameof(Tables.MatchData)}" ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}")
			 			ON UPDATE RESTRICT
			 			ON DELETE CASCADE,
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EditGraphVertices)}"
			 BEFORE UPDATE ON "{nameof(Tables.EditGraphVertices)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table. Only Insert and Delete.');
			 END;
			 """,
			connection);

		try {
			await createEditGraphVerticesTable.ExecuteNonQueryAsync();
		} catch {
			return false;
		}

		return true;
	}

	public static Task<bool> CheckIntegrity(SqliteConnection connection) {

		throw new NotImplementedException();
	}



	private async Task<AsyncTryResult<IndexRange, DataStoreError>> GetRangeByStart(string deviceId, long startIndex, RecordStatus status) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.RecordIndex)}"
			 WHERE "{Tables.RecordIndex.DeviceId}" = @DeviceId
			   AND "{Tables.RecordIndex.StartIndex}" = @StartIndex
			   AND "{Tables.RecordIndex.Status}" = '{nameof(RecordStatus.Stored)}'
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = startIndex });
		getIndexRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = status });

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<AsyncTryResult<IndexRange, DataStoreError>> GetRangeByEnd(string deviceId, long endIndex, RecordStatus status) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.RecordIndex)}"
			 WHERE "{Tables.RecordIndex.DeviceId}" = @DeviceId
			   AND "{Tables.RecordIndex.EndIndex}" = @EndIndex
			   AND "{Tables.RecordIndex.Status}" = '{nameof(RecordStatus.Stored)}'
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = endIndex });
		getIndexRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = status });

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<AsyncTryResult<IndexRange, DataStoreError>> GetRangeContaining(string deviceId, long index, RecordStatus status) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.RecordIndex)}"
			 WHERE "{Tables.RecordIndex.DeviceId}" = @DeviceId
			   AND "{Tables.RecordIndex.StartIndex}" <= @Index
			   AND "{Tables.RecordIndex.EndIndex}" >= @EndIndex
			   AND "{Tables.RecordIndex.Status}" = '{nameof(RecordStatus.Stored)}'
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@Index", SqliteType.Integer) { Value = index });
		getIndexRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = status });

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<DataStoreError?> AddRecordRange(string deviceId, IndexRange range) {

		SqliteCommand addRecordRange = new(
			$"""
			 INSERT INTO "{nameof(Tables.RecordIndex)}" (
			     "{Tables.RecordIndex.DeviceId}",
			     "{Tables.RecordIndex.StartIndex}",
			     "{Tables.RecordIndex.EndIndex}",
			     "{Tables.RecordIndex.Status}"
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
		addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.Status });

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError addRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(addRecordRangeError, Connection);
		}

		return null;
	}

	private async Task<DataStoreError?> DeleteRecordRange(string deviceId, IndexRange range) {

		SqliteCommand deleteRecordRange = new(
			$"""
			 DELETE FROM "{nameof(Tables.RecordIndex)}"
			 WHERE "{Tables.RecordIndex.DeviceId}" = @DeviceId
			   AND "{Tables.RecordIndex.StartIndex}" = @StartIndex
			   AND "{Tables.RecordIndex.EndIndex}" = @EndIndex
			   AND "{Tables.RecordIndex.Status}" = @Status;
			 """,
			Connection);

		deleteRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		deleteRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
		deleteRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
		deleteRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.Status });

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError deleteRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(deleteRecordRangeError, Connection);
		}

		return null;
	}



	private async Task<DataStoreError?> AddRecordToIndex(string deviceId, long index) {

		IndexRange? precedingRange = null;
		IndexRange? subsequentRange = null;

		if (index > 0) {

			AsyncTryResult<IndexRange, DataStoreError> result = await GetRangeByEnd(deviceId, index - 1, RecordStatus.Stored);
			if (result.IsFailure) {
				return result.Error;
			}

			precedingRange = result.Value;
		}

		if (index < long.MaxValue) {

			AsyncTryResult<IndexRange, DataStoreError> result = await GetRangeByStart(deviceId, index + 1, RecordStatus.Stored);
			if (result.IsFailure) {
				return result.Error;
			}

			subsequentRange = result.Value;
		}

		switch (precedingRange, subsequentRange) {

			case (not null, not null): {

				IndexRange newRange = new() {
					Start = precedingRange.Start,
					End = subsequentRange.End,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, precedingRange) is DataStoreError error1) {
					return error1;
				}

				if (await DeleteRecordRange(deviceId, subsequentRange) is DataStoreError error2) {
					return error2;
				}

				if (await AddRecordRange(deviceId, newRange) is DataStoreError error3) {
					return error3;
				}

				return null;
			}

			case (not null, null): {

				IndexRange newRange = new() {
					Start = precedingRange.Start,
					End = index,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, precedingRange) is DataStoreError error1) {
					return error1;
				}

				if (await AddRecordRange(deviceId, newRange) is DataStoreError error2) {
					return error2;
				}

				return null;
			}

			case (null, not null): {

				IndexRange newRange = new() {
					Start = index,
					End = subsequentRange.End,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, subsequentRange) is DataStoreError error1) {
					return error1;
				}

				if (await AddRecordRange(deviceId, newRange) is DataStoreError error2) {
					return error2;
				}

				return null;
			}

			case (null, null): {

				IndexRange newRange = new() {
					Start = index,
					End = index,
					Status = RecordStatus.Stored
				};

				if (await AddRecordRange(deviceId, newRange) is DataStoreError error) {
					return error;
				}

				return null;
			}
		}
	}

	private async Task<DataStoreError?> DeleteRecordFromIndex(string deviceId, long index) {

		AsyncTryResult<IndexRange, DataStoreError> result = await GetRangeContaining(deviceId, index, RecordStatus.Stored);
		if (result.IsFailure) {
			return result.Error;
		}

		IndexRange currentRange = result.Value;

		switch (index == currentRange.Start, index == currentRange.End) {

			case (false, false): {

				IndexRange lowerRange = new() {
					Start = currentRange.Start,
					End = index - 1,
					Status = RecordStatus.Stored
				};

				IndexRange upperRange = new() {
					Start = index + 1,
					End = currentRange.End,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, currentRange) is DataStoreError error1) {
					return error1;
				}

				if (await AddRecordRange(deviceId, lowerRange) is DataStoreError error2) {
					return error2;
				}

				if (await AddRecordRange(deviceId, upperRange) is DataStoreError error3) {
					return error3;
				}

				return null;
			}

			case (true, false): {

				IndexRange upperRange = new() {
					Start = index + 1,
					End = currentRange.End,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, currentRange) is DataStoreError error1) {
					return error1;
				}

				if (await AddRecordRange(deviceId, upperRange) is DataStoreError error2) {
					return error2;
				}

				return null;
			}

			case (false, true): {

				IndexRange lowerRange = new() {
					Start = currentRange.Start,
					End = index - 1,
					Status = RecordStatus.Stored
				};

				if (await DeleteRecordRange(deviceId, currentRange) is DataStoreError error1) {
					return error1;
				}

				if (await AddRecordRange(deviceId, lowerRange) is DataStoreError error2) {
					return error2;
				}

				return null;
			}

			case (true, true): {

				if (await DeleteRecordRange(deviceId, currentRange) is DataStoreError error) {
					return error;
				}

				return null;
			}
		}
	}





	public Task<GetGameSpecsResult> GetGameSpecs() {
		throw new NotImplementedException();
	}

	public Task<AddNewGameSpecResult> AddNewGameSpec() {
		throw new NotImplementedException();
	}

	public Task<ImportGameSpecResult> ImportGameSpec() {
		throw new NotImplementedException();
	}

	public Task<DeleteGameSpecResult> DeleteGameSpec() {
		throw new NotImplementedException();
	}



	public Task<GetEventsResult> GetEvents() {
		throw new NotImplementedException();
	}

	public Task<AddNewEventResult> AddNewEvent() {
		throw new NotImplementedException();
	}

	public Task<ImportEventResult> ImportEvent() {
		throw new NotImplementedException();
	}

	public Task<DeleteEventResult> DeleteEvent() {
		throw new NotImplementedException();
	}



	public Task<GetMatchDataFromGameResult> GetMatchDataFromGame(GameSpec game, bool ignoreMajorVersion = false, bool ignoreMinorVersion = true, bool ignorePatchVersion = true) {
		throw new NotImplementedException();
	}

	public async Task<AddNewMatchDataResult> AddNewMatchData(NewMatchDataDto newMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is DataStoreError openTransactionError) {
			return openTransactionError;
		}

		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"""SELECT "{Tables.GlobalIdSequence.NextId}" FROM "{nameof(Tables.GlobalIdSequence)}" WHERE ROWID = 1;""",
			Connection);

		AsyncTryValueResult<long, DataStoreError> result = await getMatchId.TryExecuteScalar<long>();
		if (result.IsFailure) {
			return await RollbackError.TryRollbackAndReturn(result.Error, Connection);
		}

		if (result.Value == long.MaxValue) {
			return new TableOverflowError();
		}

		long nextMatchId = result.Value + 1;

		// -------- Add Match Data --------
		string data = MatchDataToCsv.Serialize(newMatchDataDto.MatchData);

		SqliteCommand addMatchData = new(
			$"""
			 INSERT INTO "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}",
			     "{Tables.MatchData.MatchId}",
			     "{Tables.MatchData.OriginalDeviceId}",
			     "{Tables.MatchData.OriginalMatchId}",
			     "{Tables.MatchData.GameDeviceId}",
			     "{Tables.MatchData.GameId}",
			     "{Tables.MatchData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @NextMatchId,
			     @DeviceId,
			     @NextMatchId,
			     @GameDeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = newMatchDataDto.DeviceId });
		addMatchData.Parameters.Add(new("@NextMatchId", SqliteType.Integer) { Value = nextMatchId });
		addMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = newMatchDataDto.GameDeviceId });
		addMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = newMatchDataDto.GameId });
		addMatchData.Parameters.Add(new("@Data", SqliteType.Text) { Value = data });

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is DataStoreError addMatchDataError) {
			return await RollbackError.TryRollbackAndReturn(addMatchDataError, Connection);
		}

		// -------- Update Sequence Table --------
		SqliteCommand updateSequenceTable = new(
			$"""
			 UPDATE "{nameof(Tables.GlobalIdSequence)}"
			     SET "{Tables.GlobalIdSequence.NextId}" = "{Tables.GlobalIdSequence.NextId}" + 1
			     WHERE ROWID = 1;
			 """,
			Connection);

		if (await updateSequenceTable.ExecuteNonQueryAndExpect(1) is DataStoreError updateSequenceTableError) {
			return await RollbackError.TryRollbackAndReturn(updateSequenceTableError, Connection);
		}

		// -------- Update Record Index Table --------
		if (await AddRecordToIndex(newMatchDataDto.DeviceId, nextMatchId) is DataStoreError addRecordError) {
			return addRecordError;
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is DataStoreError commitError) {
			return await RollbackError.TryRollbackAndReturn(commitError, Connection);
		}

		return new Success();
	}

	public async Task<AddNewEditedMatchDataResult> AddNewEditedMatchData(NewEditedMatchDataDto newEditedMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is DataStoreError openTransactionError) {
			return openTransactionError;
		}

		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"""SELECT "{Tables.GlobalIdSequence.NextId}" FROM "{nameof(Tables.GlobalIdSequence)}" WHERE ROWID = 1;""",
			Connection);

		AsyncTryValueResult<long, DataStoreError> result = await getMatchId.TryExecuteScalar<long>();
		if (result.IsFailure) {
			return await RollbackError.TryRollbackAndReturn(result.Error, Connection);
		}

		if (result.Value == long.MaxValue) {
			return new TableOverflowError();
		}

		long nextMatchId = result.Value + 1;

		// -------- Add Match Data --------
		string data = MatchDataToCsv.Serialize(newEditedMatchDataDto.MatchData);

		SqliteCommand addMatchData = new(
			$"""
			 INSERT INTO "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}",
			     "{Tables.MatchData.MatchId}",
			     "{Tables.MatchData.OriginalDeviceId}",
			     "{Tables.MatchData.OriginalMatchId}",
			     "{Tables.MatchData.GameDeviceId}",
			     "{Tables.MatchData.GameId}",
			     "{Tables.MatchData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @NextMatchId,
			     @OriginalDeviceId,
			     @OriginalMatchId,
			     @GameDeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = newEditedMatchDataDto.DeviceId });
		addMatchData.Parameters.Add(new("@NextMatchId", SqliteType.Integer) { Value = nextMatchId });
		addMatchData.Parameters.Add(new("@OriginalDeviceId", SqliteType.Text) { Value = newEditedMatchDataDto.OriginalDeviceId });
		addMatchData.Parameters.Add(new("@OriginalMatchId", SqliteType.Integer) { Value = newEditedMatchDataDto.OriginalMatchId });
		addMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = newEditedMatchDataDto.GameDeviceId });
		addMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = newEditedMatchDataDto.GameId });
		addMatchData.Parameters.Add(new("@Data", SqliteType.Text) { Value = data });

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is DataStoreError addMatchDataError) {
			return await RollbackError.TryRollbackAndReturn(addMatchDataError, Connection);
		}

		// -------- Update Sequence Table --------
		SqliteCommand updateSequenceTable = new(
			$"""
			 UPDATE "{nameof(Tables.GlobalIdSequence)}"
			     SET "{Tables.GlobalIdSequence.NextId}" = "{Tables.GlobalIdSequence.NextId}" + 1
			     WHERE ROWID = 1;
			 """,
			Connection);

		if (await updateSequenceTable.ExecuteNonQueryAndExpect(1) is DataStoreError updateSequenceTableError) {
			return await RollbackError.TryRollbackAndReturn(updateSequenceTableError, Connection);
		}

		// -------- Update Record Index Table --------
		if (await AddRecordToIndex(newEditedMatchDataDto.DeviceId, nextMatchId) is DataStoreError addRecordError) {
			return addRecordError;
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is DataStoreError commitError) {
			return await RollbackError.TryRollbackAndReturn(commitError, Connection);
		}

		return new Success();
	}

	public async Task<ImportMatchDataResult> ImportMatchData(MatchDataDto importMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is DataStoreError openTransactionError) {
			return openTransactionError;
		}

		// -------- Add Match Data --------
		string data = MatchDataToCsv.Serialize(importMatchDataDto.MatchData);

		SqliteCommand addMatchData = new(
			$"""
			 INSERT INTO "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}",
			     "{Tables.MatchData.MatchId}",
			     "{Tables.MatchData.OriginalDeviceId}",
			     "{Tables.MatchData.OriginalMatchId}",
			     "{Tables.MatchData.GameDeviceId}",
			     "{Tables.MatchData.GameId}",
			     "{Tables.MatchData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @MatchId,
			     @OriginalDeviceId,
			     @OriginalMatchId,
			     @GameDeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = importMatchDataDto.DeviceId });
		addMatchData.Parameters.Add(new("@MatchId", SqliteType.Integer) { Value = importMatchDataDto.DeviceId });
		addMatchData.Parameters.Add(new("@OriginalDeviceId", SqliteType.Text) { Value = importMatchDataDto.OriginalDeviceId });
		addMatchData.Parameters.Add(new("@OriginalMatchId", SqliteType.Integer) { Value = importMatchDataDto.OriginalMatchId });
		addMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = importMatchDataDto.GameDeviceId });
		addMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = importMatchDataDto.GameId });
		addMatchData.Parameters.Add(new("@Data", SqliteType.Text) { Value = data });

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is DataStoreError addMatchDataError) {
			return await RollbackError.TryRollbackAndReturn(addMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		if (await AddRecordToIndex(importMatchDataDto.DeviceId, importMatchDataDto.MatchId) is DataStoreError addRecordError) {
			return addRecordError;
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is DataStoreError commitError) {
			return await RollbackError.TryRollbackAndReturn(commitError, Connection);
		}

		return new Success();
	}

	public async Task<DeleteMatchDataResult> DeleteMatchData(MatchDataDto matchDataToDelete) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is DataStoreError openTransactionError) {
			return openTransactionError;
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.OriginalDeviceId}" = @OriginalDeviceId
			   AND "{Tables.MatchData.OriginalMatchId}" = @OriginalMatchId;
			 """,
			Connection);

		deleteMatchData.Parameters.Add(new("@OriginalDeviceId", SqliteType.Text) { Value = matchDataToDelete.OriginalDeviceId });
		deleteMatchData.Parameters.Add(new("@OriginalMatchId", SqliteType.Integer) { Value = matchDataToDelete.OriginalMatchId });

		if (await deleteMatchData.ExecuteNonQueryAndExpect(1) is DataStoreError addMatchDataError) {
			return addMatchDataError;
		}

		// -------- Update Record Index Table --------
		if (await DeleteRecordFromIndex(matchDataToDelete.DeviceId, matchDataToDelete.MatchId) is DataStoreError addRecordError) {
			return addRecordError;
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is DataStoreError commitError) {
			return await RollbackError.TryRollbackAndReturn(commitError, Connection);
		}

		return new Success();
	}

	public Task<DeleteMatchDataResult> DeleteMatchDataFromEvent() {
		throw new NotImplementedException();
	}

	public Task<DeleteMatchDataResult> DeleteMatchDataFromGame() {
		throw new NotImplementedException();
	}

	public Task<DeleteAllMatchDataResult> DeleteAllMatchData() {
		throw new NotImplementedException();
	}



	public Task<GetLastScoutResult> GetLastScout() {
		throw new NotImplementedException();
	}

	public Task<SetLastScoutResult> SetLastScout(string scoutName) {
		throw new NotImplementedException();
	}


	public Task<List<GameSpec>> Old_GetGameSpecs() {

		IResult<GameSpec> result = GameSpec.Create(
			name: "Rebuilt",
			year: 2026,
			description: "",
			version: new(1, 0, 0),
			robotsPerAlliance: 3u,
			alliancesPerMatch: 2u,
			alliances: new List<AllianceColor> {
				new() { Color = Color.Red, Name = "Red Alliance" },
				new() { Color = Color.Blue, Name = "Blue Alliance" }
			}.ToReadOnly(),
			dataFields: new List<DataFieldSpec> {
				new SelectionDataFieldSpec {
					Name = "Outpost",
					Options = new List<string> { "Yes - Early", "Yes - Late", "No" }.ToReadOnly(),
					InitialValue = "No",
					RequiresValue = true
				},
				new SelectionDataFieldSpec {
					Name = "Depot",
					Options = new List<string> { "Yes - Early", "Yes - Late", "No" }.ToReadOnly(),
					InitialValue = "No",
					RequiresValue = true
				},
				new IntegerDataFieldSpec { Name = "Mid Trips", InitialValue = 0, MinValue = 0, MaxValue = 15 },
				new BooleanDataFieldSpec { Name = "Scored Preload", InitialValue = false },
				new SelectionDataFieldSpec {
					Name = "Auto Climb",
					Options = new List<string> { "Yes", "Attempted", "No" }.ToReadOnly(),
					InitialValue = "No",
					RequiresValue = true
				},
				new SelectionDataFieldSpec {
					Name = "Primary Role",
					Options = new List<string> { "Passing", "Scoring", "Defending" }.ToReadOnly(),
					InitialValue = "",
					RequiresValue = true
				},
				//new MultiIntegerDataFieldSpec { Name = "Fuel", InitialValue = 0, MinValue = 0, MaxValue = 1000 },
				new BooleanDataFieldSpec { Name = "Passing", InitialValue = false },
				new BooleanDataFieldSpec { Name = "Scoring", InitialValue = false },
				new SelectionDataFieldSpec {
					Name = "Defending",
					Options = new List<string> { "None", "Ramp", "Contact", "Ramp + Contact", "Idle" }.ToReadOnly(),
					InitialValue = "None",
					RequiresValue = true
				},
				new BooleanDataFieldSpec { Name = "Shoveled", InitialValue = false },
				new SelectionDataFieldSpec {
					Name = "Accuracy",
					Options = new List<string> { "<60", "70", "80", "90", "99" }.ToReadOnly(),
					InitialValue = "",
					RequiresValue = false
				},
				new SelectionDataFieldSpec {
					Name = "Aimlessness",
					Options = new List<string> { "0", "25", "50", "75", "100" }.ToReadOnly(),
					InitialValue = "0",
					RequiresValue = false
				},
				new SelectionDataFieldSpec {
					Name = "Effectiveness",
					Options = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }.ToReadOnly(),
					InitialValue = "",
					RequiresValue = true
				},
				new BooleanDataFieldSpec { Name = "Beached", InitialValue = false },
				new SelectionDataFieldSpec {
					Name = "Climb",
					Options = new List<string> { "None", " L1", "L2", "L3" }.ToReadOnly(),
					InitialValue = "None",
					RequiresValue = true
				},
				new SelectionDataFieldSpec {
					Name = "Disconnected",
					Options = new List<string> { "0", "25", "50", "75", "100" }.ToReadOnly(),
					InitialValue = "0",
					RequiresValue = true
				},
				new TextDataFieldSpec {
					Name = "Comments",
					InitialValue = "",
					MustNotBeEmpty = false,
					MustNotBeInitialValue = false
				}
			}.ToReadOnly(),
			setupTabInputs: new List<InputSpec>().ToReadOnly(),
			autoTabInputs: new List<InputSpec> {
				new() { DataFieldName = "Outpost", Label = "Outpost" },
				new() { DataFieldName = "Depot", Label = "Depot" },
				new() { DataFieldName = "Mid Trips", Label = "Mid Trips" },
				new() { DataFieldName = "Scored Preload", Label = "Score Preload?" },
				new() { DataFieldName = "Auto Climb", Label = "L1 Climb?" }
			}.ToReadOnly(),
			teleTabInputs: new List<InputSpec> {
				//new() { DataFieldName = "Fuel", Label = "Fuel Scored" },
				new() { DataFieldName = "Primary Role", Label = "Primary Role" },
				new() { DataFieldName = "Passing", Label = "Passing?" },
				new() { DataFieldName = "Scoring", Label = "Scoring?" },
				new() { DataFieldName = "Defending", Label = "Defense" },
				new() { DataFieldName = "Accuracy", Label = "Accuracy %" },
				new() { DataFieldName = "Aimlessness", Label = "Aimlessness %" },
				new() { DataFieldName = "Effectiveness", Label = "Role Effectiveness" },
				new() { DataFieldName = "Shoveled", Label = "Shoveled Fuel?" },
				new() { DataFieldName = "Beached", Label = "Beached Multiple Times?" }
			}.ToReadOnly(),
			endgameTabInputs: new List<InputSpec> {
				new() { DataFieldName = "Climb", Label = "Climb" },
				new() { DataFieldName = "Disconnected", Label = "Disconnected %" },
				new() { DataFieldName = "Comments", Label = "Extra Comments" }
			}.ToReadOnly());


		if (result is not IResult<GameSpec>.Success success) {
			throw new("Game specification was not successfully produced.");
		}

		return Task.FromResult(new List<GameSpec> {
			success.Value
		});
	}

	public async Task<GetMatchDataFromGameResult> Old_GetMatchData() {

		SqliteCommand getMatchDataCommand = new(
			$"SELECT * FROM \"{nameof(Tables.MatchData)}\";",
			Connection);

		SqliteDataReader reader;
		try {
			reader = await getMatchDataCommand.ExecuteReaderAsync();
		} catch (Exception exception) {
			return exception;
		}

		GameSpec gameSpec = (await GetGameSpecs()).FirstOrDefault() ?? throw new UnreachableException(); // todo

		List<ImportMatchDataDto> allMatchDtos = [];
		while (reader.Read()) {

			string deviceId = reader.GetString(0);
			int recordId = reader.GetInt32(1);
			string serializedMatch = reader.GetString(2);
			string? editOfDeviceId = reader[3] is DBNull ? null : reader.GetString(3);
			int? editOfRecordId = reader[4] is DBNull ? null : reader.GetInt32(4);

			MatchDataDeserializationResult result = MatchDataToCsv.Deserialize(serializedMatch, gameSpec);

			if (result.IsT1) {
				return result.AsT1;
			}

			MatchData data = result.AsT0;

			switch (editOfDeviceId, editOfRecordId) {

				case ({ } originatingDeviceId, { } originalRecordId):
					allMatchDtos.Add(new() {
						MatchData = data,
						DeviceId = deviceId,
						RecordId = recordId,
						EditBasedOn = (originatingDeviceId, originalRecordId)
					});
					break;

				case (null, null):
					allMatchDtos.Add(new() {
						MatchData = data,
						DeviceId = deviceId,
						RecordId = recordId,
						EditBasedOn = null
					});
					break;

				case ({ } originatingDeviceId, null):
					return new InvalidEditIdsError(originatingDeviceId);

				case (null, { } originalRecordId):
					return new InvalidEditIdsError(originalRecordId);
			}
		}

		// Since editing match data isn't implemented yet and there is no conflict resolution implemented editing
		// matches won't work and the below code is moot. Instead, just return the original match data.
		//return allMatchDtos.Where(x => x.EditBasedOn is null).ToList();

		// Identify all the match data that are original (not edits of existing match data).
		// Create an "Edit Chain" for each original match (starting with the original match itself).
		List<List<ImportMatchDataDto>> editChains = allMatchDtos
			.Where(x => x.EditBasedOn is null)
			.Select(x => new List<ImportMatchDataDto> { x })
			.ToList();

		// Iterate over all match data that is an edit of prior match data.
		// Ensure that all edits either directly or transitively (through one or more other edit match data records) point to original match data.
		// The current implementation of this relies on lower degree edits being earlier in the list than higher degree edit.
		// This order is not guaranteed but seems to be working, possibly because no one is actually editing data.
		// A first degree edit is an edit of the original data, a second degree edit is an edit of a first degree edit, etc.
		// This implementation also doesn't work with things like edit trees.

		List<ImportMatchDataDto> unlinkedEditData = allMatchDtos.Where(x => x.EditBasedOn is not null).ToList();
		int lastCountOfUnlinkedEditData = unlinkedEditData.Count;
		while (true) {

			foreach (ImportMatchDataDto editData in unlinkedEditData) {

				List<ImportMatchDataDto>? activeEditChain = editChains.FirstOrDefault(x =>
					x.Count > 0 && // should be guaranteed
					(x.Last().DeviceId, x.Last().RecordId) == editData.EditBasedOn);

				// The active edit chain will be null if the edit data is part of an edit branch that was not chosen.
				activeEditChain?.Add(editData);
			}

			// If all the edit data has a home or if the remaining edit paths are not part of the branch that has been chosen, exit.
			// If the edit history of a match has branched only pick on branch and ignore the edit data from the other branches.
			// Whichever branch is returned by the database first will be chosen.
			if (unlinkedEditData.Count == 0 || lastCountOfUnlinkedEditData == unlinkedEditData.Count) {
				break;
			}
		}

		return editChains.Select(x => x.Last()).ToList();
	}


	public async Task<bool> Old_DeleteAllMatchData() {

		SqliteCommand deleteMatchDataCommand = new(
			$"""
			 BEGIN TRANSACTION;
			 DELETE FROM "{nameof(Tables.MatchData)}";
			 COMMIT;
			 """,
			Connection);

		try {
			await deleteMatchDataCommand.ExecuteNonQueryAsync();

		} catch {
			return false;
		}

		return true;
	}

	public async Task<string?> Old_GetLastScout() {

		SqliteCommand command = new(
			$"SELECT \"{Tables.Scout.Name}\" FROM \"{nameof(Tables.Scout)}\" WHERE ROWID = 1;",
			Connection
		);

		try {
			object? result = await command.ExecuteScalarAsync();

			return result as string;

		} catch {
			return null;
		}
	}

	public async Task<bool> Old_SetLastScout(string scoutName) {

		// TODO better SQL sanitization (here and above)
		if (scoutName.Contains('\'')) {
			scoutName = scoutName.Replace("'", "''");
		}

		SqliteCommand command = new() {
			CommandText =
				$"""
				 INSERT OR REPLACE INTO "{nameof(Tables.Scout)}" (ROWID, "{Tables.Scout.Name}")
				 VALUES (1, '{scoutName}');
				 """,
			Connection = Connection
		};

		try {
			int result = await command.ExecuteNonQueryAsync();
			return result == 1;

		} catch {
			return false;
		}
	}

}

// todo: enable the game maker to define migrations from one version of a game to another