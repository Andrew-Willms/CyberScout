using OneOf;
using OneOf.Types;

namespace Database.Results.MatchData;



[GenerateOneOf]
public partial class ImportMatchDataResult : OneOfBase<
	Success,
	DuplicateMatchDataError,
	CouldNotRollBackError,
	Exception
>;



public class DuplicateMatchDataError;



public class CouldNotRollBackError {

	public required Exception FirstException { get; init; }

	public required Exception RollbackException { get; init; }

}