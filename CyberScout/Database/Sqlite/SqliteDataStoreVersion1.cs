using Comms.Serialization;
using Comms.Serialization.Event;
using Comms.Serialization.Game;
using Comms.Serialization.Match;
using Database.Domain;
using Domain.Dtos.Event;
using Domain.Dtos.Game;
using Domain.Dtos.Match;
using Domain.EventSchedule;
using Domain.GameSpecification;
using Domain.MatchData;
using Microsoft.Data.Sqlite;
using SqliteUtilities;
using UtilitiesLibrary.Results;

namespace Database.Sqlite;



public class SqliteDataStoreVersion1Creator : IDataStoreCreator {

	public async Task<Result<IDataStore>> Create(string settings) {

		Result<SqliteDataStoreVersion1> result = await SqliteDataStoreVersion1.Initialize(settings);

		if (result.IsFailure) {
			return new AdHocError("Error creating database.", result.Error);
		}

		return result.Value;
	}

}



public class SqliteDataStoreVersion1 : IDataStore {

	private const long Version = 1;

	private readonly SqliteConnection Connection;

	private readonly SqliteIndexerVersion1 Indexer;



	private SqliteDataStoreVersion1(SqliteConnection connection) {
		Connection = connection;
		Indexer = new(connection);
	}

	public static async Task<Result<SqliteDataStoreVersion1>> Initialize(string dbPath) {

		SqliteConnection connection = new($"Data Source={dbPath}");
		OpenConnectionResult openConnectionResult = await connection.SafeOpen();
		if (openConnectionResult.IsFailure) {
			return new AdHocError("Error connecting to database.", openConnectionResult.Error);
		}

		ValueResult<bool> isEmptyResult = await DatabaseChecks.IsEmpty(connection);
		if (isEmptyResult.IsFailure) {
			return new AdHocError("Error checking if the database is empty.");
		}
		bool databaseIsEmpty = isEmptyResult.Value;

		if (databaseIsEmpty) {

			Result createResult = await Create(connection);
			if (createResult.IsFailure) {
				return new AdHocError("Error creating database.", createResult.Error);
			}
		}

		// For database version X (where X > 1) you would have something like:
		//ValueResult<long> versionResult = await DatabaseChecks.GetDatabaseVersion(connection);
		//if (versionResult.IsFailure) {
		//	return new AdHocError("Error getting database version.");
		//}
		//
		//if (versionResult.Value < Version) {
		//
		//	// Replace SqliteDataStoreVersion1 with whatever the previous database version is.
		//	Result<SqliteDataStoreVersion[X - 1]> previousVersionResult = await SqliteDataStoreVersion[X - 1].Initialize(connection);
		//	if (previousVersionResult.IsFailure) {
		//		return new AdHocError("Error creating previous database version.", previousVersionResult.Error);
		//	}
		//}

		Result integrityCheckResult = await CheckIntegrity(connection);
		if (integrityCheckResult.IsFailure) {
			return new AdHocError("Database failed integrity check.", integrityCheckResult.Error);
		}

		return new SqliteDataStoreVersion1(connection);
	}

	private static async Task<Result> Create(SqliteConnection connection) {

		Result result = await CreateDatabaseVersionTable(connection);
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

		result = await CreateGameDataTable(connection);
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

	private static async Task<Result> CreateDatabaseVersionTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.DatabaseVersion)}" (
			     "{Tables.DatabaseVersion.Version}" INTEGER NOT NULL
			 );

			 INSERT INTO "{nameof(Tables.DatabaseVersion)}" ("{Tables.DatabaseVersion.Version}")
			 VALUES ({Version});

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding DatabaseVersion table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateScoutTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding Scout table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateKnownDevicesTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding KnownDevices table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateGameIdSequenceTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding GamesIdSequence table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateGameIndexTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding GamesIndex table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateGameDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.GameData)}" (
			     "{Tables.GameData.DeviceId}" TEXT NOT NULL,
			     "{Tables.GameData.GameId}" INTEGER NOT NULL,
			     "{Tables.GameData.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.GameData.DeviceId}", "{Tables.GameData.GameId}"),
			     
			     FOREIGN KEY "{Tables.GameData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.GameData)}"
			 BEFORE UPDATE ON "{nameof(Tables.GameData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding Games table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateEventIdSequenceTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding EventIdSequence table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateEventIndexTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding EventIndex table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateEventDataTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.EventData)}" (
			     "{Tables.EventData.DeviceId}" TEXT NOT NULL,
			     "{Tables.EventData.EventId}" INTEGER NOT NULL,
			     "{Tables.EventData.Data}" BLOB NOT NULL,
			     
			     PRIMARY KEY ("{Tables.EventData.DeviceId}", "{Tables.EventData.EventId}"),
			     
			     FOREIGN KEY "{Tables.EventData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
			 );

			 CREATE TRIGGER IF NOT EXISTS "block_updates_on_{nameof(Tables.EventData)}"
			 BEFORE UPDATE ON "{nameof(Tables.EventData)}"
			 BEGIN
			     SELECT RAISE(ABORT, 'Updates are not allowed on this table; only inserts and deletes.');
			 END;
			 """,
			connection);

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding EventMetaData table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateMatchIdSequenceTable(SqliteConnection connection) {

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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding MatchIdSequence table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateMatchIndexTable(SqliteConnection connection) {

		SqliteCommand command = new(
			$"""
			 CREATE TABLE IF NOT EXISTS "{nameof(Tables.MatchIndex)}" (
			     "{Tables.MatchIndex.DeviceId}" TEXT NOT NULL,
			     "{Tables.MatchIndex.StartIndex}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.EndIndex}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.Status}" TEXT CHECK("{Tables.MatchIndex.Status}" IN ('{nameof(RecordStatus.Stored)}', '{nameof(RecordStatus.Stored)}')),
			     "{Tables.MatchIndex.GameDeviceId}" TEXT NOT NULL,
			     "{Tables.MatchIndex.GameId}" INTEGER NOT NULL,
			     "{Tables.MatchIndex.EventCode}" INTEGER NOT NULL,
			     
			     CHECK ("{Tables.MatchIndex.StartIndex}" <= "{Tables.MatchIndex.EndIndex}"),
			     
			     PRIMARY KEY ("{Tables.MatchIndex.DeviceId}", "{Tables.MatchIndex.StartIndex}", "{Tables.MatchIndex.EndIndex}"),
			     
			     FOREIGN KEY "{Tables.MatchIndex.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" "{Tables.KnownDevices.DeviceId}"
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT
			 	
			     FOREIGN KEY ("{Tables.MatchIndex.GameDeviceId}", "{Tables.MatchIndex.GameId}")
			         REFERENCES "{nameof(Tables.GameData)}" ("{Tables.GameData.DeviceId}", "{Tables.GameData.GameId}")
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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding MatchDataIndex table.", result.Error);
		}

		return Result.Success;
	}

	private static async Task<Result> CreateMatchDataTable(SqliteConnection connection) {

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
			     "{Tables.MatchData.EventCode}" TEXT NOT NULL,
			     "{Tables.MatchData.Data}" TEXT NOT NULL,
			     
			     PRIMARY KEY ("{Tables.MatchData.DeviceId}", "{Tables.MatchData.MatchId}"),
			     
			     FOREIGN KEY "{Tables.MatchData.DeviceId}"
			         REFERENCES "{nameof(Tables.KnownDevices)}" ("{Tables.KnownDevices.DeviceId}")
			             ON UPDATE RESTRICT
			             ON DELETE RESTRICT,
			     
			     FOREIGN KEY ("{Tables.MatchData.GameDeviceId}", "{Tables.MatchData.GameId}")
			         REFERENCES "{nameof(Tables.GameData)}" ("{Tables.GameData.DeviceId}", "{Tables.GameData.GameId}")
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

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(0);
		if (result.IsFailure) {
			return new AdHocError("Error adding MatchData table.", result.Error);
		}

		return Result.Success;
	}

	public static Task<Result> CheckIntegrity(SqliteConnection connection) {
		throw new NotImplementedException();
	}



	public async Task<Result<List<GameDto>>> GetGameSpecs() {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get Game Data --------
		SqliteCommand getEventData = new($"SELECT * FROM \"{nameof(Tables.GameData)}\";", Connection);

		ReaderResult readerResult = await getEventData.SafeExecuteReader();
		if (readerResult.IsFailure) {
			return new AdHocError("Error executing readers.", readerResult.Error);
		}
		SqliteDataReader reader = readerResult.Value;

		// -------- Traverse Reader --------
		List<GameDto> gameDtos = [];
		while (reader.Read()) {

			GetTextResult deviceIdResult = reader.SafeGetText(Tables.GameData.DeviceId);
			if (deviceIdResult.IsFailure) {
				return new AdHocError(Tables.GameData.DeviceId, deviceIdResult.Error);
			}
			string deviceId = deviceIdResult.Value;

			GetIntegerResult gameIdResult = reader.SafeGetInteger(Tables.GameData.GameId);
			if (gameIdResult.IsFailure) {
				return new AdHocError(Tables.GameData.GameId, gameIdResult.Error);
			}
			long gameId = gameIdResult.Value;

			GetBlobResult dataResult = reader.SafeGetBlob(Tables.GameData.Data);
			if (dataResult.IsFailure) {
				return new AdHocError(Tables.GameData.Data, dataResult.Error);
			}
			byte[] serializedSchedule = dataResult.Value;

			Result<GameSpec> deserializationResult = GameSpecToBinary.Deserialize(serializedSchedule);
			if (deserializationResult.IsFailure) {
				return new AdHocError("Error deserializing game specification.", deserializationResult.Error);
			}
			GameSpec gameSpec = deserializationResult.Value;

			gameDtos.Add(new() {
				DeviceId = deviceId,
				GameId = gameId,
				Specification = gameSpec
			});
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return gameDtos;

	}

	public async Task<Result<GameDto>> AddNewGameSpec(NewGameDto newGameDto) {


		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get GameId --------
		SqliteCommand getGameId = new(
			$"SELECT \"{Tables.GameIdSequence.LastUsedId}\" FROM \"{nameof(Tables.GameIdSequence)}\" WHERE ROWID = 1;",
			Connection);

		IntegerScalarResult getGameIdResult = await getGameId.ExecuteIntegerScalar();
		if (getGameIdResult.IsFailure) {
			return await RollbackError.TryRollback(getGameIdResult.Error, Connection);
		}

		if (getGameIdResult.Value == long.MaxValue) {
			return await RollbackError.TryRollback(new AdHocError("Table overflow."), Connection);
		}

		long nextGameId = getGameIdResult.Value + 1;

		// -------- Add Game Data --------
		byte[] data = GameSpecToBinary.Serialize(newGameDto.Specification);

		SqliteCommand addGameSchedule = new(
			$"""
			 INSERT INTO "{nameof(Tables.GameData)}" (
			     "{Tables.GameData.DeviceId}",
			     "{Tables.GameData.GameId}",
			     "{Tables.GameData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addGameSchedule.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = newGameDto.DeviceId });
		addGameSchedule.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = nextGameId });
		addGameSchedule.Parameters.Add(new("@Data", SqliteType.Blob) { Value = data });

		ExecuteNonQueryAndExpectResult addEventResult = await addGameSchedule.ExecuteNonQueryAndExpect(1);
		if (addEventResult.IsFailure) {
			return await RollbackError.TryRollback(addEventResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		GameIndexMetaData metaData = new() { Status = RecordStatus.Stored };
		Result updateIndexResult = await Indexer.SetGameIndexMetaData(newGameDto.DeviceId, nextGameId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return new GameDto {
			DeviceId = newGameDto.DeviceId,
			GameId = nextGameId,
			Specification = newGameDto.Specification
		};

	}

	public async Task<Result> ImportGameSpec(GameDto gameDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Add Game Data --------
		byte[] data = GameSpecToBinary.Serialize(gameDto.Specification);

		SqliteCommand addGameSpec = new(
			$"""
			 INSERT INTO "{nameof(Tables.GameData)}" (
			     "{Tables.GameData.DeviceId}",
			     "{Tables.GameData.GameId}",
			     "{Tables.GameData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addGameSpec.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		addGameSpec.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = gameDto.GameId });
		addGameSpec.Parameters.Add(new("@Data", SqliteType.Blob) { Value = data });

		ExecuteNonQueryAndExpectResult addGameResult = await addGameSpec.ExecuteNonQueryAndExpect(1);
		if (addGameResult.IsFailure) {
			return await RollbackError.TryRollback(addGameResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		GameIndexMetaData metaData = new() { Status = RecordStatus.Stored };
		Result updateIndexResult = await Indexer.SetGameIndexMetaData(gameDto.DeviceId, gameDto.GameId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteGameData(GameDto gameDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Game Data --------
		SqliteCommand deleteGameData = new(
			$"""
			 DELETE FROM "{nameof(Tables.GameData)}"
			 WHERE "{Tables.GameData.DeviceId}" = @DeviceId,
			   AND "{Tables.GameData.GameId}" = @GameId;
			 """,
			Connection);

		deleteGameData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		deleteGameData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = gameDto.GameId });

		ExecuteNonQueryUncheckedResult deleteGameResult = await deleteGameData.ExecuteNonQueryUnchecked();
		if (deleteGameResult.IsFailure) {
			return await RollbackError.TryRollback(deleteGameResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		GameIndexMetaData metaData = new() { Status = RecordStatus.None };
		Result updateIndexResult = await Indexer.SetGameIndexMetaData(gameDto.DeviceId, gameDto.GameId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteAllGameData() {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Event Data --------
		SqliteCommand deleteGameData = new($"DELETE FROM \"{nameof(Tables.GameData)}\"", Connection);
		ExecuteNonQueryUncheckedResult deleteGameDataResult = await deleteGameData.ExecuteNonQueryUnchecked();
		if (deleteGameDataResult.IsFailure) {
			return await RollbackError.TryRollback(deleteGameDataResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		Result deleteResult = await Indexer.ResetGameIndex();
		if (deleteResult.IsFailure) {
			return await RollbackError.TryRollback(deleteResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}



	public async Task<Result<List<EventDto>>> GetEvents() {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get Event Data --------
		SqliteCommand getEventData = new($"SELECT * FROM \"{nameof(Tables.EventData)}\";", Connection);

		ReaderResult readerResult = await getEventData.SafeExecuteReader();
		if (readerResult.IsFailure) {
			return new AdHocError("Error executing readers.", readerResult.Error);
		}
		SqliteDataReader reader = readerResult.Value;

		// -------- Traverse Reader --------
		List<EventDto> eventDtos = [];
		while (reader.Read()) {

			GetTextResult deviceIdResult = reader.SafeGetText(Tables.EventData.DeviceId);
			if (deviceIdResult.IsFailure) {
				return new AdHocError(Tables.EventData.DeviceId, deviceIdResult.Error);
			}
			string deviceId = deviceIdResult.Value;

			GetIntegerResult eventIdResult = reader.SafeGetInteger(Tables.EventData.EventId);
			if (eventIdResult.IsFailure) {
				return new AdHocError(Tables.EventData.EventId, eventIdResult.Error);
			}
			long eventId = eventIdResult.Value;

			GetBlobResult dataResult = reader.SafeGetBlob(Tables.EventData.Data);
			if (dataResult.IsFailure) {
				return new AdHocError(Tables.EventData.Data, dataResult.Error);
			}
			byte[] serializedSchedule = dataResult.Value;

			Result<EventSchedule> deserializationResult = EventScheduleToBinary.Deserialize(serializedSchedule);
			if (deserializationResult.IsFailure) {
				return new AdHocError("Error deserializing event schedule.", deserializationResult.Error);
			}
			EventSchedule eventSchedule = deserializationResult.Value;

			eventDtos.Add(new() {
				DeviceId = deviceId,
				EventId = eventId,
				EventSchedule = eventSchedule
			});
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return eventDtos;
	}

	public async Task<Result<EventDto>> AddNewEvent(NewEventDto newEventDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get EventId --------
		SqliteCommand getEventId = new(
			$"SELECT \"{Tables.EventIdSequence.LastUsedId}\" FROM \"{nameof(Tables.EventIdSequence)}\" WHERE ROWID = 1;",
			Connection);

		IntegerScalarResult getEventIdResult = await getEventId.ExecuteIntegerScalar();
		if (getEventIdResult.IsFailure) {
			return await RollbackError.TryRollback(getEventIdResult.Error, Connection);
		}

		if (getEventIdResult.Value == long.MaxValue) {
			return await RollbackError.TryRollback(new AdHocError("Table overflow."), Connection);
		}

		long nextEventId = getEventIdResult.Value + 1;

		// -------- Add Event Data --------
		byte[] data = EventScheduleToBinary.Serialize(newEventDto.EventSchedule);

		SqliteCommand addEventSchedule = new(
			$"""
			 INSERT INTO "{nameof(Tables.EventData)}" (
			     "{Tables.EventData.DeviceId}",
			     "{Tables.EventData.EventId}",
			     "{Tables.EventData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @EventId,
			     @Data
			 );
			 """,
			Connection);

		addEventSchedule.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = newEventDto.DeviceId });
		addEventSchedule.Parameters.Add(new("@EventId", SqliteType.Integer) { Value = nextEventId });
		addEventSchedule.Parameters.Add(new("@Data", SqliteType.Blob) { Value = data });

		ExecuteNonQueryAndExpectResult addEventResult = await addEventSchedule.ExecuteNonQueryAndExpect(1);
		if (addEventResult.IsFailure) {
			return await RollbackError.TryRollback(addEventResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		EventIndexMetaData metaData = new() { Status = RecordStatus.Stored };
		Result updateIndexResult = await Indexer.SetEventIndexMetaData(newEventDto.DeviceId, nextEventId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return new EventDto {
			DeviceId = newEventDto.DeviceId,
			EventId = nextEventId,
			EventSchedule = newEventDto.EventSchedule
		};
	}

	public async Task<Result> ImportEvent(EventDto eventDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Add Event Data --------
		byte[] data = EventScheduleToBinary.Serialize(eventDto.EventSchedule);

		SqliteCommand addEventSchedule = new(
			$"""
			 INSERT INTO "{nameof(Tables.EventData)}" (
			     "{Tables.EventData.DeviceId}",
			     "{Tables.EventData.EventId}",
			     "{Tables.EventData.Data}"
			 )
			 VALUES (
			     @DeviceId,
			     @EventId,
			     @Data
			 );
			 """,
			Connection);

		addEventSchedule.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = eventDto.DeviceId });
		addEventSchedule.Parameters.Add(new("@EventId", SqliteType.Integer) { Value = eventDto.EventId });
		addEventSchedule.Parameters.Add(new("@Data", SqliteType.Blob) { Value = data });

		ExecuteNonQueryAndExpectResult addEventResult = await addEventSchedule.ExecuteNonQueryAndExpect(1);
		if (addEventResult.IsFailure) {
			return await RollbackError.TryRollback(addEventResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		EventIndexMetaData metaData = new() { Status = RecordStatus.Stored };
		Result updateIndexResult = await Indexer.SetEventIndexMetaData(eventDto.DeviceId, eventDto.EventId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteEventData(EventDto eventDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Event Data --------
		SqliteCommand deleteEventData = new(
			$"""
			 DELETE FROM "{nameof(Tables.EventData)}"
			 WHERE "{Tables.EventData.DeviceId}" = @DeviceId,
			   AND "{Tables.EventData.EventId}" = @EventId;
			 """,
			Connection);

		deleteEventData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = eventDto.DeviceId });
		deleteEventData.Parameters.Add(new("@EventId", SqliteType.Integer) { Value = eventDto.EventId });

		ExecuteNonQueryUncheckedResult deleteEventResult = await deleteEventData.ExecuteNonQueryUnchecked();
		if (deleteEventResult.IsFailure) {
			return await RollbackError.TryRollback(deleteEventResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		EventIndexMetaData metaData = new() { Status = RecordStatus.None };
		Result updateIndexResult = await Indexer.SetEventIndexMetaData(eventDto.DeviceId, eventDto.EventId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteAllEventData() {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Event Data --------
		SqliteCommand deleteEventData = new($"DELETE FROM \"{nameof(Tables.EventIndex)}\"", Connection);
		ExecuteNonQueryUncheckedResult deleteEventDataResult = await deleteEventData.ExecuteNonQueryUnchecked();
		if (deleteEventDataResult.IsFailure) {
			return await RollbackError.TryRollback(deleteEventDataResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		Result deleteResult = await Indexer.ResetEventIndex();
		if (deleteResult.IsFailure) {
			return await RollbackError.TryRollback(deleteResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}



	public async Task<Result<List<MatchDto>>> GetMatchDataFromGame(GameDto gameDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get Match Data --------
		SqliteCommand getMatchData = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.GameDeviceId}" = @DeviceId
			   AND "{Tables.MatchData.GameId}" = @GameId;
			 """,
			Connection);

		getMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = gameDto.DeviceId });
		getMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = gameDto.GameId });

		ReaderResult readerResult = await getMatchData.SafeExecuteReader();
		if (readerResult.IsFailure) {
			return new AdHocError("Error executing readers.", readerResult.Error);
		}
		SqliteDataReader reader = readerResult.Value;

		// -------- Traverse Reader --------
		List<MatchDto> matchDtos = [];
		while (reader.Read()) {

			GetTextResult deviceIdResult = reader.SafeGetText(Tables.MatchData.DeviceId);
			if (deviceIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.DeviceId, deviceIdResult.Error);
			}
			string deviceId = deviceIdResult.Value;

			GetIntegerResult matchIdResult = reader.SafeGetInteger(Tables.MatchData.MatchId);
			if (matchIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.MatchId, matchIdResult.Error);
			}
			long matchId = matchIdResult.Value;

			GetTextResult originalDeviceIdResult = reader.SafeGetText(Tables.MatchData.OriginalDeviceId);
			if (originalDeviceIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.OriginalDeviceId, originalDeviceIdResult.Error);
			}
			string originalDeviceId = deviceIdResult.Value;

			GetIntegerResult originalMatchIdResult = reader.SafeGetInteger(Tables.MatchData.OriginalMatchId);
			if (originalMatchIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.OriginalMatchId, originalMatchIdResult.Error);
			}
			long originalMatchId = matchIdResult.Value;

			GetNullableTextResult parentsRawResult = reader.SafeGetNullableText(Tables.MatchData.ParentsAsText);
			if (parentsRawResult.IsFailure) {
				return new AdHocError(Tables.MatchData.ParentsAsText, parentsRawResult.Error);
			}
			string parentsRaw = parentsRawResult.Value.Value.IsT0 ? parentsRawResult.Value.Value.AsT0 : string.Empty;

			Result<List<(string deviceId, long matchId)>> parentListResult = Parents.FromText(parentsRaw);
			if (parentListResult.IsFailure) {
				return new AdHocError("Error getting parents from text.", parentListResult.Error);
			}
			List<(string deviceId, long matchdId)> parents = parentListResult.Value;

			GetTextResult gameDeviceIdResult = reader.SafeGetText(Tables.MatchData.GameDeviceId);
			if (gameDeviceIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.GameDeviceId, gameDeviceIdResult.Error);
			}
			string gameDeviceId = gameDeviceIdResult.Value;

			GetIntegerResult gameIdResult = reader.SafeGetInteger(Tables.MatchData.GameId);
			if (gameIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.GameId, gameIdResult.Error);
			}
			long gameId = gameIdResult.Value;

			GetTextResult eventDeviceIdResult = reader.SafeGetText(Tables.MatchData.EventCode);
			if (eventDeviceIdResult.IsFailure) {
				return new AdHocError(Tables.MatchData.EventCode, eventDeviceIdResult.Error);
			}
			string eventCode = eventDeviceIdResult.Value;

			GetTextResult dataResult = reader.SafeGetText(Tables.MatchData.Data);
			if (dataResult.IsFailure) {
				return new AdHocError(Tables.MatchData.Data, dataResult.Error);
			}
			string serializedData = dataResult.Value;

			Result<MatchData> deserializationResult = MatchDataToCsv.Deserialize(serializedData, gameDto.Specification);
			if (deserializationResult.IsFailure) {
				return new AdHocError("Error deserializing match data.", deserializationResult.Error);
			}
			MatchData matchData = deserializationResult.Value;

			if (eventCode != matchData.EventCode) {
				return new AdHocError("EventCode column different from deserialized EventCode.");
			}

			CreateMatchDataDtoResult result = MatchDto.Create(
				matchData, deviceId, matchId, originalDeviceId, originalMatchId, parents, gameDeviceId, gameId);

			if (result.IsFailure) {
				return new AdHocError("Error creating matchDataDto.", result.Error);
			}

			matchDtos.Add(result.Value);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return matchDtos;
	}

	public async Task<Result<MatchDto>> AddNewMatchData(NewMatchDto newMatchDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"SELECT \"{Tables.MatchIdSequence.LastUsedId}\" FROM \"{nameof(Tables.MatchIdSequence)}\" WHERE ROWID = 1;", 
			Connection);

		IntegerScalarResult getMatchIdResult = await getMatchId.ExecuteIntegerScalar();
		if (getMatchIdResult.IsFailure) {
			return await RollbackError.TryRollback(getMatchIdResult.Error, Connection);
		}

		if (getMatchIdResult.Value == long.MaxValue) {
			return await RollbackError.TryRollback(new AdHocError("Table overflow."), Connection);
		}

		long nextMatchId = getMatchIdResult.Value + 1;

		// -------- Add Match Data --------
		string data = MatchDataToCsv.Serialize(newMatchDto.Data);

		SqliteCommand addMatchData = new(
			$"""
			 INSERT INTO "{nameof(Tables.MatchData)}" (
			     "{Tables.MatchData.DeviceId}",
			     "{Tables.MatchData.MatchId}"
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

		addMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = newMatchDto.DeviceId });
		addMatchData.Parameters.Add(new("@NextMatchId", SqliteType.Integer) { Value = nextMatchId });
		addMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = newMatchDto.GameDeviceId });
		addMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = newMatchDto.GameId });
		addMatchData.Parameters.Add(new("@Data", SqliteType.Text) { Value = data });

		if ((await addMatchData.ExecuteNonQueryAndExpect(1)).IsError(out ExecuteNonQueryAndExpectError? addMatchDataError)) {
			return await RollbackError.TryRollback(addMatchDataError, Connection);
		}

		// -------- Update Sequence Table --------
		SqliteCommand updateSequenceTable = new(
			$"""
			 UPDATE "{nameof(Tables.MatchIdSequence)}"
			     SET "{Tables.MatchIdSequence.LastUsedId}" = "{Tables.MatchIdSequence.LastUsedId}"
			     WHERE ROWID = 1;
			 """,
			Connection);

		ExecuteNonQueryAndExpectResult updateSequenceResult = await updateSequenceTable.ExecuteNonQueryAndExpect(1);
		if (updateSequenceResult.IsFailure) {
			return await RollbackError.TryRollback(updateSequenceResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(newMatchDto.GameDeviceId, newMatchDto.GameId, newMatchDto.EventCode);

		Result updateIndexResult = await Indexer.SetMatchIndexMetaData(newMatchDto.DeviceId, nextMatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		CreateMatchDataDtoResult createMatchDataDtoResult = MatchDto.Create(
			matchData: newMatchDto.Data,
			deviceId: newMatchDto.DeviceId,
			matchId: nextMatchId,
			originalDeviceId: newMatchDto.DeviceId,
			originalMatchId: nextMatchId,
			parents: [],
			gameDeviceId: newMatchDto.GameDeviceId,
			gameId: newMatchDto.GameId);

		if (createMatchDataDtoResult.IsFailure) {
			return new AdHocError("Error creating MatchDataDto from provided newMatchId.", createMatchDataDtoResult.Error, ("nextMatchId", nextMatchId.ToString()));
		}

		return createMatchDataDtoResult.Value;
	}

	public async Task<Result<MatchDto>> AddEditedMatchData(EditedMatchDto editedMatchDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Get MatchId --------
		SqliteCommand getMatchId = new(
			$"SELECT \"{Tables.MatchIdSequence.LastUsedId}\" FROM \"{nameof(Tables.MatchIdSequence)}\" WHERE ROWID = 1;",
			Connection);

		IntegerScalarResult getMatchIdResult = await getMatchId.ExecuteIntegerScalar();
		if (getMatchIdResult.IsFailure) {
			return await RollbackError.TryRollback(getMatchIdResult.Error, Connection);
		}

		if (getMatchIdResult.Value == long.MaxValue) {
			return await RollbackError.TryRollback(new AdHocError("Table overflow."), Connection);
		}

		long nextMatchId = getMatchIdResult.Value + 1;

		// -------- Add Match Data --------
		CreateMatchDataDtoResult createMatchDataDtoResult = MatchDto.Create(
			matchData: editedMatchDto.Data,
			deviceId: editedMatchDto.DeviceId,
			matchId: nextMatchId,
			originalDeviceId: editedMatchDto.DeviceId,
			originalMatchId: nextMatchId,
			parents: [],
			gameDeviceId: editedMatchDto.GameDeviceId,
			gameId: editedMatchDto.GameId);

		if (createMatchDataDtoResult.IsFailure) {
			return new AdHocError("Error creating MatchDataDto from provided newMatchId.", createMatchDataDtoResult.Error, ("nextMatchId", nextMatchId.ToString()));
		}

		MatchDto completeMatchDto = createMatchDataDtoResult.Value;

		Result result = await AddMatchDtoAndCommit(completeMatchDto);
		if (result.IsFailure) {
			return new AdHocError("Error executing AddMatchDtoAndCommit.", result.Error);
		}

		return completeMatchDto;
	}

	public async Task<Result> ImportMatchData(MatchDto matchDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		Result result = await AddMatchDtoAndCommit(matchDto);
		if (result.IsFailure) {
			return new AdHocError("Error executing AddMatchDtoAndCommit.", result.Error);
		}

		return Result.Success;
	}

	private async Task<Result> AddMatchDtoAndCommit(MatchDto matchDto) {

		// -------- Add Match Data --------
		string data = MatchDataToCsv.Serialize(matchDto.Data);
		string parentsAsText = Parents.ToText(matchDto.Parents);

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
			     @ParentsAsText,
			     @GameDeviceId,
			     @GameId,
			     @Data
			 );
			 """,
			Connection);

		addMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = matchDto.DeviceId });
		addMatchData.Parameters.Add(new("@MatchId", SqliteType.Integer) { Value = matchDto.DeviceId });
		addMatchData.Parameters.Add(new("@OriginalDeviceId", SqliteType.Text) { Value = matchDto.OriginalDeviceId });
		addMatchData.Parameters.Add(new("@OriginalMatchId", SqliteType.Integer) { Value = matchDto.OriginalMatchId });
		addMatchData.Parameters.Add(new("@ParentsAsText", SqliteType.Text) { Value = parentsAsText });
		addMatchData.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = matchDto.GameDeviceId });
		addMatchData.Parameters.Add(new("@GameId", SqliteType.Integer) { Value = matchDto.GameId });
		addMatchData.Parameters.Add(new("@Data", SqliteType.Text) { Value = data });

		ExecuteNonQueryAndExpectResult addMatchDataResult = await addMatchData.ExecuteNonQueryAndExpect(1);
		if (addMatchDataResult.IsFailure) {
			return await RollbackError.TryRollback(addMatchDataResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateStoredMatch(
			matchDto.GameDeviceId, matchDto.GameId, matchDto.EventCode);

		Result updateIndexResult = await Indexer.SetMatchIndexMetaData(matchDto.DeviceId, matchDto.MatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteMatchData(MatchDto matchDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.DeviceId}" = @DeviceId
			   AND "{Tables.MatchData.MatchId}" = @MatchId;
			 """,
			Connection);

		deleteMatchData.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = matchDto.OriginalDeviceId });
		deleteMatchData.Parameters.Add(new("@MatchId", SqliteType.Integer) { Value = matchDto.OriginalMatchId });

		ExecuteNonQueryUncheckedResult deleteMatchResult = await deleteMatchData.ExecuteNonQueryUnchecked();
		if (deleteMatchResult.IsFailure) {
			return await RollbackError.TryRollback(deleteMatchResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();
		Result updateIndexResult = await Indexer.SetMatchIndexMetaData(matchDto.DeviceId, matchDto.MatchId, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteMatchDataFromEvent(string eventCode) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new(
			$"""
			 DELETE FROM "{nameof(Tables.MatchData)}"
			 WHERE "{Tables.MatchData.EventCode}" = @EventCode;
			 """,
			Connection);

		deleteMatchData.Parameters.Add(new("@EventCode", SqliteType.Text) { Value = eventCode });

		ExecuteNonQueryUncheckedResult deleteMatchResult = await deleteMatchData.ExecuteNonQueryUnchecked();
		if (deleteMatchResult.IsFailure) {
			return await RollbackError.TryRollback(deleteMatchResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();

		Result updateIndexResult = await Indexer.SetMatchIndexMetaData(eventCode, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteMatchDataFromGame(GameDto gameDto) {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
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

		ExecuteNonQueryUncheckedResult deleteMatchResult = await deleteMatchData.ExecuteNonQueryUnchecked();
		if (deleteMatchResult.IsFailure) {
			return await RollbackError.TryRollback(deleteMatchResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		MatchIndexMetaData metaData = MatchIndexMetaData.CreateNoneMatch();
		Result updateIndexResult = await Indexer.SetMatchIndexMetaData(gameDto, metaData);
		if (updateIndexResult.IsFailure) {
			return await RollbackError.TryRollback(updateIndexResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}

	public async Task<Result> DeleteAllMatchData() {

		// -------- Open Transaction --------
		BeginTransactionResult beginResult = await Connection.OpenTransaction();
		if (beginResult.IsFailure) {
			return new AdHocError("Error opening transaction", beginResult.Error);
		}

		// -------- Delete Match Data --------
		SqliteCommand deleteMatchData = new($"DELETE FROM \"{nameof(Tables.MatchData)}\"", Connection);
		ExecuteNonQueryUncheckedResult deleteMatchResult = await deleteMatchData.ExecuteNonQueryUnchecked();
		if (deleteMatchResult.IsFailure) {
			return await RollbackError.TryRollback(deleteMatchResult.Error, Connection);
		}

		// -------- Update Record Index Table --------
		Result deleteResult = await Indexer.ResetMatchIndex();
		if (deleteResult.IsFailure) {
			return await RollbackError.TryRollback(deleteResult.Error, Connection);
		}

		// -------- Commit Transaction --------
		CommitTransactionResult commitResult = await Connection.CommitTransaction();
		if (commitResult.IsFailure) {
			return await RollbackError.TryRollback(commitResult.Error, Connection);
		}

		return Result.Success;
	}



	public async Task<Result<string>> GetLastScout() {

		SqliteCommand command = new(
			$"SELECT \"{Tables.Scout.Name}\" FROM \"{nameof(Tables.Scout)}\" WHERE ROWID = 1;",
			Connection
		);

		TextScalarResult result = await command.ExecuteTextScalar();
		if (result.IsFailure) {
			return new AdHocError("Error getting scout.", result.Error);
		}

		return result.Value;
	}

	public async Task<Result> SetLastScout(string scoutName) {

		SqliteCommand command = new(
			$"""
			 UPDATE "{nameof(Tables.Scout)}"
			 SET "{Tables.Scout.Name}" = @Name
			 WHERE ROWID = 1;
			 """,
			Connection);

		command.Parameters.Add(new("@Name", SqliteType.Text) { Value = scoutName });

		ExecuteNonQueryAndExpectResult result = await command.ExecuteNonQueryAndExpect(1);
		if (result.IsFailure) {
			return new AdHocError("Error setting scout.", result.Error);
		}

		return Result.Success;
	}

}

// todo: enable the game maker to define migrations from one version of a game to another