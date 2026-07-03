using Comms.Dtos;
using Domain.GameSpecification;
using UtilitiesLibrary.Results;

namespace Database;



public interface IDataStoreCreator {

	public Task<Result<IDataStore>> Create(string settings); // TODO make this Result<> instead of nullable

}



public interface IDataStore {

	public Task<Result<List<GameSpec>>> GetGameSpecs();
	
	public Task<Result> AddNewGameSpec();

	public Task<Result> ImportGameSpec();

	public Task<Result> DeleteGameSpec();



	// TODO Consider changing EventSchedule to EventDto or something
	public Task<Result<List<EventSchedule>>> GetEvents();

	public Task<Result> AddNewEvent();

	public Task<Result> ImportEvent();

	public Task<Result> DeleteEvent();



	public Task<Result<List<EditGraph>>> GetMatchDataFromGame(GameDto gameDto);

	public Task<Result> AddNewMatchData(NewMatchDataDto newMatchDataDto);

	public Task<Result> AddNewEditedMatchData(NewEditedMatchDataDto newEditedMatchDataDto);

	public Task<Result> ImportMatchData(MatchDataDto importMatchDataDto);

	public Task<Result> DeleteMatchData(MatchDataDto matchDataToDelete);

	public Task<Result> DeleteMatchDataFromEvent(EventDto eventDto);

	public Task<Result> DeleteMatchDataFromGame(GameDto gameDto);

	public Task<Result> DeleteAllMatchData();



	public Task<Result<string>> GetLastScout();

	public Task<Result> SetLastScout(string scoutName);



	// TODO: consider storing errors and sharing them across devices 

}