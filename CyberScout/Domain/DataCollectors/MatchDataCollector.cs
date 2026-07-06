using System;
using System.Collections.Generic;
using System.Linq;
using Domain.GameSpecification;
using Domain.MatchData;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace Domain.DataCollectors;


// TODO: make this equatable?
public class MatchDataCollector {

	public GameSpec GameSpecification { get; }

	// todo event selection

	public Optional<uint> MatchNumber { get; set; } = Optional.NoValue;
	public Optional<uint> ReplayNumber { get; set; } = 0u.Optionalize();
	public Optional<MatchType> MatchType { get; set; } = MatchData.MatchType.Qualification.Optionalize();

	public Optional<uint> TeamNumber { get; set; } = Optional.NoValue;

	public Optional<uint> Alliance { get; set; } = Optional.NoValue;

	public DateTime StartTime { get; } = DateTime.Now;

	public ReadOnlyList<DataField> DataFields { get; }

	public ReadOnlyList<InputDataCollector> SetupTabInputs { get; }
	public ReadOnlyList<InputDataCollector> AutoTabInputs { get; }
	public ReadOnlyList<InputDataCollector> TeleTabInputs { get; }
	public ReadOnlyList<InputDataCollector> EndgameTabInputs { get; }

	public (string DeviceId, int RecordId)? EditOf { get; init; }

	public bool IsValid =>
		MatchNumber.HasValue &&
		ReplayNumber.HasValue &&
		MatchType.HasValue &&
		Alliance.HasValue &&
		TeamNumber.HasValue &&
		DataFields.SelectMany(x => x.Errors).IsEmpty();

	public List<string> Errors {
		get {
			List<string> errors = [];

			if (!MatchNumber.HasValue) {
				errors.Add("The match number has not been set.");
			}

			if (!ReplayNumber.HasValue) {
				errors.Add("The replay number has not been set.");
			}

			if (!MatchType.HasValue) {
				errors.Add("Is playoff has not been set.");
			}

			if (!Alliance.HasValue) {
				errors.Add("The Alliance has not been set.");
			}

			if (!TeamNumber.HasValue) {
				errors.Add("The team number has not been set.");
			}

			errors.AddRange(DataFields.SelectMany(dataField => dataField.Errors));

			return errors;
		}
	}



	public MatchDataCollector(GameSpec gameSpecification) {

		GameSpecification = gameSpecification;

		DataFields = GameSpecification.DataFields.Select(x => x.ToDataField()).ToReadOnly();

		SetupTabInputs = GameSpecification.SetupTabInputs.Select(x => InputDataCollector.FromDataField(x, DataFields.Single(xx => xx.Name == x.DataFieldName))).ToReadOnly();
		AutoTabInputs = GameSpecification.AutoTabInputs.Select(x => InputDataCollector.FromDataField(x, DataFields.Single(xx => xx.Name == x.DataFieldName))).ToReadOnly();
		TeleTabInputs = GameSpecification.TeleTabInputs.Select(x => InputDataCollector.FromDataField(x, DataFields.Single(xx => xx.Name == x.DataFieldName))).ToReadOnly();
		EndgameTabInputs = GameSpecification.EndgameTabInputs.Select(x => InputDataCollector.FromDataField(x, DataFields.Single(xx => xx.Name == x.DataFieldName))).ToReadOnly();
	}

}