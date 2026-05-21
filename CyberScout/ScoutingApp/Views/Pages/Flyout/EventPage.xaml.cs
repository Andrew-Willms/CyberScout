using System.Collections.Generic;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using UtilitiesLibrary.Collections;

namespace ScoutingApp.Views.Pages.Flyout; 



public partial class EventPage : ContentPage {

	public static string Route => "Event";

	public AppManager AppManager { get; }

	public ReadOnlyList<string> Events { get; } = new List<string> {
		"Test Event",
		"Waterloo",
		"Windsor",
		"DCMP"
	}.ToReadOnly();

	public EventPage(AppManagerGetter appManagerGetter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(EventPage));

		BindingContext = this;
		InitializeComponent(); 
	}

}