using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using OneOf;
using SqliteUtilities;

namespace Database.Results;



public record SqliteExceptionError {

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

public record NonSqliteExceptionError {

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

[GenerateOneOf]
public partial class ExceptionError : OneOfBase<SqliteExceptionError, NonSqliteExceptionError> {

	public static ExceptionError FromException(Exception exception, SqliteCommand command) {

		if (exception is SqliteException sqliteException) {
			return new SqliteExceptionError(sqliteException, command);
		}

		return new NonSqliteExceptionError(exception, command);
	}

}



public record BeginTransactionError(ExecuteNonQueryAndExpectError Error);

public record GetIdError(IntegerScalarError Error) {

	public static implicit operator GetIdError(IntegerScalarError error) {
		return new(error);
	}

}

public record TableOverflowError;

public record InsertDataResult(ExecuteNonQueryAndExpectError Error) {

	public static implicit operator InsertDataResult(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

}

public record DeleteDataError(ExecuteNonQueryAndExpectError Error) {

	public static implicit operator DeleteDataError(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

}

public record BulkDeleteDataError(ExecuteNonQueryUncheckedError Error) {

	public static implicit operator BulkDeleteDataError(ExecuteNonQueryUncheckedError error) {
		return new(error);
	}

}

public record UpdateSequenceError(ExecuteNonQueryAndExpectError Error) {

	public static implicit operator UpdateSequenceError(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

}

public record ReadDataError(ExceptionError Error) {

	public static implicit operator ReadDataError(ExceptionError error) {
		return new(error);
	}

}

public record ColumnReadError(string ColumnName, SafeGetColumnError Error);

public record NullableColumnReadError(string ColumnName, SafeGetNullableColumnError Error);

public record CommitTransactionError(ExecuteNonQueryAndExpectError Error) {

	public static implicit operator CommitTransactionError(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

}





public record RollbackError<TError> {

	public required TError InitialError { get; init; }

	public required ExceptionError? SecondError { get; init; }

	private RollbackError() { }

	/// <summary>
	/// Creates a <see cref="SqliteCommand"/> that executes <c>ROLLBACK;</c> and attempts to roll back the current transaction.
	/// </summary>
	/// <param name="firstError"> The <see cref="DataStoreError"/> that occured, creating the desire to roll back. </param>
	/// <param name="connection"> The <see cref="SqliteConnection"/> to be used to execute the rollback command. </param>
	/// <returns>
	/// A <see cref="DataStoreError"/> representing the original failure if the rollback succeeds.<br/>
	/// Otherwise, a <see cref="RollbackError"/> containing information about both the original failure and the rollback failure.
	/// </returns>
	public static async Task<RollbackError<TError>> TryRollback(TError firstError, SqliteConnection connection) {

		SqliteCommand rollbackCommand = new("ROLLBACK;", connection);

		try {
			await rollbackCommand.ExecuteNonQueryAsync();
			return new() {
				InitialError = firstError,
				SecondError = null
			};

		} catch (Exception exception) {
			return new() {
				InitialError = firstError,
				SecondError = ExceptionError.FromException(exception, rollbackCommand)
			};
		}
	}

}



public record RangeOperationError;