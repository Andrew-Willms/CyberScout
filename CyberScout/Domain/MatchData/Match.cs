namespace Domain.MatchData;



public enum MatchType {
	Practice = 0,
	Qualification = 1,
	Elimination = 2,
	//QuarterFinal, // Todo add support for old style playoffs and custom tournament formats
	//SemiFinal,
	Final = 3
}

public record Match {

	public required uint MatchNumber { get; init; }

	public required uint ReplayNumber { get; init; }
	
	public required MatchType Type { get; init; }

}