using System.Collections.Generic;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



/// <summary>
/// Represents a group of matches within an event schedule. For example, the qualification matches of an event.
/// </summary>
public record MatchGroup {

	/// <summary>
	/// The name of a group of matches. For example, "Qualification".
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// The format each <see cref="ScheduledMatch"/> in the <see cref="MatchGroup"/> must follow.
	/// </summary>
	public required MatchFormat Format { get; init; }

	/// <summary>
	/// The scheduling information for each match in the <see cref="MatchGroup"/>.
	/// </summary>
	public required ReadOnlyList<ScheduledMatch> Matches { get; init; }

	private MatchGroup() { }

	public static Result<MatchGroup, CreateEventScheduleError> Create(string name, MatchFormat format, List<ScheduledMatch> matches) {

		ReadOnlyList<string> duplicates = matches.Select(match => match.Name).ToReadOnly();
		if (duplicates.Count != 0) {
			return new DuplicateMatchName { Duplicates = duplicates };
		}

		foreach (ScheduledMatch match in matches) {
			if (!match.MatchesFormat(format)) {
				return new MatchHasWrongFormat {
					RequiredMatchFormat = format,
					ViolatingMatch = match
				};
			}
		}

		return new MatchGroup {
			Name = name,
			Format = format,
			Matches = matches.ToReadOnly()
		};
	}

}