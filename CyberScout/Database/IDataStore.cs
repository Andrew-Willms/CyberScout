using Domain.GameSpecification;
using Domain.Serialization;

namespace Database;



public interface IDataStore {

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