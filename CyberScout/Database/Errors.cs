using Domain.Serialization;
using OneOf;
using OneOf.Types;

namespace Database;



[GenerateOneOf]
public partial class AddNewMatchDataResult : OneOfBase<Success, Exception>;



[GenerateOneOf]
public partial class AddMatchDataFromOtherDeviceResult : OneOfBase<Success, DuplicateMatchDataError, CouldNotRollBackError, Exception>;



public class DuplicateMatchDataError;



public class CouldNotRollBackError {

	public required Exception FirstException { get; init; }

	public required Exception RollbackException { get; init; }

}



[GenerateOneOf]
public partial class GetMatchDataResult : OneOfBase<List<MatchDataDto>, Exception, MatchDataDeserializationError, InvalidEditIdsError>;



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