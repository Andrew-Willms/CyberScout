using System.Collections;
using System.Drawing;
using Domain.GameSpecification;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;
using UtilitiesLibrary.Results;
using MatchType = Domain.MatchData.MatchType;

namespace Domain.Tests.Serialization;



public class SampleData : IEnumerable<object[]> {

	public static readonly GameSpec GameSpec = (GameSpec.Create(
			name: "ReefScape",
			year: 2025,
			description: "",
			version: new(1, 0, 0),
			robotsPerAlliance: 3u,
			alliancesPerMatch: 2u,
			alliances: new List<AllianceColor> {
				new() { Color = Color.Red, Name = "Red Alliance" },
				new() { Color = Color.Blue, Name = "Blue Alliance" }
			}.ToReadOnly(),
			dataFields: new List<DataFieldSpec> {
				new IntegerDataFieldSpec { Name = "Auto L1 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Auto L2 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Auto L3 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Auto L4 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Auto Algae Net", InitialValue = 0, MinValue = 0, MaxValue = 255 },
				new IntegerDataFieldSpec { Name = "Auto Algae Processor", InitialValue = 0, MinValue = 0, MaxValue = 255 },
				new BooleanDataFieldSpec {
					Name = "Auto Mobility",
					InitialValue = false
				},
				new IntegerDataFieldSpec { Name = "Tele L1 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Tele L2 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Tele L3 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Tele L4 Coral", InitialValue = 0, MinValue = 0, MaxValue = 12 },
				new IntegerDataFieldSpec { Name = "Tele Algae Net", InitialValue = 0, MinValue = 0, MaxValue = 255 },
				new IntegerDataFieldSpec { Name = "Tele Algae Processor", InitialValue = 0, MinValue = 0, MaxValue = 255 },
				new SelectionDataFieldSpec {
					Name = "Climb",
					Options = new List<string> { "None", "Deep", "Shallow", "Failed" }.ToReadOnly(),
					InitialValue = "None",
					RequiresValue = true
				},
				new SelectionDataFieldSpec {
					Name = "Disconnected",
					Options = new List<string> { "None of match", "Some of match", "Most of match", "All of match" }.ToReadOnly(),
					InitialValue = "None of match",
					RequiresValue = true
				},
				new SelectionDataFieldSpec {
					Name = "Defense",
					Options = new List<string> { "N/A", "1", "2", "3", "4", "5" }.ToReadOnly(),
					InitialValue = "N/A",
					RequiresValue = true
				},
				new TextDataFieldSpec { Name = "Comments", InitialValue = "", MustNotBeEmpty = true, MustNotBeInitialValue = false }
			}.ToReadOnly(),
			setupTabInputs: new List<InputSpec>().ToReadOnly(),
			autoTabInputs: new List<InputSpec> {
				new() { DataFieldName = "Auto L1 Coral", Label = "L1 Coral" },
				new() { DataFieldName = "Auto L2 Coral", Label = "L2 Coral" },
				new() { DataFieldName = "Auto L3 Coral", Label = "L3 Coral" },
				new() { DataFieldName = "Auto L4 Coral", Label = "L4 Coral" },
				new() { DataFieldName = "Auto Algae Net", Label = "Algae Net" },
				new() { DataFieldName = "Auto Algae Processor", Label = "Processor Algae" },
				new() { DataFieldName = "Auto Mobility", Label = "Auto Mobility" }
			}.ToReadOnly(),
			teleTabInputs: new List<InputSpec> {
				new() { DataFieldName = "Tele L1 Coral", Label = "L1 Coral" },
				new() { DataFieldName = "Tele L2 Coral", Label = "L2 Coral" },
				new() { DataFieldName = "Tele L3 Coral", Label = "L3 Coral" },
				new() { DataFieldName = "Tele L4 Coral", Label = "L4 Coral" },
				new() { DataFieldName = "Tele Algae Net", Label = "Algae Net" },
				new() { DataFieldName = "Tele Algae Processor", Label = "Processor Algae" }
			}.ToReadOnly(),
			endgameTabInputs: new List<InputSpec> {
				new() { DataFieldName = "Climb", Label = "Climb" },
				new() { DataFieldName = "Disconnected", Label = "Disconnected" },
				new() { DataFieldName = "Defense", Label = "Defense Effectiveness" },
				new() { DataFieldName = "Comments", Label = "Comments" },
			}.ToReadOnly()) as IOldResult<GameSpec>.OldSuccess)!.Value;

	private readonly List<object[]> Data = [

		[ 
			MatchData.FromRaw(
				gameSpecification: GameSpec, 
				eventCode: null,
				eventSchedule: null,
				scoutName: "ScoutName",
				match: new() { MatchNumber = 1, Type = MatchType.Qualification, ReplayNumber = 0 },
				teamNumber: 0,
				allianceIndex: 0,
				startTime: DateTime.Now,
				endTime: DateTime.Now,
				dataFieldValues: new object[] {
					0,0,0,0,0,0,true,0,0,0,0,0,0,"None".Optionalize(),"None of match".Optionalize(),"N/A".Optionalize(),"Comments"
				}.ToReadOnly())!
		],
		[
			MatchData.FromRaw(
				gameSpecification: GameSpec,
				eventCode: "Waterloo",
				eventSchedule: null,
				scoutName: "Scoutty McScoutface",
				match: new() { MatchNumber = 3, Type = MatchType.Elimination, ReplayNumber = 1 },
				teamNumber: 4678,
				allianceIndex: 1,
				startTime: DateTime.Now,
				endTime: DateTime.Now.AddSeconds(180),
				dataFieldValues: new object[] {
					12,1,4,2,255,0,false,12,12,12,12,9,2,"Failed".Optionalize(),"Most of match".Optionalize(),"2".Optionalize(),"things move things"
				}.ToReadOnly())!
		],

	];

	public IEnumerator<object[]> GetEnumerator() {
		return Data.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

}