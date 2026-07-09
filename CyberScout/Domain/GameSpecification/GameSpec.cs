using System;
using System.Linq;
using Domain.EventSchedule;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Domain.GameSpecification;



public record GameSpec {

	public required string Name { get; init; }
	public string Description { get; init; } = string.Empty;
	public required int Year { get; init; }

	public required Version Version { get; init; } = new(1, 0, 0);
	//public DateTime VersionReleaseDate { get; } = DateTime.Now;

	// At least for now, GameSpec event can only have one MatchFormat.
	public required MatchFormat MatchFormat { get; init; }

	public required ReadOnlyList<DataFieldSpec> DataFields { get; init; }

	public required ReadOnlyList<InputSpec> SetupTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> AutoTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> TeleTabInputs { get; init; }
	public required ReadOnlyList<InputSpec> EndgameTabInputs { get; init; }



	private GameSpec() { }

	public static IOldResult<GameSpec> Create(
		string name,
		string description,
		int year,
		Version version,
		MatchFormat matchFormat,
		ReadOnlyList<DataFieldSpec> dataFields,
		ReadOnlyList<InputSpec> setupTabInputs,
		ReadOnlyList<InputSpec> autoTabInputs,
		ReadOnlyList<InputSpec> teleTabInputs,
		ReadOnlyList<InputSpec> endgameTabInputs) {

		foreach (InputSpec input in setupTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IOldResult<GameSpec>.OldError($"Input '{input.Label}' from {nameof(SetupTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in autoTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IOldResult<GameSpec>.OldError($"Input '{input.Label}' from {nameof(AutoTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in teleTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IOldResult<GameSpec>.OldError($"Input '{input.Label}' from {nameof(TeleTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		foreach (InputSpec input in endgameTabInputs) {

			if (!dataFields.Select(x => x.Name).Contains(input.DataFieldName)) {

				return new IOldResult<GameSpec>.OldError($"Input '{input.Label}' from {nameof(EndgameTabInputs)} targets the DataField with the " +
												   $"name '{input.DataFieldName}' but no DataField of that name was found.");
			}
		}

		return new IOldResult<GameSpec>.OldSuccess {
			Value = new() {
				Name = name,
				Description = description,
				Year = year,
				Version = version,
				MatchFormat = matchFormat,
				DataFields = dataFields.ToReadOnly(),
				SetupTabInputs = setupTabInputs,
				AutoTabInputs = autoTabInputs,
				TeleTabInputs = teleTabInputs,
				EndgameTabInputs = endgameTabInputs
			}
		};
	}



	// TODO consider rapping collections with value comparison wrappers and make this a record
	public virtual bool Equals(GameSpec? other) {

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
		    MatchFormat == other.MatchFormat &&
		    DataFields.SequenceEqual(other.DataFields) &&
		    SetupTabInputs.SequenceEqual(other.SetupTabInputs) &&
		    AutoTabInputs.SequenceEqual(other.AutoTabInputs) &&
		    TeleTabInputs.SequenceEqual(other.TeleTabInputs) &&
		    EndgameTabInputs.SequenceEqual(other.EndgameTabInputs);
	}

	public override int GetHashCode() {
		HashCode hashCode = new();
		hashCode.Add(Name);
		hashCode.Add(Description);
		hashCode.Add(Year);
		hashCode.Add(Version);
		hashCode.Add(MatchFormat);
		DataFields.Foreach(dataField => hashCode.Add(dataField));
		SetupTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		AutoTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		TeleTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		EndgameTabInputs.Foreach(inputSpec => hashCode.Add(inputSpec));
		return hashCode.ToHashCode();
	}

}