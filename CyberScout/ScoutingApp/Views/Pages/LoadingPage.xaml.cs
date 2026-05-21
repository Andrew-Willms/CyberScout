using System;
using System.ComponentModel;
using System.Text;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;

namespace ScoutingApp.Views.Pages;



public partial class LoadingPage : ContentPage, INotifyPropertyChanged {

	public bool IsLoading {
		get;
		set {
			field = value;
			OnPropertyChanged();
		}
	}

	public string Text {
		get;
		set {
			field = value;
			OnPropertyChanged();
		}
	} = "Loading application...";

	public LoadingPage() {
		InitializeComponent();
	}

	// ReSharper disable once AsyncVoidMethod
	protected override async void OnAppearing() {

		try {
			base.OnAppearing();
		} catch (Exception exception) {

			StringBuilder stringBuilder = new(
				$"""
				 Error loading application.

				 An exception was raised while calling 'base.{nameof(base.OnAppearing)}()'.
				 Exception of type '{exception.GetType()}' with the message:
				 {exception.Message}
				 """
			);

			if (exception.InnerException is not null) {
				stringBuilder.Append(
					$"""

					 Inner exception of type '{exception.InnerException.GetType()}' with the message:
					 {exception.InnerException.Message}
					 """);
			}

			Text = stringBuilder.ToString();
			IsLoading = false;
			return;
		}

		try {
			await ServiceHelper.GetService<AppManagerGetter>().Create();
			// TODO: pre-create pages? maybe this should be done from AppShell

		} catch (Exception exception) {

			StringBuilder stringBuilder = new(
				$"""
				 Error loading application.

				 An exception was raised while calling '{nameof(AppManagerGetter)}.{nameof(AppManagerGetter.Create)}()'.
				 Exception of type '{exception.GetType()}' with the message:
				 {exception.Message}
				 """
			);

			if (exception.InnerException is not null) {
				stringBuilder.Append(
					$"""

					 Inner exception of type '{exception.InnerException.GetType()}' with the message:
					 {exception.InnerException.Message}
					 """);
			}
			
			Text = stringBuilder.ToString();
			IsLoading = false;
			return;
		}

		try {

			if (Application.Current is null) {

				Text =
					$"""
					Error loading application."

					'{nameof(Application)}.{nameof(Application.Current)}' was null.
					""";

				IsLoading = false;
				return;
			}

			// happy path
			Application.Current.Windows[0].Page = new AppShell();
			IsLoading = false;
			Text = "Application loaded.";

		} catch (Exception exception) {

			StringBuilder stringBuilder = new(
				$"""
				 Error loading application.

				 An exception was raised while setting the main page of the application.
				 Exception of type '{exception.GetType()}' with the message:
				 {exception.Message}
				 """
			);

			if (exception.InnerException is not null) {
				stringBuilder.Append(
					$"""

					 Inner exception of type '{exception.InnerException.GetType()}' with the message:
					 {exception.InnerException.Message}
					 """);
			}

			Text = stringBuilder.ToString();
			IsLoading = false;
		}
	}

}