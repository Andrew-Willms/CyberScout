using System.Diagnostics.CodeAnalysis;

namespace UtilitiesLibrary.Results;



/// <summary> Intended to be used as a return type for methods that may succeed or return an error. </summary>
public record Result {

	/// <summary>
	/// If this <see cref="Result"/> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="true"/>.
	/// </summary>
	public Error? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="Result"/> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Error">Error</see> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Error">Error</see> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="Result"/> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Error">Error</see> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Error">Error</see> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> An instance of <see cref="Result"/> representing a success. </summary>
	public static readonly Result Success = new();

	protected Result() {
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="Result"/> record that represents an error. </summary>
	/// <param name="error">The error to be stored by the <see cref="Result"/>.</param>
	public Result(Error error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an <see cref="Error"/> to an <see cref="Result"/> representing an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="Result"/>. </param>
	public static implicit operator Result(Error error) {
		return new(error);
	}

	public bool IsError([NotNullWhen(true)] out Error? error) {
		error = Error;
		return IsFailure;
	}

}



/// <summary> Intended to be used as a return type for methods that may return a value or an error. </summary>
/// <typeparam name="TValue"> The type of the value to be returned. Must be a reference type. </typeparam>
public record Result<TValue> where TValue : class {

	/// <summary>
	/// If this <see cref="Result{TValue}">Result</see> represents a success, returns a value, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="true"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="false"/>.
	/// </summary>
	public TValue? Value { get; }

	/// <summary>
	/// If this <see cref="Result{TValue}">Result</see> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="true"/>.
	/// </summary>
	public Error? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="Result{TValue}">Result</see> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is not <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="Result{TValue}">Result</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> not is <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> Initializes a new <see cref="Result{TValue}">Result</see> record that represents a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="Result{TValue}">Result</see>. </param>
	public Result(TValue value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="Result{TValue}">Result</see> record that represents an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="Result{TValue}">Result</see>. </param>
	public Result(Error error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to a <see cref="Result{TValue}">Result</see> representing a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="Result{TValue}">Result</see>. </param>
	public static implicit operator Result<TValue>(TValue value) {
		return new(value);
	}

	/// <summary> Implicit conversion from a <see cref="Results.Error"/> to a <see cref="Result{TValue}">Result</see> representing an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="Result{TValue}">Result</see>. </param>
	public static implicit operator Result<TValue>(Error error) {
		return new(error);
	}

	public bool IsValue([NotNullWhen(true)] out TValue? value) {
		value = Value;
		return IsSuccess;
	}

	public bool IsError([NotNullWhen(true)] out Error? error) {
		error = Error;
		return IsFailure;
	}

}



/// <summary> Intended to be used as a return type for methods that may return a value or a custom error. </summary>
/// <typeparam name="TValue"> The type of the value to be returned. Must be a reference type. </typeparam>
/// <typeparam name="TError"> The type of the error to be returned. Must be a reference type. </typeparam>
public record Result<TValue, TError>
	where TValue : class
	where TError : class {

	/// <summary>
	/// If this <see cref="Result{TValue,TError}">Result</see> represents a success, returns a value, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="true"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="false"/>.
	/// </summary>
	public TValue? Value { get; }

	/// <summary>
	/// If this <see cref="Result{TValue,TError}">Result</see> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess">IsSuccess</see> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure">IsFailure</see> is <see langword="true"/>.
	/// </summary>
	public TError? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="Result{TValue,TError}">Result</see> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is not <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="Result{TValue,TError}">Result</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Value"/> is <see langword="null"/> and <see cref="Error"/> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value"/> not is <see langword="null"/> and <see cref="Error"/> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Value))]
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> Initializes a new <see cref="Result{TValue,TError}">Result</see> record that represents a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="Result{TValue,TError}">Result</see>. </param>
	public Result(TValue value) {
		Value = value;
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="Result{TValue,TError}">Result</see> record that represents an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="Result{TValue,TError}">Result</see>. </param>
	public Result(TError error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TValue"/> to an <see cref="Result{TValue,TError}">Result</see> representing a success. </summary>
	/// <param name="value"> The value to be stored by the <see cref="Result{TValue,TError}">Result</see>. </param>
	public static implicit operator Result<TValue, TError>(TValue value) {
		return new(value);
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TError"/> to an <see cref="Result{TValue,TError}">Result</see> representing an error. </summary>
	/// <param name="error"> The error to be stored by the <see cref="Result{TValue,TError}">Result</see>. </param>
	public static implicit operator Result<TValue, TError>(TError error) {
		return new(error);
	}

	public bool IsValue([NotNullWhen(true)] out TValue? value) {
		value = Value;
		return IsSuccess;
	}

	public bool IsError([NotNullWhen(true)] out TError? error) {
		error = Error;
		return IsFailure;
	}

}



/// <summary> Intended to be used as a return type for methods that may succeed or return a custom error. </summary>
/// <typeparam name="TError"> The type of the error to be returned. Must be a reference type. </typeparam>
public record ResultCustomError<TError> where TError : class {

	/// <summary>
	/// If this <see cref="ResultCustomError{TError}">ResultCustomError</see> represents an error, returns the error, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsSuccess"/> is <see langword="false"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsFailure"/> is <see langword="true"/>.
	/// </summary>
	public TError? Error { get; }

	/// <summary>
	/// Indicates if this <see cref="ResultCustomError{TError}">ResultCustomError</see> represents a success.<br/>
	/// When <see langword="true"/>, <see cref="Error">Error</see> is <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Error">Error</see> is not <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// Indicates if this <see cref="ResultCustomError{TError}">ResultCustomError</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Error">Error</see> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Error">Error</see> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsFailure { get; }

	/// <summary> An instance of <see cref="ResultCustomError{TError}">ResultCustomError</see> representing a success. </summary>
	public static readonly ResultCustomError<TError> Success = new();

	/// <summary> Initializes a new <see cref="ResultCustomError{TError}">ResultCustomError</see> record that represents a success. </summary>
	protected ResultCustomError() {
		IsSuccess = true;
		IsFailure = false;
	}

	/// <summary> Initializes a new <see cref="ResultCustomError{TError}">ResultCustomError</see> record that represents an error. </summary>
	/// <param name="error">The error to be stored by the <see cref="ResultCustomError{TError}">ResultCustomError</see>.</param>
	public ResultCustomError(TError error) {
		Error = error;
		IsSuccess = false;
		IsFailure = true;
	}

	/// <summary> Implicit conversion from an instance of <typeparamref name="TError"/> to an <see cref="ResultCustomError{TError}">ResultCustomError</see> representing an error. </summary>
	/// <param name="error">The error to be stored by the <see cref="ResultCustomError{TError}">ResultCustomError</see>.</param>
	public static implicit operator ResultCustomError<TError>(TError error) {
		return new(error);
	}

	public bool IsError([NotNullWhen(true)] out TError? error) {
		error = Error;
		return IsFailure;
	}

}