using Comms.Dtos;
using Comms.Serialization;
using Database.Results;
using Database.Results.Event;
using Database.Results.GameSpec;
using Database.Results.MatchData;
using Database.Results.Scout;
using Database.Sqlite.Indexer;
using Microsoft.Data.Sqlite;
using SqliteUtilities;

namespace Database.Sqlite;



public class SqliteDataStoreVersion1Creator : IDataStoreCreator {

	public async Task<IDataStore?> Create(string settings) {

		return await SqliteDataStoreVersion1.Initialize(settings);
	}
}



public class SqliteDataStoreVersion1 : IDataStore {

	private const uint TargetDatabaseVersion = 1;

	private readonly SqliteConnection Connection;

	private readonly SqliteIndexerVersion1 Indexer;



	private SqliteDataStoreVersion1(SqliteConnection connection) {
		Connection = connection;
		Indexer = new(connection);
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

		if (isEmpty is true && (await Create(connection)).IsFailure) {
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

	// TODO: Consider wrapping internal errors as CreateDataBaseError if this function gets more complicated.
	private static async Task<CreateTableResult> Create(SqliteConnection connection) {

		CreateTableResult result = await CreateDatabaseVersionTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateScoutTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateKnownDevicesTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateGameIdSequenceTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateGameIndexTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateGamesTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateEventIdSequenceTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateEventIndexTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateEventMetaDataTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateEventDataTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateMatchIdSequenceTable(connection);
		if (result.IsFailure) {
			return result;
		}

		result = await CreateMatchIndexTable(connection);
		if (result.IsFailure) {
			return result;
		}

		return await CreateMatchDataTable(connection);
	}

	private static async Task<CreateTableResult> CreateDatabaseVersionTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateScoutTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateKnownDevicesTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateGameIdSequenceTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateGameIndexTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateGamesTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateEventIdSequenceTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateEventIndexTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventIndex)}" (
			     "{Tables.EventIndex.DeviceId}" TEXT NOT NULL,
			     "{Tables.EventIndex.StartIndex}" INTEGER NOT NULL,
			     "{Tables.EventIndex.EndIndex}" INTEGER NOT NULL,
			     "{Tables.EventIndex.Status}" TEXT CHECK("{Tables.EventIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			     
			     CHECK ("{Tables.EventIndex.StartIndex}" <= "{Tables.EventIndex.EndIndex}"),
			     
			     PRIMARY KEY ("{Tables.EventIndex.DeviceId}", "{Tables.EventIndex.StartIndex}", "{Tables.EventIndex.EndIndex}"),
			     
			     FOREIGN KEY "{Tables.EventIndex.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateEventMetaDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventMetaData)}" (
			     "{Tables.EventMetaData.DeviceId}" TEXT NOT NULL,
			     "{Tables.EventMetaData.MetaDataId}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.DataId}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.TimePublished}" INTEGER NOT NULL,
			     "{Tables.EventMetaData.Source}" TEXT NOT NULL CHECK ("{Tables.EventMetaData.Source}" IN ('{nameof(EventDataSources.TheBlueAlliance)}', '{EventDataSources.Manual}'))
			     
			     PRIMARY KEY ("{Tables.EventMetaData.DeviceId}", "{Tables.EventMetaData.MetaDataId}"),
			     
			     FOREIGN KEY "{Tables.EventMetaData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 	
			     FOREIGN KEY "{Tables.EventMetaData.DataId}"
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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateEventDataTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateMatchIdSequenceTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateMatchIndexTable(SqliteConnection connection) {

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

		return await command.ExecuteNonQueryAndExpect(0);
	}

	private static async Task<CreateTableResult> CreateMatchDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.MatchId}" INTEGER NOT NULL,
			     "{Tables.MatchData.OriginalDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.OriginalMatchId}" INTEGER NOT NULL,
			     "{Tables.MatchData.ParentsAsText}" TEXT,
			     "{Tables.MatchData.GameDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.GameId}" INTEGER NOT NULL,
			     "{Tables.MatchData.EventDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchData.EventMetaDataId}" INTEGER NOT NULL,
			     "{Tables.MatchData.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}"),
			     
			     FOREIGN KEY "{Tables.MatchData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			     
			     FOREIGN KEY ("{Tables.MatchData.GameDeviceId}", "{Tables.MatchData.GameId}")
			         REFERENCES "{nameof(Tables.Games)}" ("{Tables.Games.DeviceId}", "{Tables.Games.GameId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 		     
			     FOREIGN KEY ("{Tables.MatchData.EventDeviceId}", "{Tables.MatchData.EventMetaDataId}")
			         REFERENCES "{nameof(Tables.EventMetaData)}" ("{Tables.EventMetaData.DeviceId}", "{Tables.EventMetaData.MetaDataId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.MatchData)}"
			 BEFORE UPDATE ON "{nameof(Tables.MatchData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table. Only Insert and Delete.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "original_matches_must_not_have_parents"
			 BEFORE INSERT ON "{nameof(Tables.MatchData)}"
			 FOR EACH ROW
			 WHEN NEW."{Tables.MatchData.OriginalDeviceId}" = NEW."{Tables.MatchData.DeviceId}"
			  AND NEW."{Tables.MatchData.OriginalMatchId}" = NEW."{Tables.MatchData.MatchId}"
			  AND NEW."{Tables.MatchData.ParentsAsText}" IS NOT NULL
			 BEGIN
			     SELECT RAISE(ABORT, 'Matches that are not edits of other matches must not specify a parent.');
			 END;
			 
			 CREATE TRIGGER IF NOT EXISTS "edited_matches_must_have_parents"
			 BEFORE INSERT ON "{nameof(Tables.MatchData)}"
			 FOR EACH ROW
			 WHEN (NEW."{Tables.MatchData.OriginalDeviceId}" != NEW."{Tables.MatchData.DeviceId}"
			   OR NEW."{Tables.MatchData.OriginalMatchId}" != NEW."{Tables.MatchData.MatchId}")
			  AND NEW."{Tables.MatchData.ParentsAsText}" IS NULL
			 BEGIN
			     SELECT RAISE(ABORT, 'Matches that are edits of other matches must specify a parent.');
			 END;
			 """,
			connection);

		return await command.ExecuteNonQueryAndExpect(0);
	}

	public static Task<bool> CheckIntegrity(SqliteConnection connection) {
		throw new NotImplementedException();
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



	public async Task<GetMatchDataResult> GetAllMatchData() {

		SqliteCommand getMatchData = new($"SELECT * FROM \"{nameof(Tables.Games)}\";", Connection);

		SqliteDataReader reader;
		try {
			reader = await getMatchData.ExecuteReaderAsync();
		} catch (Exception exception) {
			return new ReadDataError(ExceptionError.FromException(exception, getMatchData));
		}




		throw new NotImplementedException();
	}

	public async Task<GetMatchDataResult> GetMatchDataFromGame(GameDto gameDto) {

		SqliteCommand getMatchData = new(
			$"""
			 SELECT * FROM "{nameof(Tables.Games)}"
			 WHERE "{Tables.Games.DeviceId}" = @DeviceId
			   AND "{Tables.Games.GameId}" = @GameId;
			 """,
			Connection);

		getMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		getMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = gameDto.GameId });

		SqliteDataReader reader;
		try {
			reader = await getMatchData.ExecuteReaderAsync();
		} catch (Exception exception) {
			return new ReadDataError(ExceptionError.FromException(exception, getMatchData));
		}

		List<MatchDataDto> allMatchDtos = [];
		while (reader.Read()) {

			SafeGetTextResult deviceIdResult = reader.SafeGetText(Tables.MatchData.DeviceId);
			if (deviceIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.DeviceId, deviceIdResult.Error);
			}
			string deviceId = deviceIdResult.Value;

			SafeGetIntegerResult matchIdResult = reader.SafeGetInteger(Tables.MatchData.MatchId);
			if (matchIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.MatchId, matchIdResult.Error);
			}
			long matchId = matchIdResult.Value;

			SafeGetTextResult originalDeviceIdResult = reader.SafeGetText(Tables.MatchData.OriginalDeviceId);
			if (originalDeviceIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.OriginalDeviceId, originalDeviceIdResult.Error);
			}
			string originalDeviceId = deviceIdResult.Value;

			SafeGetIntegerResult originalMatchIdResult = reader.SafeGetInteger(Tables.MatchData.OriginalMatchId);
			if (originalMatchIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.OriginalMatchId, originalMatchIdResult.Error);
			}
			long originalMatchId = matchIdResult.Value;

			SafeGetNullableTextResult parentsRawResult = reader.SafeGetNullableText(Tables.MatchData.ParentsAsText);
			if (parentsRawResult.IsFailure) {
				return new NullableColumnReadError(Tables.MatchData.ParentsAsText, parentsRawResult.Error);
			}
			string parentsRaw = parentsRawResult.Value.Value.IsT0 ? parentsRawResult.Value.Value.AsT0 : string.Empty;

			ParentsFromTextResult parentListResult = MatchDataDto.ParentsFromText(parentsRaw);
			if (parentListResult.IsFailure) {
				return parentListResult.Error;
			}
			List<(string deviceId, long matchdId)> parents = parentListResult.Value;

			SafeGetTextResult gameDeviceIdResult = reader.SafeGetText(Tables.MatchData.GameDeviceId);
			if (gameDeviceIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.GameDeviceId, gameDeviceIdResult.Error);
			}
			string gameDeviceId = gameDeviceIdResult.Value;

			SafeGetIntegerResult gameIdResult = reader.SafeGetInteger(Tables.MatchData.GameId);
			if (gameIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.GameId, gameIdResult.Error);
			}
			long gameId = gameIdResult.Value;

			SafeGetTextResult eventDeviceIdResult = reader.SafeGetText(Tables.MatchData.EventDeviceId);
			if (eventDeviceIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.EventDeviceId, eventDeviceIdResult.Error);
			}
			string eventDeviceId = eventDeviceIdResult.Value;

			SafeGetIntegerResult eventMetaDataIdResult = reader.SafeGetInteger(Tables.MatchData.EventMetaDataId);
			if (eventMetaDataIdResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.EventMetaDataId, eventMetaDataIdResult.Error);
			}
			long eventMetaDataId = gameIdResult.Value;

			SafeGetTextResult dataResult = reader.SafeGetText(Tables.MatchData.Data);
			if (dataResult.IsFailure) {
				return new ColumnReadError(Tables.MatchData.Data, dataResult.Error);
			}
			string serializedData = dataResult.Value;

			MatchDataDeserializationResult deserializationResult = MatchDataToCsv.Deserialize(serializedData, gameDto.Specification);
			if (deserializationResult.IsFailure) {
				return deserializationResult.Error;
			}

			CreateMatchDataDtoResult result = MatchDataDto.Create(deserializationResult.Value, deviceId, matchId, originalDeviceId, originalMatchId, parents, gameDeviceId,
				gameId, eventDeviceId, eventMetaDataId);

			if (result.IsFailure) {
				return result.Error;
			}

			allMatchDtos.Add(result.Value);
		}

		IEnumerable<IGrouping<(string OriginalDeviceId, long OriginalMatchId), MatchDataDto>> groupedMatches = 
			allMatchDtos.GroupBy(match => (match.OriginalDeviceId, match.OriginalMatchId));



		throw new NotImplementedException();

		return allMatchDtos;
	}

	public async Task<AddNewMatchDataResult> AddNewMatchData(NewMatchDataDto newMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
		}

		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"SELECT \"{Tables.MatchIdSequence.LastUsedId}\" FROM \"{nameof(Tables.MatchIdSequence)}\" WHERE ROWID = 1;", 
			Connection);

		IntegerScalarResult getMatchIdResult = await getMatchId.ExecuteIntegerScalar();
		if (getMatchIdResult.IsFailure) {
			return await RollbackError<GetIdError>.TryRollback(getMatchIdResult.Error, Connection);
		}

		if (getMatchIdResult.Value == long.MaxValue) {
			return await RollbackError<TableOverflowError>.TryRollback(new(), Connection);
		}

		long nextMatchId = getMatchIdResult.Value + 1;

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

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addMatchDataError) {
			return await RollbackError<InsertDataResult>.TryRollback(addMatchDataError, Connection);
		}

		// -------- Update Sequence Table --------
		SqliteCommand updateSequenceTable = new(
			$"""
			 UPDATE "{nameof(Tables.MatchIdSequence)}"
			     SET "{Tables.MatchIdSequence.LastUsedId}" = "{Tables.MatchIdSequence.LastUsedId}"
			     WHERE ROWID = 1;
			 """,
			Connection);

		if (await updateSequenceTable.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError updateSequenceTableError) {
			return await RollbackError<UpdateSequenceError>.TryRollback(updateSequenceTableError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(
			newMatchDataDto.GameDeviceId, newMatchDataDto.GameId, newMatchDataDto.EventDeviceId, newMatchDataDto.EventMetaDataId);

		SetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(newMatchDataDto.DeviceId, nextMatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<SetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}

	public async Task<AddNewEditedMatchDataResult> AddNewEditedMatchData(NewEditedMatchDataDto newEditedMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
		}

		// -------- Get MatchId --------
		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"SELECT \"{Tables.MatchIdSequence.LastUsedId}\" FROM \"{nameof(Tables.MatchIdSequence)}\" WHERE ROWID = 1;",
			Connection);

		IntegerScalarResult getMatchIdResult = await getMatchId.ExecuteIntegerScalar();
		if (getMatchIdResult.IsFailure) {
			return await RollbackError<GetIdError>.TryRollback(getMatchIdResult.Error, Connection);
		}

		if (getMatchIdResult.Value == long.MaxValue) {
			return await RollbackError<TableOverflowError>.TryRollback(new(), Connection);
		}

		long nextMatchId = getMatchIdResult.Value + 1;

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

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addMatchDataError) {
			return await RollbackError<InsertDataResult>.TryRollback(addMatchDataError, Connection);
		}

		// -------- Update Sequence Table --------
		SqliteCommand updateSequenceTable = new(
			$"""
			 UPDATE "{nameof(Tables.MatchIdSequence)}"
			     SET "{Tables.MatchIdSequence.LastUsedId}" = "{Tables.MatchIdSequence.LastUsedId}"
			     WHERE ROWID = 1;
			 """,
			Connection);

		if (await updateSequenceTable.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError updateSequenceTableError) {
			return await RollbackError<UpdateSequenceError>.TryRollback(updateSequenceTableError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(
			newEditedMatchDataDto.GameDeviceId, newEditedMatchDataDto.GameId, newEditedMatchDataDto.EventDeviceId, newEditedMatchDataDto.EventMetaDataId);

		SetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(newEditedMatchDataDto.DeviceId, nextMatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<SetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}

	public async Task<ImportMatchDataResult> ImportMatchData(MatchDataDto importMatchDataDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
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

		if (await addMatchData.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addMatchDataError) {
			return await RollbackError<InsertDataResult>.TryRollback(addMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(
			importMatchDataDto.GameDeviceId, importMatchDataDto.GameId, importMatchDataDto.EventDeviceId, importMatchDataDto.EventMetaDataId);

		SetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(importMatchDataDto.DeviceId, importMatchDataDto.MatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<SetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}

	public async Task<DeleteMatchDataResult> DeleteMatchData(MatchDataDto matchDataToDelete) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
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

		if (await deleteMatchData.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError deleteMatchDataError) {
			return await RollbackError<DeleteDataError>.TryRollback(deleteMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();

		SetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(matchDataToDelete.DeviceId, matchDataToDelete.MatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<SetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}

	public async Task<BulkDeleteMatchDataResult> DeleteMatchDataFromEvent(EventDto eventDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.EventDeviceId}" = @EventDeviceId
			   AND "{Tables.MatchData.EventMetaDataId}" = @EventMetaDataId;
			 """,
			Connection);

		deleteMatchData.Parameters.Add(new("@EventDeviceId", SqliteType.Text) { Value = eventDto.DeviceId });
		deleteMatchData.Parameters.Add(new("@EventMetaDataId", SqliteType.Integer) { Value = eventDto.MetaDataId });

		if (await deleteMatchData.ExecuteNonQueryUnchecked() is ExecuteNonQueryUncheckedError deleteMatchDataError) {
			return await RollbackError<BulkDeleteDataError>.TryRollback(deleteMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();

		BulkSetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(eventDto, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<BulkSetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}

	public async Task<BulkDeleteMatchDataResult> DeleteMatchDataFromGame(GameDto gameDto) {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.GameDeviceId}" = @GameDeviceId
			   AND "{Tables.MatchData.GameId}" = @GameId;
			 """,
			Connection);

		deleteMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		deleteMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = gameDto.GameId });

		if (await deleteMatchData.ExecuteNonQueryUnchecked() is ExecuteNonQueryUncheckedError deleteMatchDataError) {
			return await RollbackError<BulkDeleteDataError>.TryRollback(deleteMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();

		BulkSetRecordMetaDataResult updateIndexResult = await Indexer.SetMatchIndexMetaData(gameDto, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError<BulkSetRecordMetaDataError>.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return new Success();
	}

	public async Task<BulkDeleteMatchDataResult> DeleteAllMatchData() {

		// -------- Open Transaction --------
		SqliteCommand openTransaction = new("BEGIN TRANSACTION;", Connection);
		if (await openTransaction.ExecuteNonQueryAndExpect(0) is ExecuteNonQueryAndExpectError openTransactionError) {
			return new BeginTransactionError(openTransactionError);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new($"DELETE FROM \"{nameof(Tables.MatchData)}\"", Connection);

		if (await deleteMatchData.ExecuteNonQueryUnchecked() is ExecuteNonQueryUncheckedError deleteMatchDataError) {
			return await RollbackError<BulkDeleteDataError>.TryRollback(deleteMatchDataError, Connection);
		}

		// -------- Update Record Index Table --------
		ResetIndexResult deleteResult = await Indexer.ResetMatchIndex();
		if (deleteResult.IsFailure) {
			return await RollbackError<BulkSetRecordMetaDataError>.TryRollback(deleteResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		SqliteCommand commitTransaction = new("COMMIT;", Connection);
		if (await commitTransaction.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError commitError) {
			return await RollbackError<CommitTransactionError>.TryRollback(commitError, Connection);
		}

		return Success.Instance;
	}



	public async Task<GetLastScoutResult> GetLastScout() {

		SqliteCommand command = new(
			$"SELECT \"{Tables.Scout.Name}\" FROM \"{nameof(Tables.Scout)}\" WHERE ROWID = 1;",
			Connection
		);

		TextScalarResult result = await command.ExecuteTextScalar();
		if (result.IsFailure) {
			return result.Error;
		}

		return result.Value;
	}

	public async Task<SetLastScoutResult> SetLastScout(string scoutName) {

		SqliteCommand command = new(
			$"""
			 UPDATE "{nameof(Tables.Scout)}"
			 SET "{Tables.Scout.Name}" = @Name
			 WHERE ROWID = 1;
			 """,
			Connection);

		command.Parameters.Add(new("@Name", SqliteType.Text) { Value = scoutName });

		if (await command.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError error) {
			return error;
		}

		return Success.Instance;
	}

}

// todo: enable the game maker to define migrations from one version of a game to another