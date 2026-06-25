using Microsoft.Data.Sqlite;
using OneOf;

namespace SqliteUtilities;



[GenerateOneOf]
public partial class ExecuteNonQueryAndExpectError : OneOfBase<
	WrongNumberOfModificationsError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

[GenerateOneOf]
public partial class ExecuteNonQueryUncheckedError : OneOfBase<
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public static class NonQuery {

	/// <summary> Executes an <see cref="SqliteCommand"/> as a non-query, expecting a specific number of modifications to occur. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a non-query. </param>
	/// <param name="expectedModifications"> The expected number of modifications performed by the <see cref="SqliteCommand"/>. An <see cref="WrongNumberOfModificationsError"/> error is returned if the actual result differs. </param>
	/// <returns> A <see cref="ExecuteNonQueryAndExpectError"/> if an error occured, otherwise <see langword="null"/>. </returns>
	public static async Task<ExecuteNonQueryAndExpectError?> ExecuteNonQueryAndExpect(this SqliteCommand command, int expectedModifications) {

		try {
			int modifications = await command.ExecuteNonQueryAsync();

			if (modifications != expectedModifications) {
				return new WrongNumberOfModificationsError {
					CommandText = command.CommandText,
					ExpectedModifications = expectedModifications,
					ActualModifications = modifications
				};
			}

			return null;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> as a non-query, expecting a specific number of modifications to occur. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a non-query. </param>
	/// <returns> A <see cref="ExecuteNonQueryUncheckedError"/> if an error occured, otherwise <see langword="null"/>. </returns>
	public static async Task<ExecuteNonQueryUncheckedError?> ExecuteNonQueryUnchecked(this SqliteCommand command) {

		try {
			await command.ExecuteNonQueryAsync();
			return null;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

}