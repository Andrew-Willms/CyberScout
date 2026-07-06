using Comms.Dtos.Event;
using Comms.Dtos.Game;
using Comms.Dtos.Match;
using Database.Domain;
using UtilitiesLibrary.Results;

namespace Database;



public interface IDataStoreCreator {

	public Task<Result<IDataStore>> Create(string settings);

}



public interface IDataStore {

	public Task<Result<List<GameDto>>> GetGameSpecs();
	
	public Task<Result<GameDto>> AddNewGameSpec(NewGameDto newGameDto);

	public Task<Result> ImportGameSpec(GameDto gameDto);

	public Task<Result> DeleteGameData(GameDto gameDto);

	public Task<Result> DeleteAllGameData();



	public Task<Result<List<InternalEventDto>>> GetEvents();

	public Task<Result<InternalEventDto>> AddNewEvent(NewEventDto newEventDto);

	public Task<Result> ImportEvent(EventDto eventDto);

	public Task<Result> DeleteEventData(EventDto eventDto);

	public Task<Result> DeleteAllEventData();



	public Task<Result<List<MatchDto>>> GetMatchDataFromGame(GameDto gameDto);

	public Task<Result<MatchDto>> AddNewMatchData(NewMatchDto newMatchDto);

	public Task<Result<MatchDto>> AddEditedMatchData(EditedMatchDto editedMatchDto);

	public Task<Result> ImportMatchData(MatchDto matchDto);

	public Task<Result> DeleteMatchData(MatchDto matchDto);

	public Task<Result> DeleteMatchDataFromEvent(string eventCode);

	public Task<Result> DeleteMatchDataFromGame(GameDto gameDto);

	public Task<Result> DeleteAllMatchData();



	public Task<Result<string>> GetLastScout();

	public Task<Result> SetLastScout(string scoutName);



	// TODO: consider storing errors and sharing them across devices 

}