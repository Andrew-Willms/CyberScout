using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using GameMakerWpf.DisplayData.Errors;
using UtilitiesLibrary.Results;

namespace GameMakerWpf.AppManagement;



public interface IErrorPresenter {

	public void DisplayError<TError>(TError error, Func<TError, ErrorDisplayData> errorDisplayDataGetter) where TError : OldError;

}



public partial class ErrorPresenter : Window, IErrorPresenter, INotifyPropertyChanged {
	public string Caption {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(Caption));
		}
	} = "";

	public string Message {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(Message));
		}
	} = "";


	public ErrorPresenter() {

		DataContext = this;

		InitializeComponent();
	}

	public void DisplayError<TError>(TError error, Func<TError, ErrorDisplayData> errorDisplayDataGetter) where TError : OldError {

		ErrorDisplayData errorDisplayData = errorDisplayDataGetter.Invoke(error);

		Caption = errorDisplayData.Caption;
		Message = errorDisplayData.Message;

		ShowDialog();
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e) {

		if (e.ChangedButton == MouseButton.Left) {
			DragMove();
		}
	}

	private void OkayButton_Clicked(object sender, RoutedEventArgs e) {
		Close();
	}



	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}