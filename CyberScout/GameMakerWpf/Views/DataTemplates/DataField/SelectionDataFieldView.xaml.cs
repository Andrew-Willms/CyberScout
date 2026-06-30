using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using GameMakerWpf.AppManagement;
using GameMakerWpf.DisplayData.Errors.ErrorData;
using GameMakerWpf.Domain;
using GameMakerWpf.Domain.Editors.DataFieldEditors;
using Microsoft.Extensions.DependencyInjection;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;
using UtilitiesLibrary.Validation.Inputs;
using WPFUtilities;

namespace GameMakerWpf.Views.DataTemplates.DataField;



public partial class SelectionDataFieldView : AppManagerDependent, INotifyPropertyChanged {

	private static IErrorPresenter ErrorPresenter => App.ServiceProvider.GetRequiredService<IErrorPresenter>();



	private SelectionDataFieldEditor? Editor => App
		.ServiceProvider
		.GetRequiredService<IAppManager>()
		.SelectedDataField?
		.DataFieldTypeEditor as SelectionDataFieldEditor;
	
	[DependsOn(nameof(AppManager.GameEditor))]
	[DependsOn(nameof(AppManager.SelectedDataField))]
	public ObservableList<SingleInput<string, string, ErrorSeverity>, string>? Options => Editor?.Options;

	[DependsOn(nameof(AppManager.GameEditor))]
	[DependsOn(nameof(AppManager.SelectedDataField))]
	public SingleInput<string, string, ErrorSeverity>? InitialValue => Editor?.InitialValue;

	[DependsOn(nameof(AppManager.GameEditor))]
	[DependsOn(nameof(AppManager.SelectedDataField))]
	public SingleInput<bool, bool, ErrorSeverity>? RequiresValue => Editor?.RequiresValue;

	public SingleInput<string, string, ErrorSeverity>? SelectedOption {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(SelectedOption));
			OnPropertyChanged(nameof(RemoveButtonIsEnabled));
		}
	}

	public bool RemoveButtonIsEnabled => SelectedOption is not null;



	public SelectionDataFieldView() {

		DataContext = this;

		InitializeComponent();
	}



	private void AddButton_Click(object sender, RoutedEventArgs e) {

		if (Options is null) {
			throw new InvalidOperationException($"You should not be able to remove an option when the {nameof(Editor)} is null.");
		}

		Options.Add("Test");
	}

	private void RemoveButton_Click(object sender, RoutedEventArgs e) {

		if (SelectedOption is null) {
			throw new InvalidOperationException("You should not be able to press the remove button while there is no Option selected.");
		}

		if (Options is null) {
			throw new InvalidOperationException($"You should not be able to remove an option when the {nameof(Editor)} is null.");
		}

		IListRemoveOldResult<SingleInput<string, string, ErrorSeverity>> oldResult = Options.Remove(SelectedOption);

		switch (oldResult) {
			
			case OldSuccess:
				return;

			case IListRemoveOldResult<SingleInput<string, string, ErrorSeverity>>.ItemNotFound error:
				ErrorPresenter.DisplayError(error, RemoveFromListErrors.RemoveOptionError);
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