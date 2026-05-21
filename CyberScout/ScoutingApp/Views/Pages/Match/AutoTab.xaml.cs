using System.ComponentModel;
using Domain.DataCollectors;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using UtilitiesLibrary.Collections;

namespace ScoutingApp.Views.Pages.Match; 



public partial class AutoTab : ContentPage, INotifyPropertyChanged {

	public static string Route => "Auto";

	private AppManager AppManager { get; }

	public ReadOnlyList<InputDataCollector> Inputs => AppManager.ActiveMatchData.AutoTabInputs;


	public AutoTab(AppManagerGetter appManagerGetter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(AutoTab));
		AppManager.OnMatchStarted.Subscribe(() => OnPropertyChanged(nameof(Inputs)));

		BindingContext = this;
		InitializeComponent();
	}

}