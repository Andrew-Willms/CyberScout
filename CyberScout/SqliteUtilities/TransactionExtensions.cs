using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OneOf;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public static class TransactionExtensions {

	public static async Task<BeginTransactionResult> OpenTransaction(this SqliteConnection connection) {

		SqliteCommand command = new("BEGIN TRANSACTION;", connection);

		try {
			int modifications = await command.ExecuteNonQueryAsync();

			if (modifications != 1) {
				return new WrongNumberOfModificationsError {
					CommandText = command.CommandText,
					ExpectedModifications = 1,
					ActualModifications = modifications
				};
			}

			return BeginTransactionResult.Success;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	public static async Task<CommitTransactionResult> CommitTransaction(this SqliteConnection connection) {

		SqliteCommand command = new("COMMIT;", connection);

		try {
			int modifications = await command.ExecuteNonQueryAsync();

			if (modifications != 1) {
				return new WrongNumberOfModificationsError {
					CommandText = command.CommandText,
					ExpectedModifications = 1,
					ActualModifications = modifications
				};
			}

			return CommitTransactionResult.Success;

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

}


public record BeginTransactionResult : ResultCustomError<BeginTransactionError> {

	protected BeginTransactionResult() { }

	public BeginTransactionResult(BeginTransactionError error) : base(error) { }

	// TODO generate this
	public new static readonly BeginTransactionResult Success = new();

	public static implicit operator BeginTransactionResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator BeginTransactionResult(WrongNumberOfModificationsError error) {
		return new(error);
	}

	public static implicit operator BeginTransactionResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator BeginTransactionResult(NonSqliteExceptionError error) {
		return new(error);
	}

}


[GenerateOneOf]
public partial class BeginTransactionError : OneOfBase<
	WrongNumberOfModificationsError,
	SqliteExceptionError,
	NonSqliteExceptionError> {

	public static implicit operator Error(BeginTransactionError error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}

public record CommitTransactionResult : ResultCustomError<CommitTransactionError> {

	protected CommitTransactionResult() { }

	public CommitTransactionResult(CommitTransactionError error) : base(error) { }

	// TODO generate this
	public new static readonly CommitTransactionResult Success = new();

	public static implicit operator CommitTransactionResult(CommitTransactionError error) {
		return new(error);
	}

	public static implicit operator CommitTransactionResult(WrongNumberOfModificationsError error) {
		return new(error);
	}

	public static implicit operator CommitTransactionResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator CommitTransactionResult(NonSqliteExceptionError error) {
		return new(error);
	}

}


[GenerateOneOf]
public partial class CommitTransactionError : OneOfBase<
	WrongNumberOfModificationsError,
	SqliteExceptionError,
	NonSqliteExceptionError> {

	public static implicit operator Error(CommitTransactionError error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}
