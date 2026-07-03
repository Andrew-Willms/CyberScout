using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using OneOf;
using UtilitiesLibrary.Results;

namespace SqliteUtilities;



public static class ColumnReaderExtensions {

	public static GetIntegerResult SafeGetInteger(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(long), rawValue.GetType());
		}

		return value;
	}

	public static GetNullableIntegerResult SafeGetNullableInteger(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(long), rawValue.GetType());
		}

		return value;
	}

	public static GetRealResult SafeGetReal(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(double), rawValue.GetType());
		}

		return value;
	}

	public static GetNullableRealResult SafeGetNullableReal(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(double), rawValue.GetType());
		}

		return value;
	}

	public static GetTextResult SafeGetText(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(string), rawValue.GetType());
		}

		return text;
	}

	public static GetNullableTextResult SafeGetNullableText(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(string), rawValue.GetType());
		}

		return text;
	}

	public static GetBlobResult SafeGetBlob(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(byte[]), rawValue.GetType());
		}

		return value;
	}

	public static GetNullableBlobResult SafeGetNullableBlob(this SqliteDataReader reader, string columnName) {

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
			return new ColumnTypeError(columnName, typeof(byte[]), rawValue.GetType());
		}

		return value;
	}

	public static GetEnumResult<TEnum> SafeGetTextEnum<TEnum>(this SqliteDataReader reader, string columnName) where TEnum : struct, Enum {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not string stringValue) {
			return new ColumnTypeError(columnName, typeof(string), rawValue.GetType());
		}

		if (Enum.TryParse(stringValue, out TEnum result)) {
			return new NotEnumValueError(columnName, stringValue);
		}

		return result;
	}

	public static GetNullableEnumResult<TEnum> SafeGetNullableTextEnum<TEnum>(this SqliteDataReader reader, string columnName) where TEnum : struct, Enum {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not string stringValue) {
			return new ColumnTypeError(columnName, typeof(string), rawValue.GetType());
		}

		if (Enum.TryParse(stringValue, out TEnum result)) {
			return new NotEnumValueError(columnName, stringValue);
		}

		return result;
	}

	public static GetEnumResult<TEnum> SafeGetIntegerEnum<TEnum>(this SqliteDataReader reader, string columnName) where TEnum : struct, Enum {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return new ColumnNullError(columnName);
		}

		if (rawValue is not long integerValue) {
			return new ColumnTypeError(columnName, typeof(long), rawValue.GetType());
		}

		if (Enum.IsDefined(typeof(TEnum), integerValue)) {
			return new NotEnumValueError(columnName, integerValue.ToString());
		}

		return (TEnum)(object)integerValue;
	}

	public static GetNullableEnumResult<TEnum> SafeGetNullableIntegerEnum<TEnum>(this SqliteDataReader reader, string columnName) where TEnum : struct, Enum {

		object rawValue;
		try {
			rawValue = reader[columnName];
		} catch (Exception exception) {
			return new ColumnAccessError(exception, columnName);
		}

		if (rawValue is DBNull) {
			return None.Instance;
		}

		if (rawValue is not long integerValue) {
			return new ColumnTypeError(columnName, typeof(long), rawValue.GetType());
		}

		if (Enum.IsDefined(typeof(TEnum), integerValue)) {
			return new NotEnumValueError(columnName, integerValue.ToString());
		}

		return (TEnum)(object)integerValue;
	}

}



[GenerateOneOf]
public partial class GetColumnError : OneOfBase<
	ColumnAccessError,
	ColumnNullError,
	ColumnTypeError> {

	public static implicit operator Error(GetColumnError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}

[GenerateOneOf]
public partial class GetNullableColumnError : OneOfBase<
	ColumnAccessError,
	ColumnTypeError> {

	public static implicit operator Error(GetNullableColumnError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}

[GenerateOneOf]
public partial class GetEnumColumnError : OneOfBase<
	ColumnAccessError,
	ColumnNullError,
	ColumnTypeError,
	NotEnumValueError> {

	public static implicit operator Error(GetEnumColumnError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3,
			error4 => error4);
	}

}

[GenerateOneOf]
public partial class GetNullableEnumColumnError : OneOfBase<
	ColumnAccessError,
	ColumnTypeError,
	NotEnumValueError> {

	public static implicit operator Error(GetNullableEnumColumnError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}



public record GetIntegerResult : ValueResult<long, GetColumnError> {

	public GetIntegerResult(long value) : base(value) { }

	public GetIntegerResult(GetColumnError error) : base(error) { }

	public static implicit operator GetIntegerResult(long value) {
		return new(value);
	}

	public static implicit operator GetIntegerResult(GetColumnError error) {
		return new(error);
	}

	public static implicit operator GetIntegerResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetIntegerResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator GetIntegerResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetNullableIntegerResult : ValueResult<OneOf<long, None>, GetNullableColumnError> {

	public GetNullableIntegerResult(OneOf<long, None> value) : base(value) { }

	public GetNullableIntegerResult(GetNullableColumnError error) : base(error) { }

	public static implicit operator GetNullableIntegerResult(long value) {
		return new(OneOf<long, None>.FromT0(value));
	}

	public static implicit operator GetNullableIntegerResult(None none) {
		return new(OneOf<long, None>.FromT1(none));
	}

	public static implicit operator GetNullableIntegerResult(GetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator GetNullableIntegerResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetNullableIntegerResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetRealResult : ValueResult<double, GetColumnError> {

	public GetRealResult(double value) : base(value) { }

	public GetRealResult(GetColumnError error) : base(error) { }

	public static implicit operator GetRealResult(double value) {
		return new(value);
	}

	public static implicit operator GetRealResult(GetColumnError error) {
		return new(error);
	}

	public static implicit operator GetRealResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetRealResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator GetRealResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetNullableRealResult : ValueResult<OneOf<double, None>, GetNullableColumnError> {

	public GetNullableRealResult(OneOf<double, None> value) : base(value) { }

	public GetNullableRealResult(GetNullableColumnError error) : base(error) { }

	public static implicit operator GetNullableRealResult(double value) {
		return new(OneOf<double, None>.FromT0(value));
	}

	public static implicit operator GetNullableRealResult(None none) {
		return new(OneOf<double, None>.FromT1(none));
	}

	public static implicit operator GetNullableRealResult(GetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator GetNullableRealResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetNullableRealResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetTextResult : Result<string, GetColumnError> {

	public GetTextResult(string value) : base(value) { }

	public GetTextResult(GetColumnError error) : base(error) { }

	public static implicit operator GetTextResult(string value) {
		return new(value);
	}

	public static implicit operator GetTextResult(GetColumnError error) {
		return new(error);
	}

	public static implicit operator GetTextResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetTextResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator GetTextResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetNullableTextResult : ValueResult<OneOf<string, None>, GetNullableColumnError> {

	public GetNullableTextResult(OneOf<string, None> value) : base(value) { }

	public GetNullableTextResult(GetNullableColumnError error) : base(error) { }

	public static implicit operator GetNullableTextResult(string value) {
		return new(OneOf<string, None>.FromT0(value));
	}

	public static implicit operator GetNullableTextResult(None none) {
		return new(OneOf<string, None>.FromT1(none));
	}

	public static implicit operator GetNullableTextResult(GetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator GetNullableTextResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetNullableTextResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetBlobResult : Result<byte[], GetColumnError> {

	public GetBlobResult(byte[] value) : base(value) { }

	public GetBlobResult(GetColumnError error) : base(error) { }

	public static implicit operator GetBlobResult(byte[] value) {
		return new(value);
	}

	public static implicit operator GetBlobResult(GetColumnError error) {
		return new(error);
	}

	public static implicit operator GetBlobResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetBlobResult(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator GetBlobResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetNullableBlobResult : ValueResult<OneOf<byte[], None>, GetNullableColumnError> {

	public GetNullableBlobResult(OneOf<byte[], None> value) : base(value) { }

	public GetNullableBlobResult(GetNullableColumnError error) : base(error) { }

	public static implicit operator GetNullableBlobResult(byte[] value) {
		return new(OneOf<byte[], None>.FromT0(value));
	}

	public static implicit operator GetNullableBlobResult(None none) {
		return new(OneOf<byte[], None>.FromT1(none));
	}

	public static implicit operator GetNullableBlobResult(GetNullableColumnError error) {
		return new(error);
	}

	public static implicit operator GetNullableBlobResult(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetNullableBlobResult(ColumnTypeError error) {
		return new(error);
	}

}



public record GetEnumResult<TEnum> : ValueResult<TEnum, GetEnumColumnError> where TEnum : struct, Enum {

	public GetEnumResult(TEnum value) : base(value) { }

	public GetEnumResult(GetEnumColumnError error) : base(error) { }

	public static implicit operator GetEnumResult<TEnum>(TEnum value) {
		return new(value);
	}

	public static implicit operator GetEnumResult<TEnum>(GetEnumColumnError error) {
		return new(error);
	}

	public static implicit operator GetEnumResult<TEnum>(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetEnumResult<TEnum>(ColumnNullError error) {
		return new(error);
	}

	public static implicit operator GetEnumResult<TEnum>(ColumnTypeError error) {
		return new(error);
	}

	public static implicit operator GetEnumResult<TEnum>(NotEnumValueError error) {
		return new(error);
	}

}



public record GetNullableEnumResult<TEnum> : ValueResult<OneOf<TEnum, None>, GetNullableEnumColumnError> where TEnum : struct, Enum {

	public GetNullableEnumResult(OneOf<TEnum, None> value) : base(value) { }

	public GetNullableEnumResult(GetNullableEnumColumnError error) : base(error) { }

	public static implicit operator GetNullableEnumResult<TEnum>(TEnum value) {
		return new(OneOf<TEnum, None>.FromT0(value));
	}

	public static implicit operator GetNullableEnumResult<TEnum>(None none) {
		return new(OneOf<TEnum, None>.FromT1(none));
	}

	public static implicit operator GetNullableEnumResult<TEnum>(GetNullableEnumColumnError error) {
		return new(error);
	}

	public static implicit operator GetNullableEnumResult<TEnum>(ColumnAccessError error) {
		return new(error);
	}

	public static implicit operator GetNullableEnumResult<TEnum>(ColumnTypeError error) {
		return new(error);
	}

	public static implicit operator GetNullableEnumResult<TEnum>(NotEnumValueError error) {
		return new(error);
	}

}



public record ColumnAccessError : Error {

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

public record ColumnNullError : Error {

	public required string ColumnName { get; init; }

	[SetsRequiredMembers]
	public ColumnNullError(string columnName) {
		ColumnName = columnName;
	}

}

public record ColumnTypeError : Error {

	public required string ColumnName { get; init; }

	public required string ExpectedType { get; init; }

	public required string ActualType { get; init; }

	[SetsRequiredMembers]
	public ColumnTypeError(string columnName, Type expectedType, Type actualType) {

		ColumnName = columnName;
		ExpectedType = expectedType.Name;
		ActualType = actualType.Name;
	}

}

public record NotEnumValueError : Error {

	public required string ColumnName { get; init; }

	public required string Value { get; init; }

	[SetsRequiredMembers]
	public NotEnumValueError(string columnName, string value) {
		ColumnName = columnName;
		Value = value;
	}

}