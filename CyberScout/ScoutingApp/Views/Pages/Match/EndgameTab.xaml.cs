using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using UtilitiesLibrary.Collections;
using InputDataCollector = ScoutingApp.DataCollectors.InputDataCollector;

namespace ScoutingApp.Views.Pages.Match; 



public partial class EndgameTab : ContentPage {

	public static string Route => "Endgame";

	private AppManager AppManager { get; }

	public ReadOnlyList<InputDataCollector> Inputs => AppManager.ActiveMatchData.EndgameTabInputs;

	public EndgameTab(AppManagerGetter appManagerGetter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(EndgameTab));
		AppManager.OnMatchStarted.Subscribe(() => OnPropertyChanged(nameof(Inputs)));

		BindingContext = this;
		InitializeComponent();
	}

}