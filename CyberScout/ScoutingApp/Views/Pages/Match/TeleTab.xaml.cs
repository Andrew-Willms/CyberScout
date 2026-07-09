using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using UtilitiesLibrary.Collections;
using InputDataCollector = ScoutingApp.DataCollectors.InputDataCollector;

namespace ScoutingApp.Views.Pages.Match; 



public partial class TeleTab : ContentPage {

	public static string Route => "Tele";

	private AppManager AppManager { get; }

	public ReadOnlyList<InputDataCollector> Inputs => AppManager.ActiveMatchData.TeleTabInputs;

	public TeleTab(AppManagerGetter appManagerGetter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(TeleTab));
		AppManager.OnMatchStarted.Subscribe(() => OnPropertyChanged(nameof(Inputs)));

		BindingContext = this;
		InitializeComponent();
	}

}