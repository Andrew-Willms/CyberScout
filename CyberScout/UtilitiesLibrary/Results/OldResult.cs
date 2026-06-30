using System.Diagnostics.CodeAnalysis;
using UtilitiesLibrary.Optional;

namespace UtilitiesLibrary.Results;



public abstract class OldSuccess;

public abstract class OldError {

	public virtual string Message { get; init; } = string.Empty;

	public Optional<OldError> InnerError { get; init; } = Optional.Optional.NoValue;

	public OldError() { }

	public OldError(string message) {
		Message = message;
	}

	public OldError(OldError innerOldError) {
		InnerError = innerOldError.Optionalize();
	}

	public OldError(string message, OldError innerOldError) {
		Message = message;
		InnerError = innerOldError.Optionalize();
	}

}



public interface IOldResult {

	public class OldSuccess : Results.OldSuccess, IOldResult;

	public class OldError : Results.OldError, IOldResult;

}

public interface IOldResult<T> {

	public class OldSuccess : Results.OldSuccess, IOldResult<T> {

		public required T Value { get; init; }

		public static implicit operator OldSuccess(T value) {
			return new() { Value = value };
		}

		public OldSuccess() { }

		[SetsRequiredMembers]
		public OldSuccess(T value) {
			Value = value;
		}

	}

	public class OldError : Results.OldError, IOldResult<T> {

		public OldError() { }

		public OldError(string message) : base(message) { }

		public OldError(Results.OldError innerOldError) : base(innerOldError) { }

		public OldError(string message, Results.OldError innerOldError) : base(message, innerOldError) { }

	}

}