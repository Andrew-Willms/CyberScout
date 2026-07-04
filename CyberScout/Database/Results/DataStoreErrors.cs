using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using OneOf;
using SqliteUtilities;
using UtilitiesLibrary.Results;

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

public record GetIdError(IntegerScalarError Error) : Error {

	public static implicit operator GetIdError(IntegerScalarError error) {
		return new(error);
	}

}

public record TableOverflowError : Error;

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

public record ColumnReadError(string ColumnName, GetColumnError Error);

public record NullableColumnReadError(string ColumnName, GetNullableColumnError Error);

public record CommitTransactionError(ExecuteNonQueryAndExpectError Error) {

	public static implicit operator CommitTransactionError(ExecuteNonQueryAndExpectError error) {
		return new(error);
	}

}