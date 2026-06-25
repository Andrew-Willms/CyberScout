using Comms.Dtos;
using Database.Results.Event;
using Database.Results.GameSpec;
using Database.Results.MatchData;
using Database.Results.Scout;
using Domain.GameSpecification;

namespace Database;



public interface IDataStoreCreator {

	public Task<IDataStore?> Create(string settings); // TODO make this Result<> instead of nullable

}



public interface IDataStore {

	public Task<GetGameSpecsResult> GetGameSpecs();
	
	public Task<AddNewGameSpecResult> AddNewGameSpec();

	public Task<ImportGameSpecResult> ImportGameSpec();

	public Task<DeleteGameSpecResult> DeleteGameSpec();



	public Task<GetEventsResult> GetEvents();

	public Task<AddNewEventResult> AddNewEvent();

	public Task<ImportEventResult> ImportEvent();

	public Task<DeleteEventResult> DeleteEvent();



	public Task<GetMatchDataBtGameResult> GetMatchDataFromGame(GameSpec game, bool ignoreMajorVersion = false, bool ignoreMinorVersion = true, bool ignorePatchVersion = true);

	public Task<AddNewMatchDataResult> AddNewMatchData(NewMatchDataDto newMatchDataDto);

	public Task<AddNewEditedMatchDataResult> AddNewEditedMatchData(NewEditedMatchDataDto newEditedMatchDataDto);

	public Task<ImportMatchDataResult> ImportMatchData(MatchDataDto importMatchDataDto);

	public Task<DeleteMatchDataResult> DeleteMatchData(MatchDataDto matchDataToDelete);

	public Task<BulkDeleteMatchDataResult> DeleteMatchDataFromEvent(EventDto eventDto);

	public Task<BulkDeleteMatchDataResult> DeleteMatchDataFromGame(GameDto gameDto);

	public Task<BulkDeleteMatchDataResult> DeleteAllMatchData();



	public Task<GetLastScoutResult> GetLastScout();

	public Task<SetLastScoutResult> SetLastScout(string scoutName);



	// TODO: consider storing errors and sharing them across devices 

}