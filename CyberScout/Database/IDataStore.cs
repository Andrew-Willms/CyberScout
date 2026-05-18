using Domain.GameSpecification;
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


public interface IDataStore {

	// If we want Initialize() as part of the interface we will likely want:
	// public interface IDataStore<TSelf> where TSelf : IDataStore<TSelf> { ... }
	//public static abstract Task<TSelf?> Initialize(string settings);

	public Task<List<GameSpec>> GetGameSpecs();

	//public Task<bool> AddGameSpec();



	public Task<GetMatchDataResult> GetMatchData();

	public Task<AddNewMatchDataResult> AddNewMatchData(CreateMatchDataDto matchData);

	public Task<AddMatchDataFromOtherDeviceResult> AddMatchDataFromOtherDevice(MatchDataDto matchData);

	public Task<bool> DeleteMatchData(MatchDataDto matchData);

	public Task<bool> DeleteAllMatchData();

	//public Task<List<EventSchedule>> GetEventSchedules();

	//public Task<bool> AddEventSchedule(EventSchedule eventSchedule);



	//public Task<DataToSend> GetDataToSend();

	//public Task<List<KnownDevice>> GetMostRecentFromDevice();

	//public Task<List<DomainError>> GetDomainErrors();



	public Task<string?> GetLastScout();

	public Task<bool> SetLastScout(string scoutName);

}

public interface IDataStoreCreator {

	public Task<IDataStore?> Create(string settings); // TODO make this Result<> instead of nullable

}