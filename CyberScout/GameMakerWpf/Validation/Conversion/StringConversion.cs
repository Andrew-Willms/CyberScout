using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Math.Numbers;
using UtilitiesLibrary.Optional;
using Number = UtilitiesLibrary.Math.Numbers.Number;
using ValidationError = UtilitiesLibrary.Validation.Errors.ValidationError<GameMakerWpf.Domain.ErrorSeverity>;

namespace GameMakerWpf.Validation.Conversion;



public interface INumberConversionErrorSet {

	public ValidationError RequiresValueError { get; init; }

	public Func<char[], ValidationError> InvalidCharactersErrorGetter { get; init; }

	public Func<string, ValidationError> ValueTooLargeErrorGetter { get; init; }

}

public interface ISignedNumberConversionErrorSet : INumberConversionErrorSet {

	public ValidationError MinusSignMustBeAtStartError { get; init; }
	
	public Func<string, ValidationError> ValueTooNegativeErrorGetter { get; init; }

}

public interface IUnsignedNumberConversionErrorSet : INumberConversionErrorSet {

	public ValidationError CannotBeNegativeError { get; init; }

}

public interface IIntegerConversionErrorSet : INumberConversionErrorSet {

	public ValidationError MustBeIntegerError { get; init; }

}

public interface IFloatConversionErrorSet : INumberConversionErrorSet {

	public ValidationError TooManyDecimalPointsError { get; init; }

}



public class FloatConversionErrorSet : ISignedNumberConversionErrorSet, IFloatConversionErrorSet {

	public required ValidationError RequiresValueError { get; init; }

	public required Func<char[], ValidationError> InvalidCharactersErrorGetter { get; init; }

	public required Func<string, ValidationError> ValueTooLargeErrorGetter { get; init; }

	public required Func<string, ValidationError> ValueTooNegativeErrorGetter { get; init; }

	public required ValidationError TooManyDecimalPointsError { get; init; }

	public required ValidationError MinusSignMustBeAtStartError { get; init; }

}

public class IntegerConversionErrorSet : ISignedNumberConversionErrorSet, IIntegerConversionErrorSet {

	public required ValidationError RequiresValueError { get; init; }

	public required Func<char[], ValidationError> InvalidCharactersErrorGetter { get; init; }

	public required Func<string, ValidationError> ValueTooLargeErrorGetter { get; init; }

	public required Func<string, ValidationError> ValueTooNegativeErrorGetter { get; init; }

	public required ValidationError MinusSignMustBeAtStartError { get; init; }

	public required ValidationError MustBeIntegerError { get; init; }

}

public class WholeConversionErrorSet : IUnsignedNumberConversionErrorSet, IIntegerConversionErrorSet {

	public required ValidationError RequiresValueError { get; init; }

	public required Func<char[], ValidationError> InvalidCharactersErrorGetter { get; init; }

	public required Func<string, ValidationError> ValueTooLargeErrorGetter { get; init; }

	public required ValidationError MustBeIntegerError { get; init; }
	
	public required ValidationError CannotBeNegativeError { get; init; }

}



public static class StringConversion {

	private static (Optional<Number>, ReadOnlyList<ValidationError>) ToFloat(string inputString, FloatConversionErrorSet errorSet) {

		if (inputString.Length == 0) {
			return (Optional.NoValue, errorSet.RequiresValueError.ReadOnlyListify());
		}

		char[] invalidCharacters = inputString.Where(x => !char.IsDigit(x) && x != '-' && x != '.').ToArray();

		List<ValidationError> errors = new();

		if (invalidCharacters.Any()) {
			errors.Add(errorSet.InvalidCharactersErrorGetter(invalidCharacters));
		}

		if (inputString.Multiple('-') || (inputString.OnlyOne('-') && !inputString.StartsWith('-'))) {
			errors.Add(errorSet.MinusSignMustBeAtStartError);
		}

		if (inputString.Multiple('.')) {
			errors.Add(errorSet.TooManyDecimalPointsError);
		}

		if (errors.Any()) {
			return (Optional.NoValue, errors.ToReadOnly());
		}

		Number? number = Number.Parse(inputString);

		if (number is null) {
			throw new InvalidOperationException($"Somehow parsing the input as a {typeof(Number)} failed.");
		}

		return (number.Optionalize(), ReadOnlyList.Empty);
	}

	private static (Optional<Integer>, ReadOnlyList<ValidationError>) ToInteger(string inputString, IntegerConversionErrorSet errorSet) {

		if (inputString.Length == 0) {
			return (Optional.NoValue, errorSet.RequiresValueError.ReadOnlyListify());
		}

		char[] invalidCharacters = inputString.Where(x => !char.IsDigit(x) && x != '-' && x != '.').ToArray();

		List<ValidationError> errors = new();

		if (invalidCharacters.Any()) {
			errors.Add(errorSet.InvalidCharactersErrorGetter(invalidCharacters));
		}

		if (inputString.Multiple('-') || (inputString.OnlyOne('-') && !inputString.StartsWith('-'))) {
			errors.Add(errorSet.MinusSignMustBeAtStartError);
		}

		if (inputString.Contains('.')) {
			errors.Add(errorSet.MustBeIntegerError);
		}

		if (errors.Any()) {
			return (Optional.NoValue, errors.ToReadOnly());
		}

		Integer? integer = Integer.Parse(inputString);

		if (integer is null) {
			throw new InvalidOperationException($"Somehow parsing the input as a {typeof(Integer)} failed.");
		}

		return (integer.Optionalize(), ReadOnlyList.Empty);
	}

	private static (Optional<Whole>, ReadOnlyList<ValidationError>) ToWhole(string inputString, WholeConversionErrorSet errorSet) {

		if (inputString.Length == 0) {
			return (Optional.NoValue, errorSet.RequiresValueError.ReadOnlyListify());
		}

		char[] invalidCharacters = inputString.Where(x => !char.IsDigit(x)).ToArray();

		List<ValidationError> errors = new();

		if (invalidCharacters.Any()) {
			errors.Add(errorSet.InvalidCharactersErrorGetter(invalidCharacters));
		}

		if (inputString.Contains('-')) {
			errors.Add(errorSet.CannotBeNegativeError);
		}

		if (inputString.Contains('.')) {
			errors.Add(errorSet.MustBeIntegerError);
		}

		if (errors.Any()) {
			return (Optional.NoValue, errors.ToReadOnly());
		}

		Whole? natural = Whole.Parse(inputString);

		if (natural is null) {
			throw new InvalidOperationException($"Somehow parsing the input as a {typeof(Whole)} failed.");
		}

		return (natural.Optionalize(), ReadOnlyList.Empty);
	}



	private static (Optional<T>, ReadOnlyList<ValidationError>) ToFloatPrimitive<T>(
		string inputString, Func<Number, INumberToPrimitiveOldResult<T>> parser, FloatConversionErrorSet errorSet) where T : INumber<T> {

		(Optional<Number> option, ReadOnlyList<ValidationError> numberConversionErrors) = ToFloat(inputString, errorSet);

		if (!option.HasValue) {
			return (Optional.NoValue, numberConversionErrors);
		}

		Number number = option.Value;
		INumberToPrimitiveOldResult<T> oldResult = parser.Invoke(number);

		return oldResult switch {

			INumberToPrimitiveOldResult<T>.ValueBelowMin => (Optional.NoValue, errorSet.ValueTooNegativeErrorGetter(inputString).ReadOnlyListify()),

			INumberToPrimitiveOldResult<T>.ValueAboveMax => (Optional.NoValue, errorSet.ValueTooLargeErrorGetter(inputString).ReadOnlyListify()),

			INumberToPrimitiveOldResult<T>.OldSuccess success => (success.Value.Optionalize(), ReadOnlyList.Empty),

			_ => throw new UnreachableException()
		};
	} 

	private static (Optional<T>, ReadOnlyList<ValidationError>) ToIntegerPrimitive<T>(
		string inputString, Func<Integer, IIntegerToPrimitiveOldResult<T>> parser, IntegerConversionErrorSet errorSet) where T : INumber<T> {

		(Optional<Integer> option, ReadOnlyList<ValidationError> numberConversionErrors) = ToInteger(inputString, errorSet);

		if (!option.HasValue) {
			return (Optional.NoValue, numberConversionErrors);
		}

		Integer integer = option.Value;
		IIntegerToPrimitiveOldResult<T> oldResult = parser.Invoke(integer);

		return oldResult switch {

			IIntegerToPrimitiveOldResult<T>.ValueBelowMin => (Optional.NoValue, errorSet.ValueTooNegativeErrorGetter(inputString).ReadOnlyListify()),

			IIntegerToPrimitiveOldResult<T>.ValueAboveMax => (Optional.NoValue, errorSet.ValueTooLargeErrorGetter(inputString).ReadOnlyListify()),

			IIntegerToPrimitiveOldResult<T>.OldSuccess success => (success.Value.Optionalize(), ReadOnlyList.Empty),

			_ => throw new UnreachableException()
		};
	}

	private static (Optional<T>, ReadOnlyList<ValidationError>) ToWholePrimitive<T>(
		string inputString, Func<Whole, IIntegerToPrimitiveOldResult<T>> parser, WholeConversionErrorSet errorSet) where T : INumber<T> {

		(Optional<Whole> option, ReadOnlyList<ValidationError> numberConversionErrors) = ToWhole(inputString, errorSet);

		if (!option.HasValue) {
			return (Optional.NoValue, numberConversionErrors);
		}
		
		Whole whole = option.Value;
		IIntegerToPrimitiveOldResult<T> oldResult = parser.Invoke(whole);

		return oldResult switch {

			IIntegerToPrimitiveOldResult<T>.ValueBelowMin => (Optional.NoValue, errorSet.CannotBeNegativeError.ReadOnlyListify()),

			IIntegerToPrimitiveOldResult<T>.ValueAboveMax => (Optional.NoValue, errorSet.ValueTooLargeErrorGetter(inputString).ReadOnlyListify()),

			IIntegerToPrimitiveOldResult<T>.OldSuccess success => (success.Value.Optionalize(), ReadOnlyList.Empty),

			_ => throw new UnreachableException()
		};
	}



	public static (Optional<byte>, ReadOnlyList<ValidationError>) ToByte(string inputString, WholeConversionErrorSet errorSet) {

		return ToWholePrimitive(inputString, Whole.ToByte, errorSet);
	}

	public static (Optional<ushort>, ReadOnlyList<ValidationError>) ToUshort(string inputString, WholeConversionErrorSet errorSet) {

		return ToWholePrimitive(inputString, Whole.ToUshort, errorSet);
	}

	public static (Optional<uint>, ReadOnlyList<ValidationError>) ToUint(string inputString, WholeConversionErrorSet errorSet) {

		return ToWholePrimitive(inputString, Whole.ToUint, errorSet);
	}

	public static (Optional<ulong>, ReadOnlyList<ValidationError>) ToUlong(string inputString, WholeConversionErrorSet errorSet) {

		return ToWholePrimitive(inputString, Whole.ToUlong, errorSet);
	}



	public static (Optional<short>, ReadOnlyList<ValidationError>) ToShort(string inputString, IntegerConversionErrorSet errorSet) {

		return ToIntegerPrimitive(inputString, Integer.ToShort, errorSet);
	}

	public static (Optional<int>, ReadOnlyList<ValidationError>) ToInt(string inputString, IntegerConversionErrorSet errorSet) {

		return ToIntegerPrimitive(inputString, Integer.ToInt, errorSet);
	}

	public static (Optional<long>, ReadOnlyList<ValidationError>) ToLong(string inputString, IntegerConversionErrorSet errorSet) {

		return ToIntegerPrimitive(inputString, Integer.ToLong, errorSet);
	}



	public static (Optional<float>, ReadOnlyList<ValidationError>) ToFloat32(string inputString, FloatConversionErrorSet errorSet) {

		return ToFloatPrimitive(inputString, Number.ToFloat, errorSet);
	}

	public static (Optional<double>, ReadOnlyList<ValidationError>) ToFloat64(string inputString, FloatConversionErrorSet errorSet) {

		return ToFloatPrimitive(inputString, Number.ToDouble, errorSet);
	}

}