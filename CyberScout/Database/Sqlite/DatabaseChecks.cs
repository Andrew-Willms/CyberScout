using Microsoft.Data.Sqlite;
using SqliteUtilities;
using UtilitiesLibrary.Results;

namespace Database.Sqlite;



public static class DatabaseChecks {

	private const string VersionTableName = "DatabaseVersion";
	private const string VersionColumnName = "Version";

	public static async Task<ValueResult<bool>> IsEmpty(SqliteConnection connection) {

		SqliteCommand command = new() {
			CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';",
			Connection = connection
		};
		
		IntegerScalarResult result = await command.ExecuteIntegerScalar();
		if (result.IsFailure) {
			return new AdHocError("Error getting table count.", result.Error);
		}

		long tableCount = result.Value;
		return tableCount == 0;
	}

	public static async Task<ValueResult<long>> GetDatabaseVersion(SqliteConnection connection) {

		// ExecuteScalarAsync() returns the first column of the first row.
		// Because of this I don't need to specify the column or WHERE.
		// However, I feel like its better to be exact even if I don't need to be.
		SqliteCommand command = new(
			$"SELECT \"{VersionColumnName}\" FROM \"{VersionTableName}\" WHERE ROWID = 1;",
			connection
		);

		IntegerScalarResult result = await command.ExecuteIntegerScalar();
		if (result.IsFailure) {
			return new AdHocError("Error getting database version.", result.Error);
		}

		return result.Value;
	}

}