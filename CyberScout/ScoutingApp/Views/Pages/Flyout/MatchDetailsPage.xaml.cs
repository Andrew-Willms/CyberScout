using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Comms.Dtos;
using Comms.Dtos.Match;
using Comms.Serialization;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using ScoutingApp.Views.Pages.Match;

namespace ScoutingApp.Views.Pages.Flyout;



[QueryProperty(nameof(SavedImportMatch), MatchDataNavigationParameterName)]
[QueryProperty(nameof(MatchDeleter), MatchDeleterNavigationParameterName)]
public partial class MatchDetailsPage : ContentPage, INotifyPropertyChanged {

	public static string Route => $"{SavedMatchesPage.Route}/MatchDetails";
	public static string RouteFromSavedMatchesPage => "/MatchDetails";

	public const string MatchDataNavigationParameterName = nameof(MatchDto);
	public const string MatchDeleterNavigationParameterName = nameof(MatchDeleter);

	private AppManager AppManager { get; }
	private IErrorPresenter ErrorPresenter { get; }

	public MatchDto SavedImportMatch {
		get;
		set {
			field = value;
			QrCodeContent = MatchDataDtoToCsv.Serialize(value);
			OnPropertyChanged(nameof(SavedImportMatch));
		}
	} = null!;

	public string QrCodeContent {
		get;
		private set {
			field = value;
			OnPropertyChanged(nameof(QrCodeContent));
		}
	} = null!;

	public Func<MatchDto, Task<bool>> MatchDeleter { get; init; } = null!;



	public MatchDetailsPage(AppManagerGetter appManagerGetter, IErrorPresenter errorPresenter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(MatchDetailsPage));
		ErrorPresenter = errorPresenter;

		BindingContext = this;
		InitializeComponent();
		Shell.SetTabBarIsVisible(this, false);
	}



	private async void DeleteButton_OnPressed(object? sender, EventArgs e) {

		try {
			bool success = await MatchDeleter(SavedImportMatch);

			if (success) {
				await Shell.Current.GoToAsync($"//{SavedMatchesPage.Route}");
				return;
			}

			ErrorPresenter.DisplayError("Failed", "Could not delete match.");

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error deleting match.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}

	private async void EditButton_OnPressed(object? sender, EventArgs e) {

		try {

			if (!AppManager.CurrentMatchIsUnedited()) {

				bool discard = await Shell.Current.DisplayAlertAsync(
					"Discard Current Match and Edit Selected Match",
					"Do you want to discard the current match and start editing the selected One? Doing so will delete all data entered in this match",
					"Discard and start editing.",
					"Continue with current match.");

				if (!discard) {
					return;
				}
			}

			AppManager.DiscardAndStartEditingMatch(SavedImportMatch);
			await Shell.Current.GoToAsync($"//{SetupTab.Route}");

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error editing match.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}

	private async void ReturnToMatch_ButtonPressed(object? sender, EventArgs e) {

		try {
			// Because we first navigate back to the SavedMatchesPage when it animates navigating to the SetupTab
			// it shows it coming from the SavedMatchesPage and so the SavedMatchesPage flashes on the screen for a tiny bit.
			// This looks janky, but I can't find a way around it, and it still happens if I clear the navigation stack first.
			await Shell.Current.GoToAsync($"//{SavedMatchesPage.Route}");
			await Shell.Current.GoToAsync($"//{AppShell.MatchRoute}/{SetupTab.Route}");

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error returning to match.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}

	private void ScanOtherCodes_ButtonPressed(object? sender, EventArgs e) {
		Shell.Current.GoToAsync($"//{QrCodeScanner.Route}");
	}



	public new event PropertyChangedEventHandler? PropertyChanged;

	private new void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}