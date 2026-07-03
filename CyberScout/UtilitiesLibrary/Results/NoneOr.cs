using System.Diagnostics.CodeAnalysis;

namespace UtilitiesLibrary.Results;



/// <summary>
/// Intended to be used as the return type of methods that may return a value or an error.
/// </summary>
/// <typeparam name="TValue">The type of the value to be returned. Must be a reference type.</typeparam>
public record NoneOr<TValue> where TValue : class {

	/// <summary>
	/// If this <see cref="NoneOr{TValue}">NoneOr</see> represents a success, returns a value, otherwise returns <see langword="null"/>.<br/>
	/// This property is not <see langword="null"/> if and only if <see cref="IsNone"/> is <see langword="false"/>.
	/// </summary>
	public TValue? Value { get; }

	/// <summary>
	/// Indicates if this <see cref="NoneOr{TValue}">NoneOr</see> represents an error.<br/>
	/// When <see langword="true"/>, <see cref="Value">Value</see> is <see langword="null"/> and <see cref="Error">Error</see> is not <see langword="null"/>.<br/>
	/// When <see langword="false"/>, <see cref="Value">Value</see> not is <see langword="null"/> and <see cref="Error">Error</see> is <see langword="null"/>.<br/>
	/// </summary>
	[MemberNotNullWhen(false, nameof(Value))]
	public bool IsNone { get; }

	// todo
	/// <summary>
	/// 
	/// </summary>
	public static readonly NoneOr<TValue> None = new();

	public NoneOr() {
		IsNone = true;
	}

	/// <summary>
	/// Initializes a new <see cref="NoneOr{TValue}">NoneOr</see> record that represents a success.
	/// </summary>
	/// <param name="value">The value to be stored by the <see cref="NoneOr{TValue}">NoneOr</see>.</param>
	public NoneOr(TValue value) {
		Value = value;
		IsNone = false;
	}

	// todo
	/// <summary>
	/// 
	/// </summary>
	public static implicit operator NoneOr<TValue>(None _) {
		return None;
	}

	/// <summary>
	/// Implicit conversion from an <see langword="object"/> of type <typeparamref name="TValue"/> to a <see cref="NoneOr{TValue}">NoneOr</see> representing a success.
	/// </summary>
	/// <param name="value">The value to be stored by the <see cref="NoneOr{TValue}">NoneOr</see>.</param>
	public static implicit operator NoneOr<TValue>(TValue value) {
		return new(value);
	}

}