using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using GameMakerWpf.AppManagement;
using GameMakerWpf.DisplayData.Errors.ErrorData;
using GameMakerWpf.Domain.Data;
using GameMakerWpf.Domain.EditingData;
using GameMakerWpf.Domain.Editors;
using Microsoft.Extensions.DependencyInjection;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;
using WPFUtilities;

namespace GameMakerWpf.Views.Tabs;



public partial class AutoTabView : AppManagerDependent, INotifyPropertyChanged {

	private static IErrorPresenter ErrorPresenter => App.ServiceProvider.GetRequiredService<IErrorPresenter>();

	// These can't be static or PropertyChanged events on them won't work.
	private GameEditor GameEditor => App.ServiceProvider.GetRequiredService<IAppManager>().GameEditor;

	[DependsOn(nameof(AppManager.GameEditor))]
	public ObservableList<InputEditor, InputEditingData> Inputs => GameEditor.AutoTabInputs;

	public InputEditor? SelectedInput {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(SelectedInput));
			OnPropertyChanged(nameof(RemoveButtonIsEnabled));
		}
	}

	public bool RemoveButtonIsEnabled => SelectedInput is not null;



	public AutoTabView() {

		DataContext = this;

		InitializeComponent();
	}



	private void AddInputButton_Click(object sender, RoutedEventArgs e) {
		
		Inputs.Add(DefaultEditingDataValues.DefaultInputEditingData);
	}

	private void RemoveInputButton_Click(object sender, RoutedEventArgs e) {
		
		if (SelectedInput is null) {
			throw new InvalidOperationException("The RemoveButton should not be enabled if no Alliance is selected.");
		}

		IListRemoveOldResult<InputEditor> oldResult = Inputs.Remove(SelectedInput);

		switch (oldResult) {

			case OldSuccess:
				return;

			case IListRemoveOldResult<InputEditor>.ItemNotFound error:
				ErrorPresenter.DisplayError(error, RemoveFromListErrors.RemoveAutoInputError);
				return;

			default:
				throw new UnreachableException();
		}
	}

	private void MoveInputUpButton_Click(object sender, RoutedEventArgs e) {
		throw new NotImplementedException();
	}

	private void MoveInputDownButton_Click(object sender, RoutedEventArgs e) {
		throw new NotImplementedException();
	}



	public override event PropertyChangedEventHandler? PropertyChanged;

	protected override void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}