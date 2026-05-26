using Comms.Dtos;
using Database.Results.Event;
using Database.Results.GameSpec;
using Database.Results.MatchData;
using Database.Results.Scout;
using Domain.GameSpecification;

namespace Database;



public interface IDataStore {

	public Task<GetGameSpecsResult> GetGameSpecs();
	
	public Task<AddNewGameSpecResult> AddNewGameSpec();

	public Task<ImportGameSpecResult> ImportGameSpec();

	public Task<DeleteGameSpecResult> DeleteGameSpec();



	public Task<GetEventsResult> GetEvents();

	public Task<AddNewEventResult> AddNewEvent();

	public Task<ImportEventResult> ImportEvent();

	public Task<DeleteEventResult> DeleteEvent();



	public Task<GetMatchDataResult> GetMatchData(GameSpec game, bool ignoreMajorVersion = false, bool ignoreMinorVersion = true, bool ignorePatchVersion = true);

	public Task<AddNewMatchDataResult> AddNewMatchData(NewMatchDataDto newMatchDataDto);

	public Task<ImportMatchDataResult> ImportMatchData(ImportMatchDataDto importMatchDataDto);

	public Task<AddNewEditedMatchDataResult> AddNewEditedMatchData(NewMatchDataDto newMatchDataDto);

	public Task<ImportEditedMatchDataResult> ImportEditedMatchData(ImportMatchDataDto importMatchDataDto);

	public Task<DeleteMatchDataResult> DeleteMatchData(ImportMatchDataDto importMatchData);

	public Task<DeleteAllMatchDataResult> DeleteAllMatchData();



	public Task<GetLastScoutResult> GetLastScout();

	public Task<SetLastScoutResult> SetLastScout(string scoutName);



	// TODO: consider storing errors and sharing them across devices 

}

public interface IDataStoreCreator {

	public Task<IDataStore?> Create(string settings); // TODO make this Result<> instead of nullable

}