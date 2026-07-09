using Domain.Dtos.Game;
using UtilitiesLibrary.Results;

namespace Model;



// TODO move to a central design dock:
// we want the model to handle validation, otherwise the concept of public keys and authentication escapes into application code
public interface IModel {


	public Task<Result<List<GameDto>>> GetGameSpecs();

	public Task<Result<GameDto>> AddNewGameSpec(NewGameDto newGameDto);

	public Task<Result> ImportGameSpec(GameDto gameDto);

	public Task<Result> DeleteGameData(GameDto gameDto);

	public Task<Result> DeleteAllGameData();


}