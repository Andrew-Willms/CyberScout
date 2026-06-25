using System;
using System.Collections.Generic;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.GameSpecification;



public class GameSpec : IEquatable<GameSpec> {

	public required string Name { get; init; }
	public string Description { get; init; } = "";
	public required int Year { get; init; }

	public required Version Version { get; init; } = new(1, 0, 0);
	//public DateTime VersionReleaseDate { get; } = DateTime.Now;

	public required uint RobotsPerAlliance { get; init; }
	public required uint AlliancesPerMatch { get; init; }

	public required ReadOnlyList<AllianceColor> Alliances { get; init; }

	public required ReadOnlyList<DataFieldSpec> DataFields { get; init; }

	public required ReadOnlyList<InputSpec> SetupTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> AutoTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> TeleTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> EndgameTabInputs { get; init; }



	private GameSpec() { }

	public static IResult<GameSpec> Create(
		string name,
		int year,
		string description,
		Version version,
		uint robotsPerAlliance,
		uint alliancesPerMatch,
		ReadOnlyList<AllianceColor> alliances,
		ReadOnlyList<DataFieldSpec> dataFields,
		ReadOnlyList<InputSpec> setupTabInputs,
		ReadOnlyList<InputSpec> autoTabInputs,
		ReadOnlyList<InputSpec> teleTabInputs,
		ReadOnlyList<InputSpec> endgameTabInputs) {

		List<string> duplicateNames = alliances.Select(x => x.Name).Duplicates();
		foreach (string duplicate in duplicateNames) {
			return new IResult<GameSpec>.Error($"There are multiple alliances with the name '{duplicate}'.");
		}

		foreach (InputSpec input in setupTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IResult<GameSpec>.Error($"Input '{input.Label}' from {nameof(SetupTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in autoTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IResult<GameSpec>.Error($"Input '{input.Label}' from {nameof(AutoTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in teleTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IResult<GameSpec>.Error($"Input '{input.Label}' from {nameof(TeleTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in endgameTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IResult<GameSpec>.Error($"Input '{input.Label}' from {nameof(EndgameTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		return new IResult<GameSpec>.Success {
			Value = new() {
				Name = name,
				Description = description,
				Year = year,
				Version = version,
				RobotsPerAlliance = robotsPerAlliance,
				AlliancesPerMatch = alliancesPerMatch,
				Alliances = alliances,
				DataFields = dataFields.ToReadOnly(),
				SetupTabInputs = setupTabInputs,
				AutoTabInputs = autoTabInputs,
				TeleTabInputs = teleTabInputs,
				EndgameTabInputs = endgameTabInputs
			}
		};
	}



	// TODO consider rapping collections with value comparison wrappers and make this a record
	public bool Equals(GameSpec? other) {

		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return
			Name == other.Name &&
		    Description == other.Description &&
		    Year == other.Year &&
		    Version.Equals(other.Version) &&
		    RobotsPerAlliance == other.RobotsPerAlliance &&
		    AlliancesPerMatch == other.AlliancesPerMatch &&
		    Alliances.SequenceEqual(other.Alliances) &&
		    DataFields.SequenceEqual(other.DataFields) &&
		    SetupTabInputs.SequenceEqual(other.SetupTabInputs) &&
		    AutoTabInputs.SequenceEqual(other.AutoTabInputs) &&
		    TeleTabInputs.SequenceEqual(other.TeleTabInputs) &&
		    EndgameTabInputs.SequenceEqual(other.EndgameTabInputs);
	}

	public override bool Equals(object? obj) {

		if (obj is null) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		if (obj.GetType() != GetType()) {
			return false;
		}

		return Equals((GameSpec) obj);
	}

	public override int GetHashCode() {
		HashCode hashCode = new();
		hashCode.Add(Name);
		hashCode.Add(Description);
		hashCode.Add(Year);
		hashCode.Add(Version);
		hashCode.Add(RobotsPerAlliance);
		hashCode.Add(AlliancesPerMatch);
		Alliances.Foreach(alliance => hashCode.Add(alliance));
		DataFields.Foreach(dataField => hashCode.Add(dataField));
		SetupTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		AutoTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		TeleTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		EndgameTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		return hashCode.ToHashCode();
	}

}