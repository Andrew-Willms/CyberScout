using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using OneOf;
using Willmsy.AsyncTryResult;

namespace SqliteUtilities;



public static class SqliteDataReaderExtensions {

	public static SafeGetIntegerResult SafeGetInteger(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not long value) {
			return new ColumnHasWrongTypeError(columnName, typeof(long), rawValue.GetType());
		}

		return value;
	}

	public static SafeGetNullableIntegerResult SafeGetNullableInteger(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not long value) {
			return new ColumnHasWrongTypeError(columnName, typeof(long), rawValue.GetType());
		}

		return value;
	}

	public static SafeGetRealResult SafeGetReal(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not double value) {
			return new ColumnHasWrongTypeError(columnName, typeof(double), rawValue.GetType());
		}

		return value;
	}

	public static SafeGetNullableRealResult SafeGetNullableReal(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not double value) {
			return new ColumnHasWrongTypeError(columnName, typeof(double), rawValue.GetType());
		}

		return value;
	}

	public static SafeGetTextResult SafeGetText(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not string text) {
			return new ColumnHasWrongTypeError(columnName, typeof(string), rawValue.GetType());
		}

		return text;
	}

	public static SafeGetNullableTextResult SafeGetNullableText(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not string text) {
			return new ColumnHasWrongTypeError(columnName, typeof(string), rawValue.GetType());
		}

		return text;
	}

	public static SafeGetBlobResult SafeGetBlob(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not byte[] value) {
			return new ColumnHasWrongTypeError(columnName, typeof(byte[]), rawValue.GetType());
		}

		return value;
	}

	public static SafeGetNullableBlobResult SafeGetNullableBlob(this SqliteDataReader reader, string columnName) {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not byte[] value) {
			return new ColumnHasWrongTypeError(columnName, typeof(byte[]), rawValue.GetType());
		}

		return value;
	}

}




[GenerateOneOf]
public partial class SafeGetColumnError : OneOfBase<
	ColumnAccessError,
	ColumnNullError,
	ColumnHasWrongTypeError
>;



[GenerateOneOf]
public partial class SafeGetNullableColumnError : OneOfBase<
	ColumnAccessError,
	ColumnHasWrongTypeError
>;



public record SafeGetIntegerResult : AsyncTryValueResult<long, SafeGetColumnError> {

	public SafeGetIntegerResult(long value) : base(value) { }

	public SafeGetIntegerResult(SafeGetColumnError error) : base(error) { }

	public static implicit operator SafeGetIntegerResult(long value) {
		return new(value);
	}

	public static implicit operator SafeGetIntegerResult(SafeGetColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetIntegerResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetIntegerResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator SafeGetIntegerResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}





public record SafeGetNullableIntegerResult : AsyncTryValueResult<OneOf<long, None>, SafeGetNullableColumnError> {

	public SafeGetNullableIntegerResult(OneOf<long, None> value) : base(value) { }

	public SafeGetNullableIntegerResult(SafeGetNullableColumnError error) : base(error) { }

	public static implicit operator SafeGetNullableIntegerResult(long value) {
		return new(OneOf<long, None>.FromT0(value));
	}

	public static implicit operator SafeGetNullableIntegerResult(None none) {
		return new(OneOf<long, None>.FromT1(none));
	}

	public static implicit operator SafeGetNullableIntegerResult(SafeGetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableIntegerResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableIntegerResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetRealResult : AsyncTryValueResult<double, SafeGetColumnError> {

	public SafeGetRealResult(double value) : base(value) { }

	public SafeGetRealResult(SafeGetColumnError error) : base(error) { }

	public static implicit operator SafeGetRealResult(double value) {
		return new(value);
	}

	public static implicit operator SafeGetRealResult(SafeGetColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetRealResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetRealResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator SafeGetRealResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetNullableRealResult : AsyncTryValueResult<OneOf<double, None>, SafeGetNullableColumnError> {

	public SafeGetNullableRealResult(OneOf<double, None> value) : base(value) { }

	public SafeGetNullableRealResult(SafeGetNullableColumnError error) : base(error) { }

	public static implicit operator SafeGetNullableRealResult(double value) {
		return new(OneOf<double, None>.FromT0(value));
	}

	public static implicit operator SafeGetNullableRealResult(None none) {
		return new(OneOf<double, None>.FromT1(none));
	}

	public static implicit operator SafeGetNullableRealResult(SafeGetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableRealResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableRealResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetTextResult : AsyncTryResult<string, SafeGetColumnError> {

	public SafeGetTextResult(string value) : base(value) { }

	public SafeGetTextResult(SafeGetColumnError error) : base(error) { }

	public static implicit operator SafeGetTextResult(string value) {
		return new(value);
	}

	public static implicit operator SafeGetTextResult(SafeGetColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetTextResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetTextResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator SafeGetTextResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetNullableTextResult : AsyncTryValueResult<OneOf<string, None>, SafeGetNullableColumnError> {

	public SafeGetNullableTextResult(OneOf<string, None> value) : base(value) { }

	public SafeGetNullableTextResult(SafeGetNullableColumnError error) : base(error) { }

	public static implicit operator SafeGetNullableTextResult(string value) {
		return new(OneOf<string, None>.FromT0(value));
	}

	public static implicit operator SafeGetNullableTextResult(None none) {
		return new(OneOf<string, None>.FromT1(none));
	}

	public static implicit operator SafeGetNullableTextResult(SafeGetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableTextResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableTextResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetBlobResult : AsyncTryResult<byte[], SafeGetColumnError> {

	public SafeGetBlobResult(byte[] value) : base(value) { }

	public SafeGetBlobResult(SafeGetColumnError error) : base(error) { }

	public static implicit operator SafeGetBlobResult(byte[] value) {
		return new(value);
	}

	public static implicit operator SafeGetBlobResult(SafeGetColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetBlobResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetBlobResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator SafeGetBlobResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public record SafeGetNullableBlobResult : AsyncTryValueResult<OneOf<byte[], None>, SafeGetNullableColumnError> {

	public SafeGetNullableBlobResult(OneOf<byte[], None> value) : base(value) { }

	public SafeGetNullableBlobResult(SafeGetNullableColumnError error) : base(error) { }

	public static implicit operator SafeGetNullableBlobResult(byte[] value) {
		return new(OneOf<byte[], None>.FromT0(value));
	}

	public static implicit operator SafeGetNullableBlobResult(None none) {
		return new(OneOf<byte[], None>.FromT1(none));
	}

	public static implicit operator SafeGetNullableBlobResult(SafeGetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableBlobResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator SafeGetNullableBlobResult(ColumnHasWrongTypeError error) {
		return new(error);
	}

}



public readonly record struct ColumnAccessError {

	public required string ColumnName { get; init; }

	public required string? ExceptionType { get; init; }

	public required string Message { get; init; }

	public required string? StackTrack { get; init; }

	[SetsRequiredMembers]
	public ColumnAccessError(Exception exception, string columnName) {

		ColumnName = columnName;
		ExceptionType = exception.GetType().FullName;
		Message = exception.Message;
		StackTrack = exception.StackTrace;
	}

}

public readonly record struct ColumnNullError {

	public required string ColumnName { get; init; }

	[SetsRequiredMembers]
	public ColumnNullError(string columnName) {
		ColumnName = columnName;
	}

}

public readonly record struct ColumnHasWrongTypeError {

	public required string ColumnName { get; init; }

	public required string ExpectedType { get; init; }

	public required string ActualType { get; init; }

	[SetsRequiredMembers]
	public ColumnHasWrongTypeError(string columnName, Type expectedType, Type actualType) {

		ColumnName = columnName;
		ExpectedType = expectedType.Name;
		ActualType = actualType.Name;
	}

}