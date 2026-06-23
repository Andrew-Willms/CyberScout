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

	private enum EventDataSources {
		TheBlueAlliance,
		Manual
	}

	private static class Tables {

		// -------- General --------

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

		// -------- Game --------

		public static class GameIdSequence {
			public const string LastUsedId = "LastUsedId";
		}

		public static class GameIndex {
			public const string DeviceId = "DeviceId";
			public const string StartIndex = "StartIndex";
			public const string EndIndex = "EndIndex";
			public const string Status = "Status";
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

		// -------- Event --------

		public static class EventIdSequence {
			public const string LastUsedId = "LastUsedId";
		}

		public static class EventIndex {
			public const string DeviceId = "DeviceId";
			public const string StartIndex = "StartIndex";
			public const string EndIndex = "EndIndex";
			public const string Status = "Status";
			public const string GameDeviceId = "GameDeviceId";
			public const string GameId = "GameRecordId";
		}

		// Every device with an internet connection will likely create an event from TBA and then will share this event to other devices.
		// This will result in a decent number of records being shared with little purpose. However, each even should only be about the same
		// amount of data as a match. I don't think it will meaningfully slow things down, and it's very convenient to treat everything the same.
		public static class EventMetaData {
			public const string DeviceId = "DeviceId";
			public const string EventMetaDataId = "EventMetaDataId";
			public const string EventDataId = "EventDataId";
			public const string TimePublished = "TimePublished";
			public const string Source = "Source";
		}

		public static class EventData {
			public const string EventDataId = "EventDataId";
			public const string Data = "Data";
		}

		// -------- Match --------

		public static class MatchIdSequence {
			public const string LastUsedId = "LastUsedId";
		}

		public static class MatchIndex {
			public const string DeviceId = "DeviceId";
			public const string StartIndex = "StartIndex";
			public const string EndIndex = "EndIndex";
			public const string Status = "Status";
			public const string GameDeviceId = "GameDeviceId";
			public const string GameId = "GameId";
			public const string EventDataId = "EventDataId";
		}

		public static class MatchData {
			public const string DeviceId = "DeviceId";
			public const string MatchId = "MatchId";

			// OriginalDeviceId and OriginalRecordId are not foreign keys because I do not want to require
			// a device to have the original match data in order to have descendant match data.
			public const string OriginalDeviceId = "OriginalDeviceId";
			public const string OriginalMatchId = "OriginalMatchId";

			public const string GameDeviceId = "GameDeviceId";
			public const string GameRecordId = "GameRecordId";
			public const string EventDeviceId = "EventDeviceId";
			public const string EventRecordId = "EventRecordId";
			public const string Data = "Data";
		}

		public static class EditGraphVertices {
			public const string ChildDeviceId = "ChildDeviceId";
			public const string ChildRecordId = "ChildRecordId";

			// ParentDeviceId and ParentRecordId are not foreign keys because I do not want to require
			// a device to have the parent match data in order to have descendant match data.
			public const string ParentDeviceId = "ParentDeviceId";
			public const string ParentRecordId = "ParentRecordId";

			// OriginalDeviceId and OriginalRecordId are not foreign keys because I do not want to require
			// a device to have the original match data in order to have descendant match data.
			public const string OriginalDeviceId = "OriginalDeviceId";
			public const string OriginalRecordId = "OriginalRecordId";

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

		if (isEmpty is true && await Create(connection) is not null) {
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

	private static async Task<DataStoreError?> Create(SqliteConnection connection) {

		if (await CreateDatabaseVersionTable(connection) is DataStoreError databaseVersionError) {
			return databaseVersionError;
		}

		if (await CreateScoutTable(connection) is DataStoreError scoutError) {
			return scoutError;
		}

		if (await CreateKnownDevicesTable(connection) is DataStoreError knownDevicesError) {
			return knownDevicesError;
		}

		if (await CreateGameIdSequenceTable(connection) is DataStoreError gameIdSequenceError) {
			return gameIdSequenceError;
		}

		if (await CreateGameIndexTable(connection) is DataStoreError gameIndexError) {
			return gameIndexError;
		}

		if (await CreateGamesTable(connection) is DataStoreError gamesError) {
			return gamesError;
		}

		if (await CreateEventIdSequenceTable(connection) is DataStoreError eventIdSequenceError) {
			return eventIdSequenceError;
		}

		if (await CreateEventIndexTable(connection) is DataStoreError eventIndexError) {
			return eventIndexError;
		}

		if (await CreateEventMetaDataTable(connection) is DataStoreError eventMetaDataError) {
			return eventMetaDataError;
		}

		if (await CreateEventDataTable(connection) is DataStoreError eventDataError) {
			return eventDataError;
		}

		if (await CreateMatchIdSequenceTable(connection) is DataStoreError matchIdSequenceError) {
			return matchIdSequenceError;
		}

		if (await CreateMatchIndexTable(connection) is DataStoreError matchIndexError) {
			return matchIndexError;
		}

		if (await CreateMatchDataTable(connection) is DataStoreError matchDataError) {
			return matchDataError;
		}

		if (await CreateEditGraphVerticesTable(connection) is DataStoreError editGraphVerticesError) {
			return editGraphVerticesError;
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateDatabaseVersionTable(SqliteConnection connection) {

		SqliteCommand command = new(
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
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateScoutTable(SqliteConnection connection) {

		SqliteCommand command = new(
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
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateKnownDevicesTable(SqliteConnection connection) {

		SqliteCommand command = new(
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
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateGameIdSequenceTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.GameIdSequence)}" (
			     "{Tables.GameIdSequence.LastUsedId}" INTEGER NOT NULL
			 );

			 INSERT INTO "{nameof(Tables.GameIdSequence)}" ("{Tables.GameIdSequence.LastUsedId}")
			 VALUES (-1);

			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.GameIdSequence)}"
			 BEFORE INSERT ON "{nameof(Tables.GameIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;

			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.GameIdSequence)}"
			 BEFORE DELETE ON "{nameof(Tables.GameIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateGameIndexTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.GameIndex)}" (
			     "{Tables.GameIndex.DeviceId}" TEXT NOT NULL,
			     "{Tables.GameIndex.StartIndex}" INTEGER NOT NULL,
			     "{Tables.GameIndex.EndIndex}" INTEGER NOT NULL,
			     "{Tables.GameIndex.Status}" TEXT CHECK("{Tables.GameIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			     
			     CHECK ("{Tables.GameIndex.StartIndex}" <= "{Tables.GameIndex.EndIndex}"),
			     
			     PRIMARY KEY ("{Tables.GameIndex.DeviceId}", "{Tables.GameIndex.StartIndex}", "{Tables.GameIndex.EndIndex}"),
			     
			     FOREIGN KEY "{Tables.GameIndex.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.GameIndex)}"
			 BEFORE UPDATE ON "{nameof(Tables.GameIndex)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "prevent_overlapping_ranges_in_{nameof(Tables.GameIndex)}"
			 BEFORE INSERT ON "{nameof(Tables.GameIndex)}"
			 FOR EACH ROW
			 WHEN EXISTS (
			     SELECT 1
			     FROM "{nameof(Tables.GameIndex)}" current
			     WHERE NEW."{Tables.GameIndex.DeviceId}" = current."{Tables.GameIndex.DeviceId}"
			       AND NEW."{Tables.GameIndex.StartIndex}" <= current."{Tables.GameIndex.EndIndex}"
			       AND NEW."{Tables.GameIndex.EndIndex}"   >= current."{Tables.GameIndex.StartIndex}"
			 )
			 BEGIN
			     SELECT RAISE(ABORT, '{nameof(Tables.GameIndex)} ranges may not overlap.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateGamesTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.Games)}" (
			     "{Tables.Games.DeviceId}" TEXT NOT NULL,
			     "{Tables.Games.GameId}" INTEGER NOT NULL,
			     "{Tables.Games.TimePublished}" INTEGER NOT NULL,
			     "{Tables.Games.MajorVersion}" INTEGER NOT NULL,
			     "{Tables.Games.MinorVersion}" INTEGER NOT NULL,
			     "{Tables.Games.PatchVersion}" INTEGER NOT NULL,
			     "{Tables.Games.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.Games.DeviceId}", "{Tables.Games.GameId}"),
			     
			     FOREIGN KEY "{Tables.Games.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.Games)}"
			 BEFORE UPDATE ON "{nameof(Tables.Games)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateEventIdSequenceTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventIdSequence)}" (
			     "{Tables.EventIdSequence.LastUsedId}" INTEGER NOT NULL
			 );

			 INSERT INTO "{nameof(Tables.EventIdSequence)}" ("{Tables.EventIdSequence.LastUsedId}")
			 VALUES (-1);

			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.EventIdSequence)}"
			 BEFORE INSERT ON "{nameof(Tables.EventIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;

			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.EventIdSequence)}"
			 BEFORE DELETE ON "{nameof(Tables.EventIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateEventIndexTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventIndex)}" (
			     "{Tables.EventIndex.DeviceId}" TEXT NOT NULL,
			     "{Tables.EventIndex.StartIndex}" INTEGER NOT NULL,
			     "{Tables.EventIndex.EndIndex}" INTEGER NOT NULL,
			     "{Tables.EventIndex.Status}" TEXT CHECK("{Tables.EventIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			     "{Tables.EventIndex.GameDeviceId}" TEXT NOT NULL,
			     "{Tables.EventIndex.GameId}" INTEGER NOT NULL,
			     
			     CHECK ("{Tables.EventIndex.StartIndex}" <= "{Tables.EventIndex.EndIndex}"),
			     
			     PRIMARY KEY ("{Tables.EventIndex.DeviceId}", "{Tables.EventIndex.StartIndex}", "{Tables.EventIndex.EndIndex}"),
			     
			     FOREIGN KEY "{Tables.EventIndex.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			     
			     FOREIGN KEY ("{Tables.EventIndex.GameDeviceId}", "{Tables.EventIndex.GameId}")
			         REFERENCES "{nameof(Tables.Games)}" ("{Tables.Games.DeviceId}", "{Tables.Games.GameId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventIndex)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventIndex)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "prevent_overlapping_ranges_in_{nameof(Tables.EventIndex)}"
			 BEFORE INSERT ON "{nameof(Tables.EventIndex)}"
			 FOR EACH ROW
			 WHEN EXISTS (
			     SELECT 1
			     FROM "{nameof(Tables.EventIndex)}" current
			     WHERE NEW."{Tables.EventIndex.DeviceId}" = current."{Tables.EventIndex.DeviceId}"
			       AND NEW."{Tables.EventIndex.StartIndex}" <= current."{Tables.EventIndex.EndIndex}"
			       AND NEW."{Tables.EventIndex.EndIndex}"   >= current."{Tables.EventIndex.StartIndex}"
			 )
			 BEGIN
			     SELECT RAISE(ABORT, '{nameof(Tables.EventIndex)} ranges may not overlap.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateEventMetaDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventMetaData)}" (
			     "{Tables.EventMetaData.DeviceId}" TEXT NOT NULL,
			     "{Tables.EventMetaData.EventMetaDataId}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.EventDataId}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.TimePublished}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.Source}" TEXT NOT NULL CHECK ("{Tables.EventMetaData.Source}" IN ('{nameof(EventDataSources.TheBlueAlliance)}', '{EventDataSources.Manual}'))
			     
			     PRIMARY KEY ("{Tables.EventMetaData.DeviceId}", "{Tables.EventMetaData.EventMetaDataId}"),
			     
			     FOREIGN KEY "{Tables.EventMetaData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 	
			     FOREIGN KEY "{Tables.EventMetaData.EventDataId}"
			         REFERENCES "{nameof(Tables.EventData)}" ("{Tables.EventData.EventDataId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventMetaData)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventMetaData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateEventDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventData)}" (
			     "{Tables.EventData.EventDataId}" INTEGER PRIMARY KEY,
			     "{Tables.EventData.Data}" TEXT NOT NULL
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventData)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateMatchIdSequenceTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchIdSequence)}" (
			     "{Tables.MatchIdSequence.LastUsedId}" INTEGER NOT NULL
			 );

			 INSERT INTO "{nameof(Tables.MatchIdSequence)}" ("{Tables.MatchIdSequence.LastUsedId}")
			 VALUES (-1);

			 CREATE TRIGGER IF NOT EXISTS "block_inserts_on_{nameof(Tables.MatchIdSequence)}"
			 BEFORE INSERT ON "{nameof(Tables.MatchIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Inserts are not allowed on this table; only updates.');
			 END;

			 CREATE TRIGGER IF NOT EXISTS "block_deletes_on_{nameof(Tables.MatchIdSequence)}"
			 BEFORE DELETE ON "{nameof(Tables.MatchIdSequence)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Deletes are not allowed on this table; only updates.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateMatchIndexTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchIndex)}" (
			     "{Tables.MatchIndex.DeviceId}" TEXT NOT NULL,
			     "{Tables.MatchIndex.StartIndex}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.EndIndex}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.Status}" TEXT CHECK("{Tables.MatchIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			     "{Tables.MatchIndex.GameDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchIndex.GameId}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.EventDataId}" INTEGER NOT NULL,
			     
			     CHECK ("{Tables.MatchIndex.StartIndex}" <= "{Tables.MatchIndex.EndIndex}"),
			     
			     PRIMARY KEY ("{Tables.MatchIndex.DeviceId}", "{Tables.MatchIndex.StartIndex}", "{Tables.MatchIndex.EndIndex}"),
			     
			     FOREIGN KEY "{Tables.MatchIndex.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			     
			     FOREIGN KEY "{Tables.MatchIndex.EventDataId}"
			         REFERENCES "{nameof(Tables.EventData)}" "{Tables.EventData.EventDataId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 	
			     FOREIGN KEY ("{Tables.MatchIndex.GameDeviceId}", "{Tables.MatchIndex.GameId}")
			         REFERENCES "{nameof(Tables.Games)}" ("{Tables.Games.DeviceId}", "{Tables.Games.GameId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 );
			 
			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.MatchIndex)}"
			 BEFORE UPDATE ON "{nameof(Tables.MatchIndex)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "prevent_overlapping_ranges_in_{nameof(Tables.MatchIndex)}"
			 BEFORE INSERT ON "{nameof(Tables.MatchIndex)}"
			 FOR EACH ROW
			 WHEN EXISTS (
			     SELECT 1
			     FROM "{nameof(Tables.MatchIndex)}" current
			     WHERE NEW."{Tables.MatchIndex.DeviceId}" = current."{Tables.MatchIndex.DeviceId}"
			       AND NEW."{Tables.MatchIndex.StartIndex}" <= current."{Tables.MatchIndex.EndIndex}"
			       AND NEW."{Tables.MatchIndex.EndIndex}"   >= current."{Tables.MatchIndex.StartIndex}"
			 )
			 BEGIN
			     SELECT RAISE(ABORT, '{nameof(Tables.MatchIndex)} ranges may not overlap.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateMatchDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.MatchId}" INTEGER NOT NULL,
			     "{Tables.MatchData.OriginalDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.OriginalMatchId}" INTEGER NOT NULL,
			     "{Tables.MatchData.GameDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.GameRecordId}" INTEGER NOT NULL,
			     "{Tables.MatchData.EventDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.EventRecordId}" INTEGER NOT NULL,
			     "{Tables.MatchData.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}"),
			     
			     FOREIGN KEY "{Tables.MatchData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			     
			     FOREIGN KEY ("{Tables.MatchData.GameDeviceId}", "{Tables.MatchData.GameRecordId}")
			         REFERENCES "{nameof(Tables.Games)}" ("{Tables.Games.DeviceId}", "{Tables.Games.GameId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 		     
			     FOREIGN KEY ("{Tables.MatchData.EventDeviceId}", "{Tables.MatchData.EventRecordId}")
			         REFERENCES "{nameof(Tables.EventMetaData)}" ("{Tables.EventMetaData.DeviceId}", "{Tables.EventMetaData.EventMetaDataId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.MatchData)}"
			 BEFORE UPDATE ON "{nameof(Tables.MatchData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table. Only Insert and Delete.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	private static async Task<DataStoreError?> CreateEditGraphVerticesTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EditGraphVertices)}" (
			     "{Tables.EditGraphVertices.ChildDeviceId}" TEXT NOT NULL,
			     "{Tables.EditGraphVertices.ChildRecordId}" INTEGER NOT NULL,
			     "{Tables.EditGraphVertices.ParentDeviceId}" TEXT NOT NULL,
			     "{Tables.EditGraphVertices.ParentRecordId}" INTEGER NOT NULL,
			     "{Tables.EditGraphVertices.OriginalDeviceId}" TEXT NOT NULL,
			     "{Tables.EditGraphVertices.OriginalRecordId}" INTEGER NOT NULL,
			     "{Tables.EditGraphVertices.Comment}" TEXT,
			     
			     PRIMARY KEY ("{Tables.EditGraphVertices.ChildDeviceId}", "{Tables.EditGraphVertices.ChildRecordId}, {Tables.EditGraphVertices.ParentDeviceId}", "{Tables.EditGraphVertices.ParentRecordId}"),
			     
			     FOREIGN KEY ("{Tables.EditGraphVertices.ChildDeviceId}", "{Tables.EditGraphVertices.ChildRecordId}")
			         REFERENCES "{nameof(Tables.MatchData)}" ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}")
			             ON UPDATE RESTRICT
			             ON DELETE CASCADE
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EditGraphVertices)}"
			 BEFORE UPDATE ON "{nameof(Tables.EditGraphVertices)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table. Only Insert and Delete.');
			 END;
			 """,
			connection);

		try {
			await command.ExecuteNonQueryAndExpect(0);
		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command);
		}

		return null;
	}

	public static Task<bool> CheckIntegrity(SqliteConnection connection) {

		throw new NotImplementedException();
	}



	private async Task<AsyncTryResult<IndexRange, DataStoreError>> GetRangeByStart(string deviceId, long startIndex, RecordStatus status) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" = @StartIndex
			   AND "{Tables.MatchIndex.Status}" = '{nameof(RecordStatus.Stored)}'
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
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.EndIndex}" = @EndIndex
			   AND "{Tables.MatchIndex.Status}" = '{nameof(RecordStatus.Stored)}'
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
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" <= @Index
			   AND "{Tables.MatchIndex.EndIndex}" >= @EndIndex
			   AND "{Tables.MatchIndex.Status}" = '{nameof(RecordStatus.Stored)}'
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
			 INSERT INTO "{nameof(Tables.MatchIndex)}" (
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
		addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.Status });

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is DataStoreError addRecordRangeError) {
			return await RollbackError.TryRollbackAndReturn(addRecordRangeError, Connection);
		}

		return null;
	}

	private async Task<DataStoreError?> DeleteRecordRange(string deviceId, IndexRange range) {

		SqliteCommand deleteRecordRange = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" = @StartIndex
			   AND "{Tables.MatchIndex.EndIndex}" = @EndIndex
			   AND "{Tables.MatchIndex.Status}" = @Status;
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
			$"""SELECT "{Tables.MatchIdSequence.LastUsedId}" FROM "{nameof(Tables.MatchIdSequence)}" WHERE ROWID = 1;""",
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
			     "{Tables.MatchData.GameRecordId}",
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
			 UPDATE "{nameof(Tables.MatchIdSequence)}"
			     SET "{Tables.MatchIdSequence.LastUsedId}" = "{Tables.MatchIdSequence.LastUsedId}"
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
			$"""SELECT "{Tables.MatchIdSequence.LastUsedId}" FROM "{nameof(Tables.MatchIdSequence)}" WHERE ROWID = 1;""",
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
			     "{Tables.MatchData.GameRecordId}",
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
			 UPDATE "{nameof(Tables.MatchIdSequence)}"
			     SET "{Tables.MatchIdSequence.LastUsedId}" = "{Tables.MatchIdSequence.LastUsedId}"
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
			     "{Tables.MatchData.GameRecordId}",
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

	public async Task<DeleteMatchDataResult> DeleteMatchDataFromGame() {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is DataStoreError openTransactionError) {
			return openTransactionError;
		}
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