using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using GameMakerWpf.AppManagement;
using GameMakerWpf.DisplayData.Errors.ErrorData;
using GameMakerWpf.Domain.Data;
using GameMakerWpf.Domain.EditingData;
using GameMakerWpf.Domain.Editors;
using GameMakerWpf.Domain.Editors.DataFieldEditors;
using Microsoft.Extensions.DependencyInjection;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;
using WPFUtilities;

namespace GameMakerWpf.Views.Tabs;



public partial class DataFieldTabView : AppManagerDependent, INotifyPropertyChanged {

	private static IErrorPresenter ErrorPresenter => App.ServiceProvider.GetRequiredService<IErrorPresenter>();

	// These can't be static or PropertyChanged events on them won't work.
	private GameEditor GameEditor => App.ServiceProvider.GetRequiredService<IAppManager>().GameEditor;

	[DependsOn(nameof(AppManager.GameEditor))]
	public ObservableList<DataFieldEditor, DataFieldEditingData> DataFields => GameEditor.DataFields;

	[DependsOn(nameof(AppManager.SelectedDataField))]
	public DataFieldEditor? SelectedDataField {
		get => App.ServiceProvider.GetRequiredService<IAppManager>().SelectedDataField;
		set => App.ServiceProvider.GetRequiredService<IAppManager>().SelectedDataField = value;
	}

	[DependsOn(nameof(AppManager.SelectedDataField))]
	public bool RemoveButtonIsEnabled => SelectedDataField is not null;



	public DataFieldTabView() {
		
		DataContext = this;

		InitializeComponent();
	}



	private void AddButton_Click(object sender, RoutedEventArgs e) {

		DataFields.Add(DefaultEditingDataValues.DefaultDataFieldEditingData);
	}

	private void RemoveButton_Click(object sender, RoutedEventArgs e) {

		if (SelectedDataField is null) {
			throw new InvalidOperationException("The RemoveButton should not be enabled if no DataField is selected.");
		}

		IListRemoveOldResult<DataFieldEditor> oldResult = DataFields.Remove(SelectedDataField);

		switch (oldResult) {

			case OldSuccess:
				return;

			case IListRemoveOldResult<DataFieldEditor>.ItemNotFound error:
				ErrorPresenter.DisplayError(error, RemoveFromListErrors.RemoveDataFieldError);
				return;

			default:
				throw new UnreachableException();
		}
	}

	private void MoveUpButton_Click(object sender, RoutedEventArgs e) {
		throw new NotImplementedException();
	}

	private void MoveDownButton_Click(object sender, RoutedEventArgs e) {
		throw new NotImplementedException();
	}



	public override event PropertyChangedEventHandler? PropertyChanged;

	protected override void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}