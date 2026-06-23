using Microsoft.Data.Sqlite;
using UtilitiesLibrary.Results;

namespace Database.Results;



public class OpenTransactionSqliteError(SqliteException exception) : Error {

	public SqliteException Exception { get; } = exception;
}

public class OpenTransactionError(Exception exception) : Error {

	public Exception Exception { get; } = exception;
}

public class AddMatchDataSqliteError(SqliteException exception) : Error {

	public SqliteException Exception { get; } = exception;
}

public class AddMatchDataError(Exception exception) : Error {

	public Exception Exception { get; } = exception;
}

public class IncrementIdSqliteError(SqliteException exception) : Error {

	public SqliteException Exception { get; } = exception;
}

public class IncrementIdError(Exception exception) : Error {

	public Exception Exception { get; } = exception;
}

public class CommitTransactionSqliteError(SqliteException exception) : Error {

	public SqliteException Exception { get; } = exception;
}

public class CommitTransactionError(Exception exception) : Error {

	public Exception Exception { get; } = exception;
}

public class RollBackError(SqliteException firException, Exception rollbackException) : Error {

	public SqliteException FirstException { get; } = firException;

	public Exception RollbackException { get; } = rollbackException;

}