namespace Domain.MatchData;



/// <summary>
/// Uniquely identifies a match of an event.
/// </summary>
public record Match {
	
	/// <summary>
	/// The name of the <see cref="MatchGroupName"/> the match is part of. For example, "Qualification".
	/// </summary>
	public required string MatchGroupName { get; init; }

	/// <summary>
	/// The name of the match within its <see cref="MatchGroupName"/>. For example, "Match 1".
	/// </summary>
	public required string MatchName { get; init; }

	/// <summary>
	/// The replay count of the match. For example, 0 for the original play of a match, 1 for the first time a match is replayed.
	/// </summary>
	public required uint ReplayNumber { get; init; }

}