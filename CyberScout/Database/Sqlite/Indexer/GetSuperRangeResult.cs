using System.Diagnostics.CodeAnalysis;
using Database.Results;
using OneOf;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite.Indexer;



public record GetSuperRangeResult : AsyncTryResult<SuperRange, GetSuperRangeError> {

	public GetSuperRangeResult(SuperRange value) : base(value) { }

	public GetSuperRangeResult(GetSuperRangeError error) : base(error) { }

	public static implicit operator GetSuperRangeResult(SuperRange value) {
		return new(value);
	}

	public static implicit operator GetSuperRangeResult(GetSuperRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetPrecedingRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetContainingRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetSubsequentRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(RangeOperationError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class GetSuperRangeError : OneOfBase<
	SuperRange,
	GetPrecedingRangeError,
	GetContainingRangeError,
	GetSubsequentRangeError,
	RangeOperationError
>;

public record GetPrecedingRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetPrecedingRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}

public record GetContainingRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetContainingRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}

public record GetSubsequentRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetSubsequentRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}