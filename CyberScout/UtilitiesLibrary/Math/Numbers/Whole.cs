using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using UtilitiesLibrary.Collections;

namespace UtilitiesLibrary.Math.Numbers;



public class Whole : IEquatable<Whole>, IComparable<Whole> {

	/// <summary>
	/// Stores the digits of the number from least to most significant.
	/// </summary>
	protected ReadOnlyList<Digit> Digits { get; }
	//protected int LargestDecimalPosition => Digits.Count - 1;



	protected Whole(ReadOnlyList<Digit> digits) {
		Digits = digits;
	}

	protected Whole(Whole whole) {
		Digits = whole.Digits;
	}



	private static Whole FromINumber<T>(T value) where T : INumber<T> {

		List<Digit> digits = new();

		while (value >= T.One) {

			digits.Add(Digit.GetOnesColumn(value));

			value /= Numbers<T>.Ten;
		}

		return new(digits.ToReadOnly());
	}

	public static implicit operator Whole(byte value) => FromINumber(value);
	public static implicit operator Whole(ushort value) => FromINumber(value);
	public static implicit operator Whole(uint value) => FromINumber(value);
	public static implicit operator Whole(ulong value) => FromINumber(value);
	public static explicit operator Whole(short value) => FromINumber(value);
	public static explicit operator Whole(int value) => FromINumber(value);
	public static explicit operator Whole(long value) => FromINumber(value);



	private IIntegerToPrimitiveOldResult<T> ToNumberPrimitive<T>(
		Whole typeMinValue, Whole typeMaxValue, Func<Digit, T> digitToT, Func<int, T> tenToThe) where T : INumber<T> {

		if (this < typeMinValue) {
			return new IIntegerToPrimitiveOldResult<T>.ValueBelowMin();
		}

		if (this > typeMaxValue) {
			return new IIntegerToPrimitiveOldResult<T>.ValueAboveMax();
		}

		T value = T.Zero;
		for (int position = 0; position < Digits.Count; position++) {
			value += digitToT(Digits[position]) * tenToThe(position);
		}

		return new IIntegerToPrimitiveOldResult<T>.OldSuccess { Value = value };
	}

	public static IIntegerToPrimitiveOldResult<byte> ToByte(Whole whole) {

		return whole.ToNumberPrimitive(byte.MinValue, byte.MaxValue, Digit.ToINumber<byte>, i => (byte)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<ushort> ToUshort(Whole whole) {

		return whole.ToNumberPrimitive(ushort.MinValue, ushort.MaxValue, Digit.ToINumber<ushort>, i => (ushort)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<uint> ToUint(Whole whole) {

		return whole.ToNumberPrimitive(uint.MinValue, uint.MaxValue, Digit.ToINumber<uint>, i => (uint)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<ulong> ToUlong(Whole whole) {

		return whole.ToNumberPrimitive(ulong.MinValue, ulong.MaxValue, Digit.ToINumber<ulong>, i => (ulong)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<short> ToShort(Whole whole) {

		return whole.ToNumberPrimitive(0, (Whole)short.MaxValue, Digit.ToINumber<short>, i => (short)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<int> ToInt(Whole whole) {

		return whole.ToNumberPrimitive(0, int.MaxValue, Digit.ToINumber<int>, i => (int)System.Math.Pow(10, i));
	}

	public static IIntegerToPrimitiveOldResult<long> ToLong(Whole whole) {

		return whole.ToNumberPrimitive(0, long.MaxValue, Digit.ToINumber<long>, i => (long)System.Math.Pow(10, i));
	}



	public override int GetHashCode() {
		return Digits.GetHashCode();
	}

	public override bool Equals(object? obj) {

		return obj is Whole other && Equals(other);
	}

	public bool Equals(Whole? other) {

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return other is not null && Digits.SequenceEqual(other.Digits);
	}

	public int CompareTo(Whole? other) {

		if (other is null) {
			throw new ArgumentNullException(nameof(other));
		}

		if (Equals(other)) {
			return 0;
		}

		if (Digits.Count > other.Digits.Count) {
			return 1;
		}

		if (Digits.Count < other.Digits.Count) {
			return -1;
		}

		for (int position = Digits.Count - 1; position >= 0; position--) {

			if (Digits[position] > other.Digits[position]) {
				return 1;
			}

			if (Digits[position] < other.Digits[position]) {
				return -1;
			}
		}

		throw new("Since Equality is short-circuit it should not be possible that the two values are the same and" +
				  " execution should not reach this point.");
	}



	public static bool operator ==(Whole left, Whole right) {
		return left.Equals(right);
	}

	public static bool operator !=(Whole left, Whole right) {
		return !left.Equals(right);
	}

	public static bool operator >(Whole left, Whole right) {
		return left.CompareTo(right) > 0;
	}

	public static bool operator <(Whole left, Whole right) {
		return left.CompareTo(right) < 0;
	}

	public static bool operator >=(Whole left, Whole right) {
		return left.CompareTo(right) >= 0;
	}

	public static bool operator <=(Whole left, Whole right) {
		return left.CompareTo(right) <= 0;
	}



	public static Whole? Parse(string? text) {

		if (text is null || text.Length == 0) {
			return null;
		}

		if (text.All(x => x == '0')) {
			return new(Digit.Zero.ReadOnlyListify());
		}

		text = text.TrimStart('0');

		List<Digit> digits = new();

		foreach (char character in text.Reverse()) {

			if (!char.IsDigit(character)) {
				return null;
			}

			digits.Add(Digit.FromChar(character));
		}

		return new(digits.ToReadOnly());
	}

}