using Microsoft.Data.Sqlite;
using OneOf;
using Willmsy.AsyncTryResult;

namespace SqliteUtilities;



public record IntegerScalarResult : AsyncTryValueResult<long, IntegerScalarError> {

	public IntegerScalarResult(long value) : base(value) { }

	public IntegerScalarResult(IntegerScalarError error) : base(error) { }

	public static implicit operator IntegerScalarResult(long value) {
		return new(value);
	}

	public static implicit operator IntegerScalarResult(IntegerScalarError error) {
		return new(error);
	}

	public static implicit operator IntegerScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator IntegerScalarResult(NullScalarError error) {
		return new(error);
	}

	public static implicit operator IntegerScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator IntegerScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class IntegerScalarError : OneOfBase<
	WrongScalarTypeError,
	NullScalarError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record NullableIntegerScalarResult : AsyncTryValueResult<OneOf<long, None>, NullableIntegerScalarError> {

	public NullableIntegerScalarResult(OneOf<long, None> value) : base(value) { }

	public NullableIntegerScalarResult(NullableIntegerScalarError error) : base(error) { }

	public static implicit operator NullableIntegerScalarResult(long value) {
		return new(OneOf<long, None>.FromT0(value));
	}

	public static implicit operator NullableIntegerScalarResult(None none) {
		return new(OneOf<long, None>.FromT1(none));
	}

	public static implicit operator NullableIntegerScalarResult(NullableIntegerScalarError error) {
		return new(error);
	}

	public static implicit operator NullableIntegerScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator NullableIntegerScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator NullableIntegerScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class NullableIntegerScalarError : OneOfBase<
	OneOf<long, None>,
	WrongScalarTypeError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record RealScalarResult : AsyncTryValueResult<double, RealScalarError> {

	public RealScalarResult(double value) : base(value) { }

	public RealScalarResult(RealScalarError error) : base(error) { }

	public static implicit operator RealScalarResult(double value) {
		return new(value);
	}

	public static implicit operator RealScalarResult(RealScalarError error) {
		return new(error);
	}

	public static implicit operator RealScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator RealScalarResult(NullScalarError error) {
		return new(error);
	}

	public static implicit operator RealScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator RealScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class RealScalarError : OneOfBase<
	double,
	WrongScalarTypeError,
	NullScalarError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record NullableRealScalarResult : AsyncTryValueResult<OneOf<double, None>, NullableRealScalarError> {

	public NullableRealScalarResult(OneOf<double, None> value) : base(value) { }

	public NullableRealScalarResult(NullableRealScalarError error) : base(error) { }

	public static implicit operator NullableRealScalarResult(double value) {
		return new(OneOf<double, None>.FromT0(value));
	}

	public static implicit operator NullableRealScalarResult(None none) {
		return new(OneOf<double, None>.FromT1(none));
	}

	public static implicit operator NullableRealScalarResult(NullableRealScalarError error) {
		return new(error);
	}

	public static implicit operator NullableRealScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator NullableRealScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator NullableRealScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class NullableRealScalarError : OneOfBase<
	OneOf<double, None>,
	WrongScalarTypeError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record TextScalarResult : AsyncTryResult<string, TextScalarError> {

	public TextScalarResult(string value) : base(value) { }

	public TextScalarResult(TextScalarError error) : base(error) { }

	public static implicit operator TextScalarResult(string value) {
		return new(value);
	}

	public static implicit operator TextScalarResult(TextScalarError error) {
		return new(error);
	}

	public static implicit operator TextScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator TextScalarResult(NullScalarError error) {
		return new(error);
	}

	public static implicit operator TextScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator TextScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class TextScalarError : OneOfBase<
	string,
	WrongScalarTypeError,
	NullScalarError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record NullableTextScalarResult : AsyncTryValueResult<OneOf<string, None>, NullableTextScalarError> {

	public NullableTextScalarResult(OneOf<string, None> value) : base(value) { }

	public NullableTextScalarResult(NullableTextScalarError error) : base(error) { }

	public static implicit operator NullableTextScalarResult(string value) {
		return new(OneOf<string, None>.FromT0(value));
	}

	public static implicit operator NullableTextScalarResult(None none) {
		return new(OneOf<string, None>.FromT1(none));
	}

	public static implicit operator NullableTextScalarResult(NullableTextScalarError error) {
		return new(error);
	}

	public static implicit operator NullableTextScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator NullableTextScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator NullableTextScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class NullableTextScalarError : OneOfBase<
	OneOf<string, None>,
	WrongScalarTypeError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record BlobScalarResult : AsyncTryResult<byte[], BlobScalarError> {

	public BlobScalarResult(byte[] value) : base(value) { }

	public BlobScalarResult(BlobScalarError error) : base(error) { }

	public static implicit operator BlobScalarResult(byte[] value) {
		return new(value);
	}

	public static implicit operator BlobScalarResult(BlobScalarError error) {
		return new(error);
	}

	public static implicit operator BlobScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator BlobScalarResult(NullScalarError error) {
		return new(error);
	}

	public static implicit operator BlobScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator BlobScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class BlobScalarError : OneOfBase<
	byte[],
	WrongScalarTypeError,
	NullScalarError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public record NullableBlobScalarResult : AsyncTryValueResult<OneOf<byte[], None>, NullableBlobScalarError> {

	public NullableBlobScalarResult(OneOf<byte[], None> value) : base(value) { }

	public NullableBlobScalarResult(NullableBlobScalarError error) : base(error) { }

	public static implicit operator NullableBlobScalarResult(byte[] value) {
		return new(OneOf<byte[], None>.FromT0(value));
	}

	public static implicit operator NullableBlobScalarResult(None none) {
		return new(OneOf<byte[], None>.FromT1(none));
	}

	public static implicit operator NullableBlobScalarResult(NullableBlobScalarError error) {
		return new(error);
	}

	public static implicit operator NullableBlobScalarResult(WrongScalarTypeError error) {
		return new(error);
	}

	public static implicit operator NullableBlobScalarResult(SqliteExceptionError error) {
		return new(error);
	}

	public static implicit operator NullableBlobScalarResult(NonSqliteExceptionError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class NullableBlobScalarError : OneOfBase<
	OneOf<byte[], None>,
	WrongScalarTypeError,
	SqliteExceptionError,
	NonSqliteExceptionError
>;

public static class Scalar {

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return an <see langword="long"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<IntegerScalarResult> ExecuteIntegerScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				long value => value,
				null => new NullScalarError { CommandText = command.CommandText },
				_ => new WrongScalarTypeError {
					ExpectedType = "long",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return an <see langword="long"/> or <see langword="null"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<NullableIntegerScalarResult> ExecuteNullableIntegerScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				long value => value,
				null => None.Instance,
				_ => new WrongScalarTypeError {
					ExpectedType = "long?",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see langword="double"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<RealScalarResult> ExecuteRealScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				double value => value,
				null => new NullScalarError { CommandText = command.CommandText },
				_ => new WrongScalarTypeError {
					ExpectedType = "double",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see langword="double"/> or <see langword="null"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<NullableRealScalarResult> ExecuteNullableRealScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				double value => value,
				null => None.Instance,
				_ => new WrongScalarTypeError {
					ExpectedType = "double?",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see langword="string"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<TextScalarResult> ExecuteTextScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				string value => value,
				null => new NullScalarError { CommandText = command.CommandText },
				_ => new WrongScalarTypeError {
					ExpectedType = "string",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see langword="string"/> or <see langword="null"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<NullableTextScalarResult> ExecuteNullableTextScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				string value => value,
				null => None.Instance,
				_ => new WrongScalarTypeError {
					ExpectedType = "string?",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see cref="byte"/> array. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<BlobScalarResult> ExecuteBlobScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				byte[] value => value,
				null => new NullScalarError { CommandText = command.CommandText },
				_ => new WrongScalarTypeError {
					ExpectedType = "string",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

	/// <summary> Executes an <see cref="SqliteCommand"/> that should return a <see cref="byte"/> array or <see langword="null"/>. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/>. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/>. </returns>
	public static async Task<NullableBlobScalarResult> ExecuteNullableBlobScalar(this SqliteCommand command) {

		try {
			object? result = await command.ExecuteScalarAsync();

			return result switch {
				byte[] value => value,
				null => None.Instance,
				_ => new WrongScalarTypeError {
					ExpectedType = "string?",
					ActualType = result.GetType().Name,
					CommandText = command.CommandText
				}
			};

		} catch (SqliteException exception) {
			return new SqliteExceptionError(exception, command);

		} catch (Exception exception) {
			return new NonSqliteExceptionError(exception, command);
		}
	}

}