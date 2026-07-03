using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OneOf;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public static class DataReaderExtensions {

	public static async Task<ReaderResult> SafeExecuteReader(this SqliteCommand command) {

		try {
			return await command.ExecuteReaderAsync();

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

}



public record ReaderResult : Result<SqliteDataReader, ReaderError> {

	public ReaderResult(SqliteDataReader value) : base(value) { }

	public ReaderResult(ReaderError error) : base(error) { }

	public static implicit operator ReaderResult(SqliteDataReader value) {
		return new(value);
	}

	public static implicit operator ReaderResult(ReaderError error) {
		return new(error);
	}

	public static implicit operator ReaderResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator ReaderResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class ReaderError : OneOfBase<SqliteExceptionError, NonSqliteExceptionError> {

	public static implicit operator Error(ReaderError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}