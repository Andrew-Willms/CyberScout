using Microsoft.Data.Sqlite;

namespace Database.Sqlite;



public static class DatabaseChecks {

	private const string VersionTableName = "DatabaseVersion";
	private const string VersionColumnName = "Version";

	public static async Task<bool?> IsEmpty(SqliteConnection connection) {

		// TODO: add errors

		SqliteCommand command = new() {
			CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';",
			Connection = connection
		};

		try {
			SqliteDataReader reader = await command.ExecuteReaderAsync();
			int tableCount = reader.GetInt32(0);
			return tableCount == 0;

		} catch {
			return null;
		}
	}

	public static async Task<uint?> GetDatabaseVersion(SqliteConnection connection) {

		// TODO: add errors

		// ExecuteScalarAsync() returns the first column of the first row.
		// Because of this I don't need to specify the column or WHERE.
		// However, I feel like its better to be exact even if I don't need to be.
		SqliteCommand command = new(
			$"SELECT \"{VersionColumnName}\" FROM \"{VersionTableName}\" WHERE ROWID = 1;",
			connection
		);

		try {
			object? result = await command.ExecuteScalarAsync();
			return result as uint?;

		} catch {
			return null;
		}
	}

}