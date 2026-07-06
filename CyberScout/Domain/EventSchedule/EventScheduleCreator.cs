using System;
using System.Collections.Generic;
using UtilitiesLibrary.Collections;

namespace Domain.EventSchedule;



// TODO: move this to somewhere GUI related
public class EventScheduleCreator {

	public ReadOnlyList<CreateEventScheduleError> Errors { get; private set; } = new List<CreateEventScheduleError>().ToReadOnly();

	public MatchFormat? MatchFormat { get; set; }

	public string? Name { get; set; }

	public string? EventCode { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public List<uint> Teams { get; } = [];

	public List<ScheduledMatch> Matches { get; } = [];

}