using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;

namespace ScoutingApp.Views.Pages.Flyout;



public partial class ScoutPage : ContentPage {

	public static string Route => "Scout";

	private AppManager AppManager { get; }
	private IErrorPresenter ErrorPresenter { get; }

	public string ScoutName {
		get => AppManager.Scout;
		set {
			AppManager.Scout = value;
			OnPropertyChanged();
		}
	}

	public string? Error {
		get;
		set {
			field = value;
			OnPropertyChanged();
		}
	}



	public ScoutPage(AppManagerGetter appManagerGetter, IErrorPresenter errorPresenter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(ScoutPage));
		ErrorPresenter = errorPresenter;

		Task.Run(async () => {

			try {
				string? name = await AppManager.GetScoutName();

				MainThread.BeginInvokeOnMainThread(() => {
					Error = name is null
						? "Could not load the last scout from the data store."
						: null;

					ScoutName = name ?? string.Empty;
				});

			} catch (Exception exception) {

				ErrorPresenter.DisplayError(
					$"Error loading scout name.",
					$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
					$"{(exception.InnerException is null
						? string.Empty
						: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
						  $"with message:\r\n{exception.InnerException.Message}")}");
			}
		});

		BindingContext = this;
		InitializeComponent();
	}

	private void SaveButton_Clicked(object? sender, EventArgs e) {

		Task.Run(async () => {

			try {
				bool success = await AppManager.SetScoutName(ScoutName);

				MainThread.BeginInvokeOnMainThread(() => {
					Error = success ? null : "There was an error saving the scout to the data store.";
				});

			} catch (Exception exception) {

				ErrorPresenter.DisplayError(
					$"Error saving scout name.",
					$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
					$"{(exception.InnerException is null
						? string.Empty
						: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
						  $"with message:\r\n{exception.InnerException.Message}")}");
			}
		});

		OnPropertyChanged();
	}

} 