using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public record WrongNumberOfModificationsError : Error {

	public required int ExpectedModifications { get; init; }

	public required int ActualModifications { get; init; }

	public required string CommandText { get; init; }

}

public record SqliteExceptionError : Error {

	public required int SqliteErrorCode { get; init; }

	public required int SqliteExtendedErrorCode { get; init; }

	public required string Message { get; init; }

	public required string CommandText { get; init; }

	[SetsRequiredMembers]
	public SqliteExceptionError(SqliteException exception, SqliteCommand command) {

		SqliteErrorCode = exception.SqliteErrorCode;
		SqliteExtendedErrorCode = exception.SqliteExtendedErrorCode;
		Message = exception.Message;
		CommandText = command.CommandText;
	}

}

public record NonSqliteExceptionError : Error {

	public required string? ExceptionType { get; init; }

	public required string Message { get; init; }

	public required string? StackTrack { get; init; }

	public required string CommandText { get; init; }

	[SetsRequiredMembers]
	public NonSqliteExceptionError(Exception exception, SqliteCommand command) {

		ExceptionType = exception.GetType().FullName;
		Message = exception.Message;
		StackTrack = exception.StackTrace;
		CommandText = command.CommandText;
	}

}

public record WrongScalarTypeError : Error {

	public required string ExpectedType { get; init; }

	public required string ActualType { get; init; }

	public required string CommandText { get; init; }

}

public record NullScalarError : Error {

	public required string CommandText { get; init; }

}

public record RollbackError<TError> : Error where TError : Error {

	public required TError InitialError { get; init; }

	public required ExecuteNonQueryAndExpectError? SecondError { get; init; }

	protected RollbackError() { }

	/// <summary>
	/// Creates a <see cref="SqliteCommand"/> that executes <c>ROLLBACK;</c> and attempts to roll back the current transaction.
	/// </summary>
	/// <param name="firstError"> The <see cref="Error"/> that occured, creating the desire to roll back. </param>
	/// <param name="connection"> The <see cref="SqliteConnection"/> to be used to execute the rollback command. </param>
	/// <returns>
	/// An <see cref="Error"/> representing the original failure if the rollback succeeds.<br/>
	/// Otherwise, a <see cref="RollbackError{TError}"/> containing information about both the original failure and the rollback failure.
	/// </returns>
	public static async Task<Error> TryRollback(TError firstError, SqliteConnection connection) {

		SqliteCommand rollbackCommand = new("ROLLBACK;", connection);
		ExecuteNonQueryAndExpectResult result = await rollbackCommand.ExecuteNonQueryUnchecked();

		if (result.IsFailure) {
			return new RollbackError<TError> {
				InitialError = firstError,
				SecondError = result.Error
			};
		}

		return new RollbackError<TError> {
			InitialError = firstError,
			SecondError = null
		};
	}

}

public record RollbackError : RollbackError<Error>;