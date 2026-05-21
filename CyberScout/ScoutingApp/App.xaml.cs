using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ScoutingApp.Views.Pages;

namespace ScoutingApp;



public partial class App : Application {

	public App() {

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState) {
		return new(new LoadingPage());
	}

}