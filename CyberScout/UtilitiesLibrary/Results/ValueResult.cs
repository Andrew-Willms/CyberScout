using System.Diagnostics.CodeAnalysis;

namespace UtilitiesLibrary.Results;



/// <summary> Intended to be used as a return type for methods that may return a value or an error. </summary>
/// <typeparam name="TValue"> The type of the value to be returned. Must be a reference type. </typeparam>
public record ValueResult<TValue> where TValue : struct {

	/// <summary>
	/// If this <see cref="ValueResult{TValue}">ValueResult</see> represents a success, returns a value, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="true"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="false"/>.
	/// </summary>
	public Box<TValue>? Value { get; }

	/// <summary>
	/// If this <see cref="ValueResult{TValue}">ValueResult</see> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="true"/>.
	/// </summary>
	public Error? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="ValueResult{TValue}">ValueResult</see> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is not <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="ValueResult{TValue}">ValueResult</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> not is <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> Initializes a new <see cref="ValueResult{TValue}">ValueResult</see> record that represents a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public ValueResult(TValue value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="ValueResult{TValue}">ValueResult</see> record that represents a success. </summary>
	/// <param name="value"> The pre-boxed value to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public ValueResult(Box<TValue> value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="ValueResult{TValue}">ValueResult</see> record that represents an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public ValueResult(Error error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to a <see cref="ValueResult{TValue}">ValueResult</see> representing a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue>(TValue value) {
		return new(value);
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to a <see cref="ValueResult{TValue}">ValueResult</see> representing a success. </summary>
	/// <param name="value"> The pre-boxed value to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue>(Box<TValue> value) {
		return new(value);
	}

	/// <summary> Implicit conversion from a <see cref="Results.Error"/> to a <see cref="ValueResult{TValue}">ValueResult</see> representing an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="ValueResult{TValue}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue>(Error error) {
		return new(error);
	}

}



/// <summary> Intended to be used as a return type for methods that may return a value or a custom error. </summary>
/// <typeparam name="TValue"> The type of the value to be returned. Must be a reference type. </typeparam>
/// <typeparam name="TError"> The type of the error to be returned. Must be a reference type. </typeparam>
public record ValueResult<TValue, TError>
	where TValue : struct
	where TError : class {

	/// <summary>
	/// If this <see cref="ValueResult{TValue,TError}">ValueResult</see> represents a success, returns a value, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="true"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="false"/>.
	/// </summary>
	public Box<TValue>? Value { get; }

	/// <summary>
	/// If this <see cref="ValueResult{TValue,TError}">ValueResult</see> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="true"/>.
	/// </summary>
	public TError? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="ValueResult{TValue,TError}">ValueResult</see> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is not <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="ValueResult{TValue,TError}">ValueResult</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> not is <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> Initializes a new <see cref="ValueResult{TValue,TError}">ValueResult</see> record that represents a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public ValueResult(TValue value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="ValueResult{TValue,TError}">ValueResult</see> record that represents a success. </summary>
	/// <param name="value"> The pre-boxed value to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public ValueResult(Box<TValue> value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="ValueResult{TValue,TError}">ValueResult</see> record that represents an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public ValueResult(TError error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to an <see cref="ValueResult{TValue,TError}">ValueResult</see> representing a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue, TError>(TValue value) {
		return new(value);
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to an <see cref="ValueResult{TValue,TError}">ValueResult</see> representing a success. </summary>
	/// <param name="value"> The pre-boxed value to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue, TError>(Box<TValue> value) {
		return new(value);
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TError"/> to an <see cref="ValueResult{TValue,TError}">ValueResult</see> representing an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="ValueResult{TValue,TError}">ValueResult</see>. </param>
	public static implicit operator ValueResult<TValue, TError>(TError error) {
		return new(error);
	}

}