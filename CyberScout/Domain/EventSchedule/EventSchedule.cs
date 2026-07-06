using System;
using System.Collections.Generic;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.EventSchedule;



public class EventSchedule {

	public required MatchFormat MatchFormat { get; init; }

	public required string Name { get; init; }

	public required string EventCode { get; init; }

	public required DateTime StartDate { get; init; }

	public required DateTime EndDate { get; init; }

	public required ReadOnlyList<uint> Teams { get; init; }

	public required ReadOnlyList<MatchGroup> MatchGroups { get; set; }



	public static Result<EventSchedule, CreateEventScheduleError> Create(
		MatchFormat format,
		string name,
		string eventCode,
		DateTime startDate,
		DateTime endDate,
		List<uint> teams,
		List<MatchGroup> matchGroups) {

		if (startDate > endDate) {
			return new StartAfterEnd {
				StartDateTime = startDate,
				EndDateTime = endDate
			};
		}

		foreach (MatchGroup matchGroup in matchGroups) {
			if (matchGroup.Format != format) {
				return new GroupHasWrongFormat {
					ScheduleFormat = format,
					GroupFormat = matchGroup.Format
				};
			}
		}

		return new EventSchedule {
			MatchFormat = format,
			Name = name,
			EventCode = eventCode,
			StartDate = startDate,
			EndDate = endDate,
			Teams = teams.ToReadOnly(),
			MatchGroups = matchGroups.ToReadOnly()
		};
	}

}