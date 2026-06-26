using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Comms.Dtos;
using Database;
using Database.Results.MatchData;
using Domain.Data;
using Domain.DataCollectors;
using Domain.GameSpecification;
using Microsoft.Maui.ApplicationModel;
using OneOf;
using UtilitiesLibrary.Optional;
using Event = UtilitiesLibrary.SimpleEvent.Event;

namespace ScoutingApp.AppManagement;



[GenerateOneOf]
public partial class SaveAndStartNewMatchResult : OneOfBase<OneOf.Types.Success, Exception, MatchDataIsInvalid>;



public class MatchDataIsInvalid;



public class AppManager : INotifyPropertyChanged {

	public GameSpec? GameSpecification { get; private set; }

	public MatchDataCollector ActiveMatchData {
		get;
		private set {
			field = value;
			OnPropertyChanged(nameof(ActiveMatchData));
		}
	} = null!;

	public string Scout {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(Scout));
		}
	} = "";


	public string EventCode { get; set; }

	public EventSchedule? EventSchedule { get; set; }

	public Event OnMatchStarted { get; } = new();

	public Event OnNewData { get; } = new();

	public IDataStore DataStore { get; }



	private AppManager(IDataStore dataStore) {

		GameSpecification = null!; // todo fix hack
		DataStore = dataStore;

		// todo: fix hack
		if (DateTime.Now >= new DateTime(2026, 3, 27) && DateTime.Now <= new DateTime(2026, 3, 28)) {
			EventCode = "Waterloo";

		} else if (DateTime.Now >= new DateTime(2026, 4, 9) && DateTime.Now <= new DateTime(2026, 4, 11)) {
			EventCode = "Windsor";

		} else if (DateTime.Now >= new DateTime(2026, 4, 16) && DateTime.Now <= new DateTime(2025, 4, 19)) {
			EventCode = "DCMP";

		} else if (DateTime.Now >= new DateTime(2026, 4, 29) && DateTime.Now <= new DateTime(2026, 5, 2)) {
			EventCode = "Worlds";

		} else {
			EventCode = "Test Event";
		}
	}



	public static async Task<AppManager?> Create() {

#if ANDROID
		string directory = Android.App.Application.Context.GetExternalFilesDir(null)!.AbsoluteFile.Path;
#else
		string directory = FileSystem.Current.AppDataDirectory;
#endif

		string dbPath = System.IO.Path.Combine(directory, "ScoutingApp.db"); // TODO: move magic string literal somewhere else

		IDataStoreCreator dataStoreCreator = ServiceHelper.GetService<IDataStoreCreator>();
		IDataStore? dataStore = await dataStoreCreator.Create(dbPath);

		if (dataStore is null) {
			throw new(); // TODO: handle this better
		}

		AppManager appManager = new(dataStore);

		appManager.GameSpecification = (await dataStore.GetGameSpecs()).First(); // todo game specs should be selectable within the app
		appManager.Scout = await appManager.GetScoutName() ?? string.Empty; // todo cleanup
		appManager.StartNewMatch();

		return appManager;
	}

	private void StartNewMatch() {

		ActiveMatchData = new(GameSpecification);
		OnMatchStarted.Invoke();
	}

	public async Task<SaveAndStartNewMatchResult> SaveAndStartNewMatch() {

#if ANDROID
		string deviceId = Android.Provider.Settings.Secure.GetString(
			Platform.CurrentActivity!.ContentResolver,
			Android.Provider.Settings.Secure.AndroidId)!;
#elif IOS
		string deviceId = UIKit.UIDevice.CurrentDevice.IdentifierForVendor.ToString();
#else
		string deviceId = null as string ?? throw new NotSupportedException();
#endif

		MatchData? matchData = MatchData.FromDataCollector(ActiveMatchData, EventCode, EventSchedule, Scout);

		if (matchData is null) {
			return new MatchDataIsInvalid();
		}

		CreateMatchDataDto createDto = new() {
			MatchData = matchData,
			DeviceId = deviceId,
			EditBasedOn = ActiveMatchData.EditOf is null ? null : (ActiveMatchData.EditOf?.DeviceId!, (int)ActiveMatchData.EditOf?.RecordId!)
		};


		AddNewMatchDataResult result = await DataStore.AddNewMatchData(createDto);

		return result.Match<SaveAndStartNewMatchResult>(
			success => {
				StartNewMatch();
				return new OneOf.Types.Success();
			},
			exception => exception);
	}

	// TODO: move to MatchDataCollector and make it implement IEquatable<>
	public bool CurrentMatchIsUnedited() {

		MatchDataCollector unedited = new(GameSpecification);

		if (ActiveMatchData.MatchNumber != unedited.MatchNumber) {
			return false;
		}

		if (ActiveMatchData.ReplayNumber != unedited.ReplayNumber) {
			return false;
		}

		if (ActiveMatchData.MatchType != unedited.MatchType) {
			return false;
		}

		if (ActiveMatchData.TeamNumber != unedited.TeamNumber) {
			return false;
		}

		if (ActiveMatchData.Alliance != unedited.Alliance) {
			return false;
		}

		for (int i = 0; i < ActiveMatchData.DataFields.Count; i++) {

			switch (ActiveMatchData.DataFields[i], unedited.DataFields[i]) {

				// TODO: this is another instance where Optional causes issues.
				case (SelectionDataField { Value.HasValue: false}, SelectionDataField { Value.HasValue: true } uneditedField): {

					if (((Optional<string>)uneditedField.BaseValue).Value != string.Empty) {
						return false;
					}

					break;
				}
				case (SelectionDataField { Value.HasValue: true } activeField, SelectionDataField { Value.HasValue: false }): {

					if (((Optional<string>)activeField.BaseValue).Value != string.Empty) {
						return false;
					}

					break;
				}
				default: {
					if (!ActiveMatchData.DataFields[i].BaseValue.Equals(unedited.DataFields[i].BaseValue)) {
						return false;
					}
					break;
				}
			}
		}

		return true;
	}

	public void DiscardAndStartNewMatch() {
		StartNewMatch();
	}

	public void DiscardAndStartEditingMatch(MatchDataDto importMatchData) {

		// todo fix this, the check is broken and returns false when it should return true I think
		//if (!matchData.MatchData.GameSpecification.Equals(GameSpecification)) {
		//	throw new NotImplementedException("Figure out how to handle the game specs being different.");
		//}

		ActiveMatchData = new(GameSpecification) {
			MatchNumber = importMatchData.MatchData.Match.MatchNumber,
			ReplayNumber = importMatchData.MatchData.Match.ReplayNumber,
			MatchType = importMatchData.MatchData.Match.Type,
			TeamNumber = importMatchData.MatchData.TeamNumber,
			Alliance = importMatchData.MatchData.AllianceIndex,
			EditOf = (importMatchData.DeviceId, importMatchData.RecordId)
		};

		for (int i = 0; i < GameSpecification.DataFields.Count; i++) {

			switch (ActiveMatchData.DataFields[i], importMatchData.MatchData.DataFields[i]) {
				case (BooleanDataField booleanDataField, bool boolValue):
					booleanDataField.Value = boolValue;
					break;
				case (IntegerDataField integerDataField, int intValue):
					integerDataField.Value = intValue;
					break;
				case (SelectionDataField selectionDataField, Optional<string> selection):
					selectionDataField.Value = selection;
					break;
				case (SelectionDataField selectionDataField, Optional): // TODO: deal with this shit again
					selectionDataField.Value = Optional.NoValue;
					break;
				case (TextDataField textDataField, string text):
					textDataField.Value = text;
					break;
				default:
					throw new NotImplementedException("Figure out how to hand data field type miss-matches");
			}
		}

		OnMatchStarted.Invoke();
	}




	public async Task<string?> GetScoutName() {
		return await DataStore.GetLastScout();
	}

	public async Task<bool> SetScoutName(string name) {
		return await DataStore.SetLastScout(name);
	}



	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}