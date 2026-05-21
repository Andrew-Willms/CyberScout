using Microsoft.Maui.Controls;
using ScoutingApp.Views.Pages.Flyout;

namespace ScoutingApp;



public partial class AppShell : Shell {

	public static string MatchRoute => "Match";

	public AppShell() {

		Routing.RegisterRoute($"{MatchDetailsPage.Route}", typeof(MatchDetailsPage));

		InitializeComponent();

		GoToAsync(MatchRoute);
		// TODO: figure out if I actually need this line and if I do should I move it to LoadingPage?
	}

}