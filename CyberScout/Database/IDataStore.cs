using Comms.Dtos;
using Database.Results;
using UtilitiesLibrary.Results;

namespace Database;



public interface IDataStoreCreator {

	public Task<Result<IDataStore>> Create(string settings);

}



public interface IDataStore {

	public Task<Result<List<GameDto>>> GetGameSpecs();
	
	public Task<Result<GameDto>> AddNewGameSpec();

	public Task<Result> ImportGameSpec();

	public Task<Result> DeleteGameSpec();



	public Task<Result<List<InternalEventDto>>> GetEvents();

	public Task<Result<InternalEventDto>> AddNewEvent();

	public Task<Result> ImportEvent();

	public Task<Result> DeleteEvent();



	public Task<Result<List<MatchDataDto>>> GetMatchDataFromGame(GameDto gameDto);

	public Task<Result<MatchDataDto>> AddNewMatchData(NewMatchDataDto newMatchDataDto);

	public Task<Result<MatchDataDto>> AddEditedMatchData(EditedMatchDataDto editedMatchDataDto);

	public Task<Result> ImportMatchData(MatchDataDto importMatchDataDto);

	public Task<Result> DeleteMatchData(MatchDataDto matchDataToDelete);

	public Task<Result> DeleteMatchDataFromEvent(string eventCode);

	public Task<Result> DeleteMatchDataFromGame(GameDto gameDto);

	public Task<Result> DeleteAllMatchData();



	public Task<Result<string>> GetLastScout();

	public Task<Result> SetLastScout(string scoutName);



	// TODO: consider storing errors and sharing them across devices 

}