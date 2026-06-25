using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;

namespace SqliteUtilities;



public record WrongNumberOfModificationsError {

	public required int ExpectedModifications { get; init; }

	public required int ActualModifications { get; init; }

	public required string CommandText { get; init; }

}

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

public record WrongScalarTypeError {

	public required string ExpectedType { get; init; }

	public required string ActualType { get; init; }

	public required string CommandText { get; init; }

}

public record NullScalarError {

	public required string CommandText { get; init; }

}