using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OneOf;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public static class NonQueryExtensions {

	/// <summary> Executes an <see cref="SqliteCommand"/> as a non-query, expecting a specific number of modifications to occur. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a non-query. </param>
	/// <param name="expectedModifications"> The expected number of modifications performed by the <see cref="SqliteCommand"/>. An <see cref="WrongNumberOfModificationsError"/> error is returned if the actual result differs. </param>
	/// <returns> A <see cref="ExecuteNonQueryAndExpectError"/> if an error occured, otherwise <see langword="null"/>. </returns>
	public static async Task<ExecuteNonQueryAndExpectResult> ExecuteNonQueryAndExpect(this SqliteCommand command, int expectedModifications) {

		try {
			int modifications = await command.ExecuteNonQueryAsync();

			if (modifications != expectedModifications) {
				return new WrongNumberOfModificationsError {
					CommandText = command.CommandText,
					ExpectedModifications = expectedModifications,
					ActualModifications = modifications
				};
			}

			return ExecuteNonQueryAndExpectResult.Success;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> as a non-query, expecting a specific number of modifications to occur. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a non-query. </param>
	/// <returns> A <see cref="ExecuteNonQueryUncheckedError"/> if an error occured, otherwise <see langword="null"/>. </returns>
	public static async Task<ExecuteNonQueryUncheckedResult> ExecuteNonQueryUnchecked(this SqliteCommand command) {

		try {
			await command.ExecuteNonQueryAsync();
			return ExecuteNonQueryAndExpectResult.Success;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

}



public record ExecuteNonQueryAndExpectResult : ResultCustomError<ExecuteNonQueryAndExpectError> {

	protected ExecuteNonQueryAndExpectResult() {} 

	public ExecuteNonQueryAndExpectResult(ExecuteNonQueryAndExpectError error) : base(error) { }

	// TODO generate this
	public new static readonly ExecuteNonQueryAndExpectResult Success = new();

	public static implicit operator ExecuteNonQueryAndExpectResult(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

	public static implicit operator ExecuteNonQueryAndExpectResult(WrongNumberOfModificationsError error) {
		return new(error);
	}

	public static implicit operator ExecuteNonQueryAndExpectResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator ExecuteNonQueryAndExpectResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

public record ExecuteNonQueryUncheckedResult : ResultCustomError<ExecuteNonQueryUncheckedError> {

	protected ExecuteNonQueryUncheckedResult() { }

	public ExecuteNonQueryUncheckedResult(ExecuteNonQueryUncheckedError error) : base(error) { }

	public new static readonly ExecuteNonQueryUncheckedResult Success = new();  // TODO generate this

	public static implicit operator ExecuteNonQueryUncheckedResult(ExecuteNonQueryUncheckedError error) {
		return new(error);
	}

	public static implicit operator ExecuteNonQueryUncheckedResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator ExecuteNonQueryUncheckedResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class ExecuteNonQueryAndExpectError : OneOfBase<
	WrongNumberOfModificationsError,
	SqliteExceptionError,
	NonSqliteExceptionError> {

	public static implicit operator Error(ExecuteNonQueryAndExpectError error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}

[GenerateOneOf]
public partial class ExecuteNonQueryUncheckedError : OneOfBase<
	SqliteExceptionError,
	NonSqliteExceptionError> {

	public static implicit operator Error(ExecuteNonQueryUncheckedError error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}