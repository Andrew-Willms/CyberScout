using System;

namespace UtilitiesLibrary.Optional;



public class Optional {

	public static Optional NoValue { get; } = new();

	private Optional() { }

}



public record Optional<T> : IEquatable<Optional<T>> {

	public T Value => !HasValue ? throw new EvaluatingValuelessOptionalException() : field;

	public bool HasValue { get; }

	private Optional() {

		HasValue = false;
		Value = default!;
	}

	public Optional(T value) {

		HasValue = true;
		Value = value;
	}

	private static readonly Optional<T> NoValue = new();

	public static implicit operator Optional<T>(T value) {
		return new(value);
	}

	public static implicit operator Optional<T>(Optional _) {
		return NoValue;
	}

}



public class EvaluatingValuelessOptionalException : InvalidOperationException;