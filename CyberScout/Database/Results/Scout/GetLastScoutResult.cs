using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Results.Scout;



public record GetLastScoutResult : AsyncTryResult<string, GetLastScoutError> {

	public GetLastScoutResult(string value) : base(value) { }

	public GetLastScoutResult(GetLastScoutError error) : base(error) { }

	public static implicit operator GetLastScoutResult(string value) {
		return new(value);
	}

	public static implicit operator GetLastScoutResult(GetLastScoutError error) {
		return new(error);
	}

	public static implicit operator GetLastScoutResult(TextScalarError error) {
		return new GetLastScoutError(error);
	}

}

public record GetLastScoutError(TextScalarError Error);