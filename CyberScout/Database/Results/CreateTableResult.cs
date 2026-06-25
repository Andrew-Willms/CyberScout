using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results;


[GenerateOneOf]
public partial class CreateTableError : OneOfBase<ExecuteNonQueryAndExpectError>;

// TODO: make source generated
public record CreateTableResult : AsyncTryValueResult<Success, CreateTableError> {

	public CreateTableResult(Success value) : base(value) { }

	public CreateTableResult(ExecuteNonQueryAndExpectError error) : base(error) { }

	public static implicit operator CreateTableResult(Success value) {
		return new(value);
	}

	public static implicit operator CreateTableResult(ExecuteNonQueryAndExpectError? error) {
		return error is null ? new(Success.Instance) : new(error);
	}

}