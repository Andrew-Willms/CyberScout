using Comms.Dtos;
using Comms.Serialization;
using OneOf;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class GetMatchDataResult : OneOfBase<
	List<MatchDataDto>,
	Exception,
	MatchDataDeserializationError,
	InvalidEditIdsError
>;


public class InvalidEditIdsError {

	public string? EditOfRecordFromDevice { get; }

	public int? EditOfRecord { get; }

	public InvalidEditIdsError(string editOfRecordFromDevice) {
		EditOfRecordFromDevice = editOfRecordFromDevice;
		EditOfRecord = null;
	}

	public InvalidEditIdsError(int editOfRecord) {
		EditOfRecordFromDevice = null;
		EditOfRecord = editOfRecord;
	}

};