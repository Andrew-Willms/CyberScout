using Comms.Dtos;
using Comms.Serialization;
using OneOf;
using Willmsy.AsyncTryResult;

namespace Database.Results.MatchData;




public record GetMatchDataResult : AsyncTryResult<List<EditGraph>, GetAllMatchDataError> {

	public GetMatchDataResult(List<EditGraph> value) : base(value) { }

	public GetMatchDataResult(GetAllMatchDataError error) : base(error) { }

	public static implicit operator GetMatchDataResult(List<EditGraph> value) {
		return new(value);
	}

	public static implicit operator GetMatchDataResult(GetAllMatchDataError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(BeginTransactionError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(ReadDataError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(ColumnReadError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(NullableColumnReadError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(ParentsFromTextError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(MatchDataDeserializationError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(CreateMatchDataDtoError error) {
		return new(error);
	}

	public static implicit operator GetMatchDataResult(CreateEditGraphError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class GetAllMatchDataError : OneOfBase<
	BeginTransactionError,
	ReadDataError,
	ColumnReadError,
	NullableColumnReadError,
	ParentsFromTextError,
	MatchDataDeserializationError,
	CreateMatchDataDtoError,
	CreateEditGraphError
>;