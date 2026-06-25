using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace Database.Results;



public abstract record DataStoreError {

	public static DataStoreError FromException(
		Exception exception,
		SqliteCommand command,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "",
		[CallerArgumentExpression(nameof(command))]
			string? commandName = null) {

	return exception is SqliteException sqliteException
		? new SqliteExceptionError(sqliteException, command, callerFilePath, callerMemberName, commandName)
		: new NonSqliteExceptionError(exception, command, callerFilePath, callerMemberName, commandName);
	}

}



public record SqliteExceptionError : DataStoreError {

	public required int SqliteErrorCode { get; init; }

	public required int SqliteExtendedErrorCode { get; init; }

	public required string Message { get; init; }

	public required string CommandText { get; init; }

	public required string CallerFilePath { get; init; }

	public required string CallerMemberName { get; init; }

	public required string? CommandName { get; init; }

	[SetsRequiredMembers]
	public SqliteExceptionError(
		SqliteException exception,
		SqliteCommand command,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "",
		[CallerArgumentExpression(nameof(command))] string? commandName = null) {

		SqliteErrorCode = exception.SqliteErrorCode;
		SqliteExtendedErrorCode = exception.SqliteExtendedErrorCode;
		Message = exception.Message;
		CommandText = command.CommandText;

		CallerFilePath = callerFilePath;
		CallerMemberName = callerMemberName;
		CommandName = commandName;
	}

}



public record NonSqliteExceptionError : DataStoreError {

	public required string? ExceptionType { get; init; }

	public required string Message { get; init; }

	public required string? StackTrack { get; init; }

	public required string CommandText { get; init; }

	public required string CallerFilePath { get; init; }

	public required string CallerMemberName { get; init; }

	public required string? CommandName { get; init; }

	[SetsRequiredMembers]
	public NonSqliteExceptionError(
		Exception exception,
		SqliteCommand command,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "",
		[CallerArgumentExpression(nameof(command))] string? commandName = null) {

		ExceptionType = exception.GetType().FullName;
		Message = exception.Message;
		StackTrack = exception.StackTrace;
		CommandText = command.CommandText;

		CallerFilePath = callerFilePath;
		CallerMemberName = callerMemberName;
		CommandName = commandName;
	}

}



public record TableOverflowError : DataStoreError {

	public required string CallerFilePath { get; init; }

	public required string CallerMemberName { get; init; }

	[SetsRequiredMembers]
	public TableOverflowError(
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "") {

		CallerFilePath = callerFilePath;
		CallerMemberName = callerMemberName;
	}

}



public record WrongNumberOfModificationsError : DataStoreError {

	public required string CommandText { get; init; }

	public required int ExpectedModifications { get; init; }

	public required int ActualModifications { get; init; }

	public required string CallerFilePath { get; init; }

	public required string CallerMemberName { get; init; }

	public required string? CommandName { get; init; }
}



public record ParseError : DataStoreError {

	public required string CommandText { get; init; }

	public required string? ExpectedType { get; init; }

	public required string? ActualType { get; init; }

	public required string? StringValue { get; init; }

	public required string CallerFilePath { get; init; }

	public required string CallerMemberName { get; init; }

	public required string? CommandName { get; init; }
}



public record IndexUpdateError : DataStoreError {

	public required DataStoreError Error { get; init; }
}



public record RollbackError : DataStoreError {

	public required DataStoreError InitialError { get; init; }

	public required DataStoreError SecondError { get; init; }

	private RollbackError() { }

	/// <summary>
	/// Creates a <see cref="SqliteCommand"/> that executes <c>ROLLBACK;</c> and attempts to roll back the current transaction.
	/// </summary>
	/// <param name="firstError"> The <see cref="DataStoreError"/> that occured, creating the desire to roll back. </param>
	/// <param name="connection"> The <see cref="SqliteConnection"/> to be used to execute the rollback command. </param>
	/// <param name="callerFilePath"> The file path of the caller, supplied by <see cref="CallerFilePathAttribute"/>. </param>
	/// <param name="callerMemberName"> The name of the caller, supplied by <see cref="CallerMemberNameAttribute"/>. </param>
	/// <returns>
	/// A <see cref="DataStoreError"/> representing the original failure if the rollback succeeds.<br/>
	/// Otherwise, a <see cref="RollbackError"/> containing information about both the original failure and the rollback failure.
	/// </returns>
	public static async Task<DataStoreError> TryRollbackAndReturn(
		DataStoreError firstError,
		SqliteConnection connection,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "") {

		SqliteCommand rollback = new("ROLLBACK;", connection);

		try {
			await rollback.ExecuteNonQueryAsync();
			return firstError;

		} catch (Exception exception) {
			return new RollbackError {
				InitialError = firstError,
				SecondError = FromException(exception, rollback, callerFilePath, callerMemberName)
			};
		}
	}

}



public record RangeOperationError : DataStoreError;