using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Domain.DataCollectors;
using Domain.Serialization;
using Database;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using ScoutingApp.Views.Pages.Flyout;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace ScoutingApp.Views.Pages.Match; 


// TODO: Fix scroll view in the xaml, make the errors wrap horizontally
public partial class ConfirmTab : ContentPage, INotifyPropertyChanged {

	public static string Route => "Confirm";

	private AppManager AppManager { get; }

	private IErrorPresenter ErrorPresenter { get; }

	public bool MatchIsDiscardable => !AppManager.CurrentMatchIsUnedited();

	public ObservableCollection<string> Errors {
		get {

			ObservableCollection<string> errors = [];

			if (AppManager.Scout == "") {
				errors.Add("The scout name has not been set. Go to the Scout page in the flyout menu to set the scout name.");
			}

			if (AppManager.EventCode == "") {
				errors.Add("The event has not been set. Go to the Event page in the flyout menu to set the event.");
			}

			if (AppManager.ActiveMatchData.MatchNumber == Optional.NoValue) {
				errors.Add("The match number has not been set.");
			}

			if (AppManager.ActiveMatchData.ReplayNumber == Optional.NoValue) {
				errors.Add("The replay number has not been set.");
			}

			// todo figure out how to not have the default MatchType overridden
			//if (AppManager.ActiveMatchData.MatchType == Optional.NoValue) {
			//	errors.Add("MatchType has not been set.");
			//}

			if (AppManager.ActiveMatchData.Alliance == Optional.NoValue) {
				errors.Add("An alliance has not been set.");
			}

			if (AppManager.ActiveMatchData.TeamNumber == Optional.NoValue) {
				errors.Add("The team number has not been set.");
			}

			foreach (DataField dataField in AppManager.ActiveMatchData.DataFields) {
				foreach (string error in dataField.Errors) {
					errors.Add(error);
				}
			}

			return errors;
		}
	}

	public bool MatchIstValid => Errors.IsEmpty();



	public ConfirmTab(AppManagerGetter appManagerGetter, IErrorPresenter errorPresenter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(ConfirmTab));
		ErrorPresenter = errorPresenter;

		AppManager.OnMatchStarted.Subscribe(() => OnPropertyChanged(""));

		BindingContext = this;
		InitializeComponent();
	}



	private void ConfirmTab_OnNavigatedTo(object? sender, NavigatedToEventArgs e) {
		OnPropertyChanged("");
	}

	private async void SaveButton_OnClicked(object? sender, EventArgs e) {

		try {
			SaveAndStartNewMatchResult result = await AppManager.SaveAndStartNewMatch();

			result.Switch(
				success => { },
				exception => {
					ErrorPresenter.DisplayError(
						"Cannot Save Match",
						$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
						$"{(exception.InnerException is null
							? string.Empty
							: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
							  $"with message:\r\n{exception.InnerException.Message}")}");
				},
				matchDataIsInvalid => {
					ErrorPresenter.DisplayError("Cannot Save Match", "The match data is invalid.");
				});

			if (!result.IsT0) {
				return;
			}

			// todo hacky as fuck way to navigate to the most recent match
#if ANDROID
			string deviceId = Android.Provider.Settings.Secure.GetString(
				Platform.CurrentActivity!.ContentResolver,
				Android.Provider.Settings.Secure.AndroidId)!;
#elif IOS
		string deviceId = UIKit.UIDevice.CurrentDevice.IdentifierForVendor.ToString();
#else
		string deviceId = null as string ?? throw new NotSupportedException();
#endif

			GetMatchDataResult getMatchDataResult = await AppManager.DataStore.GetMatchData();
			List<MatchDataDto>? allData = getMatchDataResult.IsT0 ? getMatchDataResult.AsT0 : null;
			MatchDataDto? mostRecentMatch = allData?.Where(x => x.DeviceId == deviceId).MaxBy(x => x.RecordId);

			// TODO: This seems like bad error handling
			if (mostRecentMatch is null) {
				await Shell.Current.GoToAsync($"//{MatchDetailsPage.Route}");
				return;
			}

			Dictionary<string, object> parameters = new() {
				{ MatchDetailsPage.MatchDataNavigationParameterName, mostRecentMatch },
#pragma warning disable CS8974 // Converting method group to non-delegate type
				{ MatchDetailsPage.MatchDeleterNavigationParameterName, DeleteMatch }
#pragma warning restore CS8974 // Converting method group to non-delegate type
			};

			await Shell.Current.GoToAsync($"//{AppShell.MatchRoute}/{SetupTab.Route}", false);
			await Shell.Current.GoToAsync($"//{SavedMatchesPage.Route}", false);
			await Shell.Current.GoToAsync($"{MatchDetailsPage.RouteFromSavedMatchesPage}", parameters);

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error saving match.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}

	private async void DiscardButton_OnClicked(object? sender, EventArgs e) {

		try {

			// Make CurrentMatchIsUnedited a bindable property and link it to the discard button being enabled
			if (!AppManager.CurrentMatchIsUnedited()) {

				bool discard = await Shell.Current.DisplayAlertAsync(
					"Discard Current Match?",
					"Do you want to discard the current match and start a new one? Doing so will delete all data entered in this match",
					"Discard and start new match.",
					"Continue with current match.");

				if (!discard) {
					return;
				}
			}

			AppManager.DiscardAndStartNewMatch();
			await Shell.Current.GoToAsync($"//{SetupTab.Route}");

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error discarding match.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}



	private async Task<bool> DeleteMatch(MatchDataDto matchData) {

		return await AppManager.DataStore.DeleteMatchData(matchData);
	}



	public new event PropertyChangedEventHandler? PropertyChanged;

	private new void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}