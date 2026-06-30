using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using UtilitiesLibrary.Collections;

namespace UtilitiesLibrary.Math.Numbers;



public class Number : IEquatable<Number>, IComparable<Number> {

	public bool IsNegative { get; }

	/// <summary>
	/// How many digits are there to the right of the decimal point.
	/// </summary>
	private int DecimalPosition { get; }

	/// <summary>
	/// Stores the digits of the number from least to most significant.
	/// </summary>
	private ReadOnlyList<Digit> Digits { get; }

	public bool IsInteger => DecimalPosition == 0;

	private int LargestDecimalPosition => Digits.Count - DecimalPosition;
	private int SmallestDecimalPosition => -DecimalPosition;



	private Number(bool isNegative, int decimalPosition, ReadOnlyList<Digit> digits) {
		IsNegative = isNegative;
		DecimalPosition = decimalPosition;
		Digits = digits;
	}



	public override string ToString() {
		throw new NotImplementedException();
	}



	private Digit GetDigitInPosition(int position) {
		return Digits[position + DecimalPosition];
	}



	private static Number FromINumber<T>(T value) where T : INumber<T> {

		bool isNegative = value < T.Zero;

		int placesRightOfDecimalPoint = 0;
		T valueDecimals = Numbers<T>.Abs(value - value % T.One);

		while (valueDecimals > Numbers<T>.TenToThe(-10)) {

			placesRightOfDecimalPoint++;
			valueDecimals *= Numbers<T>.Ten;
			valueDecimals -= valueDecimals % T.One;
		}

		List<Digit> digits = new();
		while (value >= T.One || value <= Numbers<T>.MinusOne) {

			digits.Add(Digit.GetOnesColumn(value));

			value /= Numbers<T>.Ten;
		}

		return new(isNegative, placesRightOfDecimalPoint, digits.ToReadOnly());
	}

	public static implicit operator Number(byte value) => FromINumber(value);
	public static implicit operator Number(ushort value) => FromINumber(value);
	public static implicit operator Number(uint value) => FromINumber(value);
	public static implicit operator Number(ulong value) => FromINumber(value);
	public static implicit operator Number(short value) => FromINumber(value);
	public static implicit operator Number(int value) => FromINumber(value);
	public static implicit operator Number(long value) => FromINumber(value);
	public static implicit operator Number(float value) => FromINumber(value);
	public static implicit operator Number(double value) => FromINumber(value);
	public static implicit operator Number(decimal value) => FromINumber(value);



	private INumberToPrimitiveOldResult<T> ToNumberPrimitive<T>(
		Number typeMinValue, Number typeMaxValue, Func<Digit, T> digitToT, Func<int, T> tenToThe) where T : INumber<T> {

		if (this < typeMinValue) {
			return new INumberToPrimitiveOldResult<T>.ValueBelowMin();
		}

		if (this > typeMaxValue) {
			return new INumberToPrimitiveOldResult<T>.ValueAboveMax();
		}

		T value = T.Zero;
		for (int position = SmallestDecimalPosition; position < LargestDecimalPosition; position++) {
			value += digitToT(Digits[position]) * tenToThe(position);
		}

		if (IsNegative) {
			value *= Numbers<T>.MinusOne;
		}

		return new INumberToPrimitiveOldResult<T>.OldSuccess { Value = value };
	}

	public static INumberToPrimitiveOldResult<byte> ToByte(Number number) {

		return number.ToNumberPrimitive(byte.MinValue, byte.MaxValue, Digit.ToINumber<byte>, i => (byte)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<ushort> ToUshort(Number number) {

		return number.ToNumberPrimitive(ushort.MinValue, ushort.MaxValue, Digit.ToINumber<ushort>, i => (ushort)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<uint> ToUint(Number number) {

		return number.ToNumberPrimitive(uint.MinValue, uint.MaxValue, Digit.ToINumber<uint>, i => (uint)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<ulong> ToUlong(Number number) {

		return number.ToNumberPrimitive(ulong.MinValue, ulong.MaxValue, Digit.ToINumber<ulong>, i => (ulong)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<short> ToShort(Number number) {

		return number.ToNumberPrimitive(short.MinValue, short.MaxValue, Digit.ToINumber<short>, i => (short)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<int> ToInt(Number number) {

		return number.ToNumberPrimitive(int.MinValue, int.MaxValue, Digit.ToINumber<int>, i => (int)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<long> ToLong(Number number) {

		return number.ToNumberPrimitive(long.MinValue, long.MaxValue, Digit.ToINumber<long>, i => (long)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<float> ToFloat(Number number) {

		return number.ToNumberPrimitive(float.MinValue, float.MaxValue, Digit.ToINumber<float>, i => (float)System.Math.Pow(10, i));
	}

	public static INumberToPrimitiveOldResult<double> ToDouble(Number number) {

		return number.ToNumberPrimitive(double.MinValue, double.MaxValue, Digit.ToINumber<double>, i => System.Math.Pow(10, i));
	}



	public override int GetHashCode() {
		return HashCode.Combine(IsNegative, DecimalPosition, Digits);
	}

	public override bool Equals(object? obj) {
		return obj is Number other && Equals(other);
	}

	public bool Equals(Number? other) {

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return other is not null &&
			   IsNegative == other.IsNegative &&
			   DecimalPosition == other.DecimalPosition &&
			   Digits.SequenceEqual(other.Digits);
	}

	public int CompareTo(Number? other) {

		if (other is null) {
			throw new ArgumentNullException(nameof(other));
		}

		if (Equals(other)) {
			return 0;
		}

		if (IsNegative && !other.IsNegative) {
			return -1;
		}

		if (!IsNegative && other.IsNegative) {
			return 1;
		}

		if (LargestDecimalPosition > other.LargestDecimalPosition) {
			return IsNegative ? -1 : 1;
		}

		if (LargestDecimalPosition < other.LargestDecimalPosition) {
			return IsNegative ? 1 : -1;
		}

		int smallestDecimalPosition = System.Math.Min(SmallestDecimalPosition, other.SmallestDecimalPosition);
		for (int position = LargestDecimalPosition; position > smallestDecimalPosition; position--) {
			if (GetDigitInPosition(position) > other.GetDigitInPosition(position)) {
				return 1;
			}

			if (GetDigitInPosition(position) < other.GetDigitInPosition(position)) {
				return -1;
			}
		}

		throw new UnreachableException(
			"Since Equality is short-circuit it should not be possible that the two values are the same and execution should not reach this point.");
	}



	public static bool operator ==(Number left, Number right) {
		return left.Equals(right);
	}

	public static bool operator !=(Number left, Number right) {
		return !(left == right);
	}

	public static bool operator >(Number left, Number right) {
		return left.CompareTo(right) > 0;
	}

	public static bool operator <(Number left, Number right) {
		return left.CompareTo(right) < 0;
	}

	public static bool operator >=(Number left, Number right) {
		return left.CompareTo(right) >= 0;
	}

	public static bool operator <=(Number left, Number right) {
		return left.CompareTo(right) <= 0;
	}



	public static Number? Parse(string? text) {

		if (text is null || text.Length == 0) {
			return null;
		}

		if (text.Any(x => !char.IsDigit(x) && x is not '.' or '-')) {
			return null;
		}

		if (text.Multiple('.')) {
			return null;
		}

		if (text.Multiple('-') || (text.Contains('-') && !text.StartsWith('-'))) {
			return null;
		}

		// After this point the string should be a valid number.

		if (text.Where(x => x is not '.' or '-').All(x => x == '0')) {
			return new(false, 0, Digit.Zero.ReadOnlyListify());
		}

		bool isNegative = text.First() is '-';
		int decimalPosition = 0;
		List<Digit> digits = new();

		for (int i = text.Length - 1; i >= 0; i--) {

			if (text[i] is '.') {

				decimalPosition = text.Length - 1 - i;
				continue;
			}

			digits.Add(Digit.FromChar(text[i]));
		}

		return new(isNegative, decimalPosition, digits.ToReadOnly());
	}

}