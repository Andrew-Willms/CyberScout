using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results.Scout;



public record SetLastScoutResult : AsyncTryValueResult<Success, SetLastScoutError> {

	public SetLastScoutResult(Success value) : base(value) { }

	public SetLastScoutResult(SetLastScoutError error) : base(error) { }

	public static implicit operator SetLastScoutResult(Success value) {
		return new(value);
	}

	public static implicit operator SetLastScoutResult(SetLastScoutError error) {
		return new(error);
	}

	public static implicit operator SetLastScoutResult(ExecuteNonQueryAndExpectError error) {
		return new SetLastScoutError(error);
	}

}

public record SetLastScoutError(ExecuteNonQueryAndExpectError Error);