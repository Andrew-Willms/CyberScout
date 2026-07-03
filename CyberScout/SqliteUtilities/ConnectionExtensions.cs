using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OneOf;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public static class ConnectionExtensions {

	public static async Task<OpenConnectionResult> SafeOpen(this SqliteConnection connection) {

		try {
			await connection.OpenAsync();
			return OpenConnectionResult.Success;

		} catch (SqliteException exception) {
			return new SqliteExceptionOpenConnectionError(exception, connection.ConnectionString);

		} catch (Exception exception) {
			return new ExceptionOpenConnectionError(exception, connection.ConnectionString);
		}
	}

}



public record OpenConnectionResult : ResultCustomError<OpenConnectionError> {

	protected OpenConnectionResult() { }

	public OpenConnectionResult(OpenConnectionError error) : base(error) { }

	public new static readonly OpenConnectionResult Success = new();

	public static implicit operator OpenConnectionResult(OpenConnectionError error) {
		return new(error);
	}

	public static implicit operator OpenConnectionResult(SqliteExceptionOpenConnectionError error) {
		return new(error);
	}

	public static implicit operator OpenConnectionResult(ExceptionOpenConnectionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class OpenConnectionError : OneOfBase<
	SqliteExceptionOpenConnectionError,
	ExceptionOpenConnectionError> {

	public static implicit operator Error(OpenConnectionError error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}

public record SqliteExceptionOpenConnectionError : Error {

	public required int SqliteErrorCode { get; init; }

	public required int SqliteExtendedErrorCode { get; init; }

	public required string Message { get; init; }

	public required string ConnectionString { get; init; }

	[SetsRequiredMembers]
	public SqliteExceptionOpenConnectionError(SqliteException exception, string connectionString) {

		SqliteErrorCode = exception.SqliteErrorCode;
		SqliteExtendedErrorCode = exception.SqliteExtendedErrorCode;
		Message = exception.Message;
		ConnectionString = connectionString;
	}

}

public record ExceptionOpenConnectionError : Error {

	public required string? ExceptionType { get; init; }

	public required string Message { get; init; }

	public required string? StackTrack { get; init; }

	public required string ConnectionString { get; init; }

	[SetsRequiredMembers]
	public ExceptionOpenConnectionError(Exception exception, string connectionString) {

		ExceptionType = exception.GetType().FullName;
		Message = exception.Message;
		StackTrack = exception.StackTrace;
		ConnectionString = connectionString;
	}

}