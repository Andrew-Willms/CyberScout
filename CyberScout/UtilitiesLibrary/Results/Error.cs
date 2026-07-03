using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UtilitiesLibrary.Collections;

namespace UtilitiesLibrary.Results;



// IError is an interface so that it can work with classes, records, structs, and existing type hierarchies.
// Never mind. Error can't be an interface because user defined conversions are not allowed.
// This means you can't have an IError implicitly convert to a Result.
public abstract record Error;

// Being an interface would allow IError to be implemented on record, class, struct, and record struct types.
// Unfortunately interface cannot be used as the input or output type of implicit or explicit operators.
// The current implementation rellies all result types defining implicit operators from Error to said result type.
// This is what allows the body of a method that returns a result to have `return someError;` and have that error be converted into a result and accepted as a return value.
// One workaround would be to ask every type that implements IError to define conversions to every result type.
// Unfortunately this would only work for conversions to build in result types and would not be able to support conversions to user defined result types.
// To make these conversions also work with user defined result types you would have to limit user defined result types to global using aliases.
// This sounds like a pain in the ass and limits the functionality that can be accomplished with user defined result types.
// Why choose record over class, struct, or record struct.
//   - errors should probably be immutable data holding structures so the built-in value comparison and ToString functionality of records is a plus
//   - structs require an empty constructor which may not be desired for all error types
//   - errors may represent large chains of errors which could contain a large amount of data and take longer to copy than pass by reference


public record AdHocError : Error {

	public required string Message { get; init; }

	public Error? InternalError { get; init; }

	public ReadOnlyList<(string, string)> Data { get; }

	public AdHocError() {
		Data = ReadOnlyList.Empty;
	}

	public AdHocError(params List<(string, string)> data) {
		Data = data.ToReadOnly();
	}

	[SetsRequiredMembers]
	public AdHocError(string message, params List<(string, string)> data) {
		Message = message;
		InternalError = null;
		Data = data.ToReadOnly();
	}

	[SetsRequiredMembers]
	public AdHocError(string message, Error internalError, params List<(string, string)> data) {
		Message = message;
		InternalError = internalError;
		Data = data.ToReadOnly();
	}

}